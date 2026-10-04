// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Audio;
using osu.Framework.Bindables;
using osu.Framework.Timing;
using typebeat.Game.Rulesets.TypeBeat.Beatmaps;
using typebeat.Game.Rulesets.TypeBeat.Gameplay;
using typebeat.Game.Rulesets.TypeBeat.Mods;
using typebeat.Game.Screens.Edit;
using typebeat.Game.Screens.Play;

namespace typebeat.Game.Rulesets.TypeBeat.Tests.NonVisual
{
    [TestFixture]
    public class EditorPlaybackRateTest
    {
        [Test]
        public void EditorOwnsItsPlaybackTempoWhenTheControlsAreRecreated()
        {
            using var clock = new EditorClock();
            clock.PlaybackRate.Value = 0.5;
            Assert.That(clock.AudioAdjustments.AggregateTempo.Value, Is.EqualTo(0.5));
            clock.ResetSpeedAdjustments();
            Assert.That(clock.PlaybackRate.Value, Is.EqualTo(1));
            Assert.That(clock.AudioAdjustments.AggregateTempo.Value, Is.EqualTo(1));
            clock.PlaybackRate.Value = 0.75;
            Assert.That(clock.AudioAdjustments.AggregateTempo.Value, Is.EqualTo(0.75), "reset retains the owned playback adjustment");
        }

        [TestCase(0.25, 1)]
        [TestCase(0.5, 1)]
        [TestCase(0.75, 1)]
        [TestCase(1, 1)]
        [TestCase(0.5, 0.75)]
        [TestCase(0.5, 1.5)]
        [TestCase(1, 0.75)]
        [TestCase(1, 1.5)]
        public void WpmExcludesEditorTempoAndRetainsOrdinaryRateMods(double editorRate, double modRate)
        {
            using var clock = new GameplayClockContainer(new StopwatchClock(), applyOffsets: false, requireDecoupling: false);
            var editorAdjustment = new BindableDouble(editorRate);
            clock.AdjustmentsFromPlayback.AddAdjustment(AdjustableProperty.Tempo, editorAdjustment);
            if (modRate < 1)
                new TypeBeatModHalfTime().ApplyToTrack(clock.AdjustmentsFromMods);
            else if (modRate > 1)
                new TypeBeatModDoubleTime().ApplyToTrack(clock.AdjustmentsFromMods);

            Assert.That(clock.GetTrueGameplayRate(), Is.EqualTo(modRate).Within(1e-6));
            var normalized = run(clock.GetTrueGameplayRate());
            var ordinary = run(modRate);
            Assert.That(normalized.LiveWpm, Is.EqualTo(42 * modRate).Within(1e-8));
            Assert.That(normalized.LiveRollingWpm, Is.EqualTo(ordinary.LiveRollingWpm).Within(1e-8),
                "the top WPM counter reads this value directly; a separate editor tempo must not scale it twice");
        }

        private static TypingEngine run(double wpmRate)
        {
            var map = new LyricBeatmap
            {
                Metadata = new LyricBeatmapMetadata { Artist = "Test", Title = "Rate", FolderPath = "", AudioFileName = "" },
                Granularity = TimingGranularity.Line,
                Lines = new[]
                {
                    new LyricLine
                    {
                        RawText = "ab cd", StartTime = 1000, EndTime = 10000, SingEndTime = 3000,
                        Units = new[]
                        {
                            new TimedUnit { Text = "ab", StartTime = 1000, EndTime = 2000 },
                            new TimedUnit { Text = "cd", StartTime = 2000, EndTime = 3000 },
                        },
                    },
                    new LyricLine
                    {
                        RawText = "ef", StartTime = 10000, EndTime = 12000, SingEndTime = 11000,
                        Units = new[] { new TimedUnit { Text = "ef", StartTime = 10000, EndTime = 11000 } },
                    },
                },
            };
            var engine = new TypingEngine(map);
            engine.Update(0, wpmRate);
            engine.Update(500, wpmRate);
            engine.Update(1000, wpmRate);
            engine.ProcessKey('a', 1000);
            engine.Update(1500, wpmRate);
            engine.ProcessKey('b', 1500);
            engine.Update(2000, wpmRate);
            engine.ProcessKey(' ', 2000);
            engine.ProcessKey('c', 2000);
            engine.Update(2500, wpmRate);
            engine.ProcessKey('d', 2500);
            engine.Update(9000, wpmRate);
            engine.Update(10000, wpmRate);
            engine.ProcessKey('e', 10000);
            engine.Update(10500, wpmRate);
            engine.ProcessKey('f', 10500);
            engine.Update(12000, wpmRate);
            return engine;
        }
    }
}
