// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using typebeat.Game.Beatmaps;

namespace typebeat.Game.Rulesets.TypeBeat.Beatmaps
{
    /// <summary>Describes the authored map using the same windows, rhythm boost and typability index as SR.</summary>
    public static class DifficultyTagClassifier
    {
        public const double RHYTHM_MULTIPLIER_THRESHOLD = 1.07;
        public const double COMPLEX_Z_THRESHOLD = 0;
        public const double MINIMUM_WINDOW_STRAIN = 0.35;

        public static BeatmapDifficultyTags Compute(IEnumerable<LyricLine> source) => ComputeInfo(source).Tags;

        public static BeatmapDifficultyTagInfo ComputeInfo(IEnumerable<LyricLine> source)
        {
            var lines = source.ToList();
            var rating = LyricDifficulty.RateChunked(lines, 1, false, LyricDifficulty.ShippedScores, ChunkedEndurance.Active);
            var report = rating.Report;
            BeatmapDifficultyTags tags = BeatmapDifficultyTags.None;

            if (HasRhythm(report))
                tags |= BeatmapDifficultyTags.Rhythm;

            // Unscored words are unknown, not evidence of difficult spelling.
            if (rating.TypabilityApplied && rating.TypabilityMeanZ < COMPLEX_Z_THRESHOLD)
                tags |= BeatmapDifficultyTags.Complex;

            if (lines.Any(line => line.RawText.Any(Typeability.IsFreestyle)))
                tags |= BeatmapDifficultyTags.Freestyle;

            tags |= TimingTags(report);

            return new BeatmapDifficultyTagInfo(tags)
            {
                BurstWindow = tags.HasFlag(BeatmapDifficultyTags.Burst) ? describe(report.HardestWindow) : null,
                SustainWindow = tags.HasFlag(BeatmapDifficultyTags.Sustain) ? describe(report.SustainedWindow) : null,
                StaminaWindow = tags.HasFlag(BeatmapDifficultyTags.Stamina) ? describe(report.StaminaWindow) : null,
            };
        }

        internal static BeatmapDifficultyTags TimingTags(ChunkedEndurance.Report report)
        {
            BeatmapDifficultyTags tags = BeatmapDifficultyTags.None;
            // Stamina is the longer-duration label. A map qualifying for it does not also need Sustain.
            if (report.DurationSeconds > 180 && report.StaminaWindow.Seconds > ChunkedEndurance.stamina_profile_seconds
                && report.StaminaWindow.Strain >= MINIMUM_WINDOW_STRAIN)
                tags |= BeatmapDifficultyTags.Stamina;
            else if (report.SustainedWindowPeak >= MINIMUM_WINDOW_STRAIN)
                tags |= BeatmapDifficultyTags.Sustain;

            // Burst can accompany either sustained tag whenever the hardest SR window is short.
            if (report.HardestWindow.Seconds > 0 && report.HardestWindow.Seconds <= ChunkedEndurance.short_profile_seconds
                && report.HardestWindow.Strain >= MINIMUM_WINDOW_STRAIN)
                tags |= BeatmapDifficultyTags.Burst;

            return tags;
        }

        internal static bool HasRhythm(ChunkedEndurance.Report report)
            => report.WholeMapRhythmMultiplier >= RHYTHM_MULTIPLIER_THRESHOLD || report.HardestWindowRhythmMultiplier >= RHYTHM_MULTIPLIER_THRESHOLD;

        private static DifficultyTagWindow describe(ChunkedEndurance.WindowProfile window) => new DifficultyTagWindow(window.Wpm, window.Seconds);
    }
}
