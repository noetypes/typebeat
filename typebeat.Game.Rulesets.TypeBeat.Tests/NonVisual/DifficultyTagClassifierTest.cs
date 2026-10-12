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
using typebeat.Game.Beatmaps.Drawables;
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
        [TestCase("hey kevin whats up", true)]
        [TestCase("with one hand on the door", false)]
        [TestCase("zzqx wvvt qqzz", false)]
        public void ComplexRequiresLowScoredTypability(string text, bool expected)
            => Assert.That(DifficultyTagClassifier.Compute(new[] { phrase(text, 0, 4000) }).HasFlag(BeatmapDifficultyTags.Complex), Is.EqualTo(expected));

        [TestCase(125)]
        [TestCase(500)]
        public void DenseShortPassageIsBurstRatherThanSustain(double wordMs)
        {
            var lines = steady(4, 2000).ToList();
            lines.AddRange(steady(2, wordMs, start: 32000));
            lines.AddRange(steady(4, 2000, start: 32000 + 8 * wordMs));
            var tags = DifficultyTagClassifier.Compute(lines);
            Assert.That(tags.HasFlag(BeatmapDifficultyTags.Burst), Is.True);
            Assert.That(tags.HasFlag(BeatmapDifficultyTags.Sustain), Is.False);
            Assert.That(tags.HasFlag(BeatmapDifficultyTags.Stamina), Is.False);
        }

        [Test]
        public void SustainedDemandIsSustainRatherThanBurst()
        {
            var tags = DifficultyTagClassifier.Compute(steady(40, 500));
            Assert.That(tags.HasFlag(BeatmapDifficultyTags.Sustain), Is.True);
            Assert.That(tags.HasFlag(BeatmapDifficultyTags.Stamina), Is.False);
            Assert.That(tags.HasFlag(BeatmapDifficultyTags.Burst), Is.False);
        }

        [TestCase(40, BeatmapDifficultyTags.Sustain)]
        [TestCase(100, BeatmapDifficultyTags.Stamina)]
        public void BurstCanCoexistWithTheAppropriateSustainedTag(int lineCount, BeatmapDifficultyTags sustainedTag)
        {
            var lines = steady(lineCount, 500).Concat(steady(2, 125, lineCount * 2000)).ToArray();
            var rating = LyricDifficulty.RateChunked(lines, 1, false, LyricDifficulty.ShippedScores, ChunkedEndurance.Active);
            var info = DifficultyTagClassifier.ComputeInfo(lines);
            Assert.That(rating.Report.HardestWindow.Seconds, Is.LessThanOrEqualTo(5));
            Assert.That(info.Tags & (BeatmapDifficultyTags.Burst | BeatmapDifficultyTags.Sustain | BeatmapDifficultyTags.Stamina),
                Is.EqualTo(BeatmapDifficultyTags.Burst | sustainedTag));
            Assert.That(info.BurstWindow, Is.Not.Null);
            Assert.That(info.WindowFor(sustainedTag), Is.Not.Null);
            Assert.That(info.WindowFor(sustainedTag == BeatmapDifficultyTags.Sustain ? BeatmapDifficultyTags.Stamina : BeatmapDifficultyTags.Sustain), Is.Null);
        }

        [TestCase(1.5, true)]
        [TestCase(5, true)]
        [TestCase(5.001, false)]
        [TestCase(12, false)]
        public void BurstNeedsTheHardestWindowToBeShort(double hardestSeconds, bool expected)
        {
            // The highest SR strain must belong to a short window to earn Burst.
            var report = ChunkedEndurance.EmptyReport(ChunkedEndurance.Active) with
            {
                DurationSeconds = 200,
                HardestWindow = new ChunkedEndurance.WindowProfile(0, hardestSeconds * 1000, 0.7, 200),
                ShortWindowPeak = 0.7,
                LongerWindowPeak = 0.6,
                SustainedWindowPeak = 0.4,
                StaminaWindow = new ChunkedEndurance.WindowProfile(0, 48000, 0.4, 160),
            };
            var tags = DifficultyTagClassifier.TimingTags(report);
            Assert.That(tags.HasFlag(BeatmapDifficultyTags.Burst), Is.EqualTo(expected));
            Assert.That(tags.HasFlag(BeatmapDifficultyTags.Stamina), Is.True);
            Assert.That(tags.HasFlag(BeatmapDifficultyTags.Sustain), Is.False);
        }

        [TestCase(0.34, 0.25, false)]
        [TestCase(0.49, 0.4, true)]
        [TestCase(0.5, 0.4, true)]
        [TestCase(0.35, 0.34, true)]
        [TestCase(0.35, 0, true)]
        public void BurstRetainsMinimumStrainWithoutComparingToSustainedDemand(double burstStrain, double sustainedStrain, bool expected)
        {
            var report = ChunkedEndurance.EmptyReport(ChunkedEndurance.Active) with
            {
                HardestWindow = new ChunkedEndurance.WindowProfile(0, 1500, burstStrain, 200),
                ShortWindowPeak = burstStrain,
                SustainedWindowPeak = sustainedStrain,
            };
            Assert.That(DifficultyTagClassifier.TimingTags(report).HasFlag(BeatmapDifficultyTags.Burst), Is.EqualTo(expected));
        }

        [TestCase(100, BeatmapDifficultyTags.Sustain)]
        [TestCase(200, BeatmapDifficultyTags.Stamina)]
        public void BurstCoexistsWithSustainedTagsWithoutADominanceCutoff(double duration, BeatmapDifficultyTags sustainedTag)
        {
            var report = ChunkedEndurance.EmptyReport(ChunkedEndurance.Active) with
            {
                DurationSeconds = duration,
                HardestWindow = new ChunkedEndurance.WindowProfile(0, 1500, 0.4, 200),
                ShortWindowPeak = 0.4,
                SustainedWindowPeak = 0.39,
                StaminaWindow = new ChunkedEndurance.WindowProfile(0, 48000, 0.39, 160),
            };
            Assert.That(DifficultyTagClassifier.TimingTags(report), Is.EqualTo(BeatmapDifficultyTags.Burst | sustainedTag));
        }

        [Test]
        public void ShortSteadyMapIsNotBurstJustBecauseItHasNoLongWindow()
            => Assert.That(DifficultyTagClassifier.Compute(steady(4, 500)).HasFlag(BeatmapDifficultyTags.Burst), Is.False);

        [Test]
        public void LongSparseMapDoesNotGetStaminaJustForItsLength()
            => Assert.That(DifficultyTagClassifier.Compute(steady(40, 10000)) & (BeatmapDifficultyTags.Burst | BeatmapDifficultyTags.Sustain | BeatmapDifficultyTags.Stamina), Is.EqualTo(BeatmapDifficultyTags.None));

        [Test]
        public void IrregularTimingGetsRhythm()
        {
            var lines = Enumerable.Range(0, 20).Select(i => phrase("flame river cider amber", i * 4000, 4000, irregular: true)).ToArray();
            var plain = Enumerable.Range(0, 20).Select(i => phrase("flame river cider amber", i * 4000, 4000)).ToArray();
            var irregular = LyricDifficulty.RateChunked(lines, 1, false, LyricDifficulty.ShippedScores, ChunkedEndurance.Active).Report;
            var regular = LyricDifficulty.RateChunked(plain, 1, false, LyricDifficulty.ShippedScores, ChunkedEndurance.Active).Report;
            TestContext.WriteLine($"Rhythm multipliers: irregular {irregular.RhythmMultiplier}, regular {regular.RhythmMultiplier}");
            Assert.That(DifficultyTagClassifier.Compute(lines).HasFlag(BeatmapDifficultyTags.Rhythm), Is.True);
            Assert.That(DifficultyTagClassifier.Compute(plain).HasFlag(BeatmapDifficultyTags.Rhythm), Is.False);
        }

        [TestCase(89, false)]
        [TestCase(90, false)]
        [TestCase(91, true)]
        public void StaminaRequiresMoreThanThreeMinutes(int lines, bool expected)
        {
            var info = DifficultyTagClassifier.ComputeInfo(steady(lines, 500));
            Assert.That(info.Tags.HasFlag(BeatmapDifficultyTags.Sustain), Is.EqualTo(!expected));
            Assert.That(info.Tags.HasFlag(BeatmapDifficultyTags.Stamina), Is.EqualTo(expected));
            if (expected)
                Assert.That(info.StaminaWindow!.Value.Seconds, Is.GreaterThan(45));
            else
                Assert.That(info.StaminaWindow, Is.Null);
        }

        [Test]
        public void LongMapNeedsAHardWindowOverFortyFiveSeconds()
        {
            var lines = steady(10, 500).Concat(steady(1, 10000, 180000)).ToArray();
            var report = LyricDifficulty.RateChunked(lines, 1, false, LyricDifficulty.ShippedScores, ChunkedEndurance.Active).Report;
            Assert.That(report.DurationSeconds, Is.GreaterThan(180));
            Assert.That(report.SustainedWindowPeak, Is.GreaterThanOrEqualTo(DifficultyTagClassifier.MINIMUM_WINDOW_STRAIN));
            Assert.That(report.StaminaWindow.Strain, Is.LessThan(DifficultyTagClassifier.MINIMUM_WINDOW_STRAIN));
            Assert.That(DifficultyTagClassifier.Compute(lines).HasFlag(BeatmapDifficultyTags.Stamina), Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RhythmCanComeFromTheHardPartOrTheWholeMap(bool wholeMap)
        {
            var lines = wholeMap
                ? steady(4, 50).Concat(Enumerable.Range(0, 100).Select(i => phrase("flame river cider amber", 10000 + i * 4000, 4000, irregular: true))).ToArray()
                : steady(40, 2000).Concat(Enumerable.Range(0, 4).Select(i => phrase("flame river cider amber", 320000 + i * 2000, 2000, irregular: true))).ToArray();
            var report = LyricDifficulty.RateChunked(lines, 1, false, LyricDifficulty.ShippedScores, ChunkedEndurance.Active).Report;
            TestContext.WriteLine($"Whole-map case {wholeMap}: whole {report.WholeMapRhythmMultiplier}, hard {report.HardestWindowRhythmMultiplier}");
            Assert.That(wholeMap ? report.WholeMapRhythmMultiplier : report.HardestWindowRhythmMultiplier, Is.GreaterThanOrEqualTo(DifficultyTagClassifier.RHYTHM_MULTIPLIER_THRESHOLD));
            if (!wholeMap)
                Assert.That(report.WholeMapRhythmMultiplier, Is.LessThan(DifficultyTagClassifier.RHYTHM_MULTIPLIER_THRESHOLD));
            Assert.That(DifficultyTagClassifier.Compute(lines).HasFlag(BeatmapDifficultyTags.Rhythm), Is.True);
        }

        [TestCase(1, 1, false)]
        [TestCase(1.069, 1.069, false)]
        [TestCase(1.07, 1, true)]
        [TestCase(1, 1.07, true)]
        [TestCase(1.1, 1.1, true)]
        public void EitherRhythmReadingCanMeetTheCutoff(double whole, double hard, bool expected)
        {
            var report = ChunkedEndurance.EmptyReport(ChunkedEndurance.Active) with { WholeMapRhythmMultiplier = whole, HardestWindowRhythmMultiplier = hard };
            Assert.That(DifficultyTagClassifier.HasRhythm(report), Is.EqualTo(expected));
        }

        [Test]
        public void AbracadabraGetsRhythmFromItsHardPart()
        {
            LyricBeatmapDecoder.Register();
            using var stream = File.OpenRead(StandaloneMaps.Require("Lady Gaga - Abracadabra", "Lady Gaga - Abracadabra (Noe) [Lunatic].osu"));
            using var reader = new typebeat.Game.IO.LineBufferedReader(stream);
            var beatmap = (Beatmap)typebeat.Game.Beatmaps.Formats.Decoder.GetDecoder<Beatmap>(reader).Decode(reader);
            var lines = beatmap.HitObjects.OfType<TypeBeatHitObject>().Select(h => h.Line).ToArray();
            var report = LyricDifficulty.RateChunked(lines, 1, false, LyricDifficulty.ShippedScores, ChunkedEndurance.Active).Report;
            Assert.That(report.WholeMapRhythmMultiplier, Is.LessThan(DifficultyTagClassifier.RHYTHM_MULTIPLIER_THRESHOLD));
            Assert.That(report.HardestWindowRhythmMultiplier, Is.GreaterThanOrEqualTo(DifficultyTagClassifier.RHYTHM_MULTIPLIER_THRESHOLD));
            Assert.That(DifficultyTagClassifier.Compute(lines).HasFlag(BeatmapDifficultyTags.Rhythm), Is.True);
        }

        [Test]
        public void TimingTagsExplainTheirMeasuredWindow()
        {
            var lines = steady(4, 2000).Concat(steady(2, 125, 32000)).Concat(steady(4, 2000, 33000)).ToArray();
            var burst = DifficultyTagClassifier.ComputeInfo(lines);
            var sustain = DifficultyTagClassifier.ComputeInfo(steady(40, 500));
            var stamina = DifficultyTagClassifier.ComputeInfo(steady(100, 500));
            Assert.That(burst.BurstWindow!.Value.Seconds, Is.LessThanOrEqualTo(5));
            Assert.That(sustain.SustainWindow!.Value.Seconds, Is.GreaterThanOrEqualTo(20));
            Assert.That(stamina.StaminaWindow!.Value.Seconds, Is.GreaterThan(45));
            foreach (var (info, tag) in new[] { (burst, BeatmapDifficultyTags.Burst), (sustain, BeatmapDifficultyTags.Sustain), (stamina, BeatmapDifficultyTags.Stamina) })
            {
                Assert.That(info.WindowFor(tag)!.Value.Wpm, Is.GreaterThan(0));
                Assert.That(new BeatmapDifficultyTagPill(tag, info.WindowFor(tag)).TooltipText.ToString(), Does.Match(@"^\d+wpm / \d+(\.\d+)?s - "));
            }
            Assert.That(new BeatmapDifficultyTagPill(BeatmapDifficultyTags.Burst, new DifficultyTagWindow(233, 1.5)).TooltipText.ToString(), Is.EqualTo("233wpm / 1.5s - A short passage is the map's hardest typing window."));
        }

        [Test]
        public void EveryTagHasADifferentColour()
        {
            var colours = Enum.GetValues<BeatmapDifficultyTags>().Where(t => t != BeatmapDifficultyTags.None).Select(BeatmapDifficultyTagPill.ColourFor).ToArray();
            Assert.That(colours.Distinct().Count(), Is.EqualTo(colours.Length));
            var burst = BeatmapDifficultyTagPill.ColourFor(BeatmapDifficultyTags.Burst);
            Assert.That(burst.R, Is.GreaterThan(burst.G).And.GreaterThan(burst.B));
            var rhythm = BeatmapDifficultyTagPill.ColourFor(BeatmapDifficultyTags.Rhythm);
            Assert.That(rhythm.B, Is.GreaterThan(rhythm.G));
            Assert.That(rhythm.R, Is.GreaterThan(rhythm.G));
        }

        [Test]
        public void ApplyingFactsReplacesTagsAfterAnEdit()
        {
            var info = new BeatmapInfo
            {
                DifficultyTags = BeatmapDifficultyTags.Rhythm,
                BurstWindowWpm = 233,
                BurstWindowSeconds = 1.5,
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
            Assert.That(info.DifficultyTagInfo.BurstWindow, Is.Null, "edits must clear stale measured windows too");
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
                using (var realm = Realm.GetInstance(new RealmConfiguration(storage.GetFullPath("client.realm", true)) { SchemaVersion = 65 }))
                {
                    realm.Write(() =>
                    {
                        var ruleset = realm.Add(new RulesetInfo("typebeat", "type!beat", "typebeat.Game.Rulesets.TypeBeat.TypeBeatRuleset, typebeat.Game.Rulesets.TypeBeat", 0));
                        var set = new BeatmapSetInfo();
                        var beatmap = new BeatmapInfo(ruleset, new BeatmapDifficulty(), new BeatmapMetadata()) { BeatmapSet = set, TargetWpm = 120, DifficultyTags = BeatmapDifficultyTags.Sustain, DifficultyTagsVersion = StoredBeatmapFacts.DIFFICULTY_TAGS_VERSION - 1 };
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
                    beatmap.DifficultyTagInfo = new BeatmapDifficultyTagInfo(BeatmapDifficultyTags.Rhythm | BeatmapDifficultyTags.Freestyle | BeatmapDifficultyTags.Burst)
                    {
                        BurstWindow = new DifficultyTagWindow(233, 1.5),
                    };
                    beatmap.DifficultyTagsVersion = StoredBeatmapFacts.DIFFICULTY_TAGS_VERSION;
                });
                var cached = access.Run(r => r.Find<BeatmapInfo>(id)!.Detach());
                Assert.That(cached.DifficultyTags, Is.EqualTo(BeatmapDifficultyTags.Rhythm | BeatmapDifficultyTags.Freestyle | BeatmapDifficultyTags.Burst));
                Assert.That(cached.DifficultyTagInfo.BurstWindow, Is.EqualTo(new DifficultyTagWindow(233, 1.5)));
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
