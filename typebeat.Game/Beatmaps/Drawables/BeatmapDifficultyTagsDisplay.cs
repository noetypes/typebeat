// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using typebeat.Game.Configuration;
using typebeat.Game.Graphics;
using typebeat.Game.Utils;
using osuTK;

namespace typebeat.Game.Beatmaps.Drawables
{
    public partial class BeatmapDifficultyTagsDisplay : FillFlowContainer<BeatmapDifficultyTagPill>
    {
        private readonly BindableBool showTags = new BindableBool(true);
        private BeatmapDifficultyTags tags;

        public float TextSize { get; init; } = OsuFont.Style.Caption2.Size;
        public MarginPadding TextPadding { get; init; } = new MarginPadding { Horizontal = 4, Bottom = 1 };

        public BeatmapDifficultyTags Tags
        {
            get => tags;
            set
            {
                if (!DevelopmentFeatures.DifficultyTags)
                    value = BeatmapDifficultyTags.None;
                if ((value & BeatmapDifficultyTags.Burst) != 0)
                    value &= ~BeatmapDifficultyTags.Stamina;
                if (tags == value)
                    return;
                tags = value;
                if (IsLoaded)
                    rebuild();
            }
        }

        public BeatmapDifficultyTagsDisplay()
        {
            AutoSizeAxes = Axes.Both;
            Direction = FillDirection.Horizontal;
            Spacing = new Vector2(5, 0);
        }

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager? config) => config?.BindWith(OsuSetting.ShowDifficultyTags, showTags);

        protected override void LoadComplete()
        {
            base.LoadComplete();
            showTags.BindValueChanged(_ => updateVisibility(), true);
            rebuild();
        }

        private void rebuild()
        {
            Clear();
            if (!DevelopmentFeatures.DifficultyTags)
            {
                updateVisibility();
                return;
            }

            foreach (var tag in new[] { BeatmapDifficultyTags.Rhythm, BeatmapDifficultyTags.Complex, BeatmapDifficultyTags.Freestyle, BeatmapDifficultyTags.Burst, BeatmapDifficultyTags.Stamina })
            {
                if ((tags & tag) != 0)
                    Add(new BeatmapDifficultyTagPill(tag) { TextSize = TextSize, TextPadding = TextPadding });
            }
            updateVisibility();
        }

        private void updateVisibility() => Alpha = DevelopmentFeatures.DifficultyTags && showTags.Value && tags != BeatmapDifficultyTags.None ? 1 : 0;
    }
}
