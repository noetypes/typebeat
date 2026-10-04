// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Platform;
using Realms;
using typebeat.Game.Beatmaps;
using typebeat.Game.Database;
using typebeat.Game.Rulesets;
using typebeat.Game.Rulesets.TypeBeat.Beatmaps;
using typebeat.Game.Rulesets.TypeBeat.Objects;
using typebeat.Game.Tests.Beatmaps;

namespace typebeat.Game.Rulesets.TypeBeat.Tests.NonVisual
{
    [TestFixture]
    public class DifficultyTagClassifierTest
    {
        [Test]
        public void EmptyMapHasNoTags() => Assert.That(DifficultyTagClassifier.Compute(Array.Empty<LyricLine>()), Is.EqualTo(BeatmapDifficultyTags.None));

        [Test]
        public void FreestyleComesFromPlayableLyrics()
        {
            var freestyle = phrase("hello &&&", 0, 4000);
            Assert.That(DifficultyTagClassifier.Compute(new[] { freestyle }).HasFlag(BeatmapDifficultyTags.Freestyle), Is.True);
            var plain = phrase("hello world", 0, 4000, original: "hello &&&");
            Assert.That(DifficultyTagClassifier.Compute(new[] { plain }).HasFlag(BeatmapDifficultyTags.Freestyle), Is.False);
        }

        [TestCase("Asphyxiated", true)]
        [TestCase("with one hand on the door", false)]
        [TestCase("zzqx wvvt qqzz", false)]
        public void ComplexRequiresLowScoredTypability(string text, bool expected)
            => Assert.That(DifficultyTagClassifier.Compute(new[] { phrase(text, 0, 4000) }).HasFlag(BeatmapDifficultyTags.Complex), Is.EqualTo(expected));

        [TestCase(125)]
        [TestCase(500)]
        public void DenseShortPassageIsBurstRatherThanStamina(double wordMs)
        {
            var lines = steady(4, 2000).ToList();
            lines.AddRange(steady(2, wordMs, start: 32000));
            lines.AddRange(steady(4, 2000, start: 32000 + 8 * wordMs));
            var tags = DifficultyTagClassifier.Compute(lines);
            Assert.That(tags.HasFlag(BeatmapDifficultyTags.Burst), Is.True);
            Assert.That(tags.HasFlag(BeatmapDifficultyTags.Stamina), Is.False);
        }

        [Test]
        public void SustainedDemandIsStaminaRatherThanBurst()
        {
            var tags = DifficultyTagClassifier.Compute(steady(40, 500));
            Assert.That(tags.HasFlag(BeatmapDifficultyTags.Stamina), Is.True);
            Assert.That(tags.HasFlag(BeatmapDifficultyTags.Burst), Is.False);
        }

        [Test]
        public void ShortSteadyMapIsNotBurstJustBecauseItHasNoLongWindow()
            => Assert.That(DifficultyTagClassifier.Compute(steady(4, 500)).HasFlag(BeatmapDifficultyTags.Burst), Is.False);

        [Test]
        public void LongSparseMapDoesNotGetStaminaJustForItsLength()
            => Assert.That(DifficultyTagClassifier.Compute(steady(40, 10000)) & (BeatmapDifficultyTags.Burst | BeatmapDifficultyTags.Stamina), Is.EqualTo(BeatmapDifficultyTags.None));

        [Test]
        public void IrregularTimingGetsRhythm()
        {
            var lines = Enumerable.Range(0, 20).Select(i => phrase("flame river cider amber", i * 4000, 4000, irregular: true)).ToArray();
            var plain = Enumerable.Range(0, 20).Select(i => phrase("flame river cider amber", i * 4000, 4000)).ToArray();
            var irregular = LyricDifficulty.RateChunked(lines, 1, false, LyricDifficulty.ShippedScores, ChunkedEndurance.Live).Report;
            var regular = LyricDifficulty.RateChunked(plain, 1, false, LyricDifficulty.ShippedScores, ChunkedEndurance.Live).Report;
            TestContext.WriteLine($"Rhythm multipliers: irregular {irregular.RhythmMultiplier}, regular {regular.RhythmMultiplier}");
            Assert.That(DifficultyTagClassifier.Compute(lines).HasFlag(BeatmapDifficultyTags.Rhythm), Is.True);
            Assert.That(DifficultyTagClassifier.Compute(plain).HasFlag(BeatmapDifficultyTags.Rhythm), Is.False);
        }

        [Test]
        public void ApplyingFactsReplacesTagsAfterAnEdit()
        {
            var info = new BeatmapInfo
            {
                DifficultyTags = BeatmapDifficultyTags.Rhythm,
                DifficultyTagsVersion = StoredBeatmapFacts.DIFFICULTY_TAGS_VERSION,
            };
            TypeBeatBeatmap map(string text) => new TypeBeatBeatmap
            {
                BeatmapInfo = new BeatmapInfo { Ruleset = new TypeBeatRuleset().RulesetInfo },
                HitObjects = new List<TypeBeatHitObject> { new TypeBeatHitObject { Line = phrase(text, 0, 4000) } },
            };
            StoredBeatmapFacts.Apply(info, new TestWorkingBeatmap(map("hello &&&")));
#if DEBUG
            Assert.That(info.DifficultyTags.HasFlag(BeatmapDifficultyTags.Freestyle), Is.True);
            Assert.That(info.DifficultyTagsVersion, Is.EqualTo(StoredBeatmapFacts.DIFFICULTY_TAGS_VERSION));
#else
            Assert.That(info.DifficultyTags, Is.EqualTo(BeatmapDifficultyTags.None));
            Assert.That(info.DifficultyTagsVersion, Is.Zero);
#endif
            StoredBeatmapFacts.Apply(info, new TestWorkingBeatmap(map("hello world")));
            Assert.That(info.DifficultyTags.HasFlag(BeatmapDifficultyTags.Freestyle), Is.False);
        }

        [Test]
        public void ExistingLibrariesAreQueuedAndCachedFlagsSurviveRealmDetach()
        {
            using var context = new SynchronousRealmTestContext();
            string directory = Path.Combine(Path.GetTempPath(), "typebeat-tags-migration-" + Guid.NewGuid().ToString("N"));
            try
            {
                var storage = new NativeStorage(directory);
                Guid id = Guid.Empty;
                using (var realm = Realm.GetInstance(new RealmConfiguration(storage.GetFullPath("client.realm", true)) { SchemaVersion = 64 }))
                {
                    realm.Write(() =>
                    {
                        var ruleset = realm.Add(new RulesetInfo("typebeat", "type!beat", "typebeat.Game.Rulesets.TypeBeat.TypeBeatRuleset, typebeat.Game.Rulesets.TypeBeat", 0));
                        var set = new BeatmapSetInfo();
                        var beatmap = new BeatmapInfo(ruleset, new BeatmapDifficulty(), new BeatmapMetadata()) { BeatmapSet = set, TargetWpm = 120 };
                        set.Beatmaps.Add(beatmap);
                        realm.Add(set);
                        id = beatmap.ID;
                    });
                }
                using var access = new RealmAccess(storage, "client.realm");
                int version = StoredBeatmapFacts.ActiveDifficultyTagsVersion;
                int pending = access.Run(r => r.All<BeatmapInfo>().Count(b => (b.TargetWpm < 0 || b.DifficultyTagsVersion < version) && b.BeatmapSet != null));
#if DEBUG
                Assert.That(pending, Is.EqualTo(1));
#else
                Assert.That(pending, Is.Zero, "release builds must not queue tag-only backfills");
#endif
                access.Write(r =>
                {
                    var beatmap = r.Find<BeatmapInfo>(id)!;
                    beatmap.DifficultyTags = BeatmapDifficultyTags.Rhythm | BeatmapDifficultyTags.Freestyle;
                    beatmap.DifficultyTagsVersion = StoredBeatmapFacts.DIFFICULTY_TAGS_VERSION;
                });
                var cached = access.Run(r => r.Find<BeatmapInfo>(id)!.Detach());
                Assert.That(cached.DifficultyTags, Is.EqualTo(BeatmapDifficultyTags.Rhythm | BeatmapDifficultyTags.Freestyle));
                Assert.That(cached.TargetWpm, Is.EqualTo(120));
                Assert.That(access.Run(r => r.All<BeatmapInfo>().Count(b => b.DifficultyTagsVersion < StoredBeatmapFacts.DIFFICULTY_TAGS_VERSION)), Is.Zero);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        private static IEnumerable<LyricLine> steady(int count, double wordMs, double start = 0)
            => Enumerable.Range(0, count).Select(i => phrase("flame river cider amber", start + i * wordMs * 4, wordMs * 4));

        private static LyricLine phrase(string text, double start, double duration, bool irregular = false, string? original = null)
        {
            var words = text.Split(' ');
            double[] fractions = irregular ? new[] { 0d, .025, .5, .525, 1d } : Enumerable.Range(0, words.Length + 1).Select(i => (double)i / words.Length).ToArray();
            return new LyricLine
            {
                RawText = text,
                Original = original,
                StartTime = start,
                EndTime = start + duration,
                SingEndTime = start + duration,
                Units = words.Select((word, i) => new TimedUnit
                {
                    Text = word,
                    StartTime = start + duration * fractions[i],
                    EndTime = start + duration * fractions[i + 1],
                    SyllableBoundaries = irregular ? Enumerable.Range(1, word.Length - 1)
                        .Select(j => start + duration * (fractions[i] + (fractions[i + 1] - fractions[i]) * j / word.Length)).ToArray() : Array.Empty<double>(),
                    SyllableSplits = irregular ? Enumerable.Range(1, word.Length - 1).ToArray() : Array.Empty<int>(),
                }).ToArray(),
            };
        }
    }
}
