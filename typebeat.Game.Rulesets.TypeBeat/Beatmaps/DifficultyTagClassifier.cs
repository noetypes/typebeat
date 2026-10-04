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
        public const double RHYTHM_MULTIPLIER_THRESHOLD = 1.08;
        public const double COMPLEX_Z_THRESHOLD = -0.5;
        public const double MINIMUM_WINDOW_STRAIN = 0.35;
        public const double BURST_DOMINANCE = 1.25;

        public static BeatmapDifficultyTags Compute(IEnumerable<LyricLine> source)
        {
            var lines = source.ToList();
            var rating = LyricDifficulty.RateChunked(lines, 1, false, LyricDifficulty.ShippedScores, ChunkedEndurance.Live);
            var report = rating.Report;
            BeatmapDifficultyTags tags = BeatmapDifficultyTags.None;

            if (report.RhythmMultiplier >= RHYTHM_MULTIPLIER_THRESHOLD)
                tags |= BeatmapDifficultyTags.Rhythm;

            // Unscored words are unknown, not evidence of difficult spelling.
            if (rating.TypabilityApplied && rating.TypabilityMeanZ <= COMPLEX_Z_THRESHOLD)
                tags |= BeatmapDifficultyTags.Complex;

            if (lines.Any(line => line.RawText.Any(Typeability.IsFreestyle)))
                tags |= BeatmapDifficultyTags.Freestyle;

            // Compare to sustained windows when the map has them. Short maps use their longer
            // supported windows instead, so a steady short map is not automatically a burst map.
            double sustainedPeak = report.SustainedWindowPeak > 0 ? report.SustainedWindowPeak : report.LongerWindowPeak;
            if (report.ShortWindowPeak >= MINIMUM_WINDOW_STRAIN
                && sustainedPeak > 0
                && report.ShortWindowPeak >= sustainedPeak * BURST_DOMINANCE)
                tags |= BeatmapDifficultyTags.Burst;
            else if (report.SustainedWindowPeak >= MINIMUM_WINDOW_STRAIN)
                tags |= BeatmapDifficultyTags.Stamina;

            return tags;
        }
    }
}
