// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using typebeat.Game.Graphics;
using typebeat.Game.Graphics.Sprites;
using osuTK.Graphics;

namespace typebeat.Game.Beatmaps.Drawables
{
    /// <summary>The shared rounded badge used for online status and content tags.</summary>
    public partial class BeatmapPill : CircularContainer
    {
        protected readonly OsuSpriteText PillText;
        protected readonly Box Background;

        public float TextSize
        {
            init => PillText.Font = PillText.Font.With(size: value);
        }

        public MarginPadding TextPadding
        {
            init => PillText.Padding = value;
        }

        public BeatmapPill()
        {
            AutoSizeAxes = Axes.Both;
            Masking = true;
            Children = new Drawable[]
            {
                Background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.Black,
                },
                PillText = new OsuSpriteText
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Font = OsuFont.GetFont(weight: FontWeight.Bold),
                },
            };
            TextPadding = new MarginPadding { Horizontal = 4, Bottom = 1 };
        }
    }
}
