// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Globalization;
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

        public DifficultyTagWindow? Window { get; }

        public BeatmapDifficultyTagPill(BeatmapDifficultyTags tag, DifficultyTagWindow? window = null)
        {
            Tag = tag;
            Window = window;
            PillText.Text = tag.ToString().ToUpperInvariant();
            Background.Colour = ColourFor(tag);
        }

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider? colourProvider)
        {
            PillText.Colour = colourProvider?.Background3 ?? Color4.Black;
        }

        public static Color4 ColourFor(BeatmapDifficultyTags tag) => Color4Extensions.FromHex(tag switch
        {
            BeatmapDifficultyTags.Burst => "FF6969",
            BeatmapDifficultyTags.Rhythm => "C68AFF",
            BeatmapDifficultyTags.Complex => "FFBA66",
            BeatmapDifficultyTags.Freestyle => "69DAB3",
            BeatmapDifficultyTags.Sustain => "75BBFF",
            BeatmapDifficultyTags.Stamina => "FF8CC4",
            _ => "9BD7F5",
        });

        public LocalisableString TooltipText => Window is { } window
            ? $"{window.Wpm.ToString("0", CultureInfo.InvariantCulture)}wpm / {window.Seconds.ToString("0.##", CultureInfo.InvariantCulture)}s - {description}"
            : description;

        private string description => Tag switch
        {
            BeatmapDifficultyTags.Rhythm => "Frequent changes in typing rhythm.",
            BeatmapDifficultyTags.Complex => "Words and letter sequences that are harder to type.",
            BeatmapDifficultyTags.Freestyle => "Includes freestyle characters that accept any input.",
            BeatmapDifficultyTags.Burst => "A short passage is the map's hardest typing window.",
            BeatmapDifficultyTags.Sustain => "Sustained passages demand a consistently high typing pace.",
            BeatmapDifficultyTags.Stamina => "Maps over three minutes with a hard sustained window longer than 45 seconds.",
            _ => string.Empty,
        };
    }
}
