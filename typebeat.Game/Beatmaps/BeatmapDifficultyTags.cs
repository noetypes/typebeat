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
        Stamina = 16,
    }

    public interface IHasDifficultyTags
    {
        /// <summary>Classifies the map at its authored speed. Compute off the update thread.</summary>
        BeatmapDifficultyTags GetDifficultyTags();
    }
}
