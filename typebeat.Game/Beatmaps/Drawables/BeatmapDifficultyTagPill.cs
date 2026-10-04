// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Localisation;
using typebeat.Game.Overlays;
using osuTK.Graphics;

namespace typebeat.Game.Beatmaps.Drawables
{
    public partial class BeatmapDifficultyTagPill : BeatmapPill, IHasTooltip
    {
        public BeatmapDifficultyTags Tag { get; }

        public BeatmapDifficultyTagPill(BeatmapDifficultyTags tag)
        {
            Tag = tag;
            PillText.Text = tag.ToString().ToUpperInvariant();
            Background.Colour = Color4Extensions.FromHex("9BD7F5");
        }

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider? colourProvider)
        {
            PillText.Colour = colourProvider?.Background3 ?? Color4.Black;
        }

        public LocalisableString TooltipText => Tag switch
        {
            BeatmapDifficultyTags.Rhythm => "Frequent changes in typing rhythm.",
            BeatmapDifficultyTags.Complex => "Words and letter sequences that are harder to type.",
            BeatmapDifficultyTags.Freestyle => "Includes freestyle characters that accept any input.",
            BeatmapDifficultyTags.Burst => "Short passages are much harder than sustained stretches.",
            BeatmapDifficultyTags.Stamina => "Sustained passages demand a consistently high typing pace.",
            _ => string.Empty,
        };
    }
}
