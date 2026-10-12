// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace typebeat.Game.Beatmaps
{
    [Flags]
    public enum BeatmapDifficultyTags
    {
        None = 0,
        Rhythm = 1,
        Complex = 2,
        Freestyle = 4,
        Burst = 8,
        Sustain = 16,
        Stamina = 32,
    }

    /// <summary>The pace and length of the SR window responsible for a timing tag.</summary>
    public readonly record struct DifficultyTagWindow(double Wpm, double Seconds);

    public sealed record BeatmapDifficultyTagInfo(BeatmapDifficultyTags Tags)
    {
        public static readonly BeatmapDifficultyTagInfo Empty = new BeatmapDifficultyTagInfo(BeatmapDifficultyTags.None);
        public DifficultyTagWindow? BurstWindow { get; init; }
        public DifficultyTagWindow? SustainWindow { get; init; }
        public DifficultyTagWindow? StaminaWindow { get; init; }

        public DifficultyTagWindow? WindowFor(BeatmapDifficultyTags tag) => tag switch
        {
            BeatmapDifficultyTags.Burst => BurstWindow,
            BeatmapDifficultyTags.Sustain => SustainWindow,
            BeatmapDifficultyTags.Stamina => StaminaWindow,
            _ => null,
        };
    }

    public interface IHasDifficultyTags
    {
        /// <summary>Classifies the map at its authored speed. Compute off the update thread.</summary>
        BeatmapDifficultyTags GetDifficultyTags();

        BeatmapDifficultyTagInfo GetDifficultyTagInfo();
    }
}
