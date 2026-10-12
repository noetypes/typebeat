// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace typebeat.Game.Utils
{
    /// <summary>Work-in-progress features available only in local Debug builds, never Release builds.</summary>
    public static class DevelopmentFeatures
    {
#if DEBUG
        public static bool DifficultyTags => true;
        public static bool NormalizeEditorStatistics => true;
        public static bool ExperimentalDifficulty => true;
#else
        public static bool DifficultyTags => false;
        public static bool NormalizeEditorStatistics => false;
        public static bool ExperimentalDifficulty => false;
#endif
    }
}
