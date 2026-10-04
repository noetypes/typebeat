// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace typebeat.Game.Screens.Edit.GameplayTest
{
    /// <summary>The editor tempo inherited by one Test Play, without adding a gameplay mod.</summary>
    public sealed class EditorTestPlayback
    {
        public double Rate { get; }

        public EditorTestPlayback(double rate)
        {
            Rate = rate;
        }
    }
}
