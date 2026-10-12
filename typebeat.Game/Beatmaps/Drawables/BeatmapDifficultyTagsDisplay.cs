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
        private BeatmapDifficultyTagInfo info = BeatmapDifficultyTagInfo.Empty;

        public float TextSize { get; init; } = OsuFont.Style.Caption2.Size;
        public MarginPadding TextPadding { get; init; } = new MarginPadding { Horizontal = 4, Bottom = 1 };

        public BeatmapDifficultyTags Tags
        {
            get => info.Tags;
            set => Info = new BeatmapDifficultyTagInfo(value);
        }

        public BeatmapDifficultyTagInfo Info
        {
            get => info;
            set
            {
                if (!DevelopmentFeatures.DifficultyTags)
                    value = BeatmapDifficultyTagInfo.Empty;
                if ((value.Tags & BeatmapDifficultyTags.Burst) != 0)
                    value = value with { Tags = value.Tags & ~(BeatmapDifficultyTags.Sustain | BeatmapDifficultyTags.Stamina) };
                if (info == value)
                    return;
                info = value;
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

            foreach (var tag in new[] { BeatmapDifficultyTags.Rhythm, BeatmapDifficultyTags.Complex, BeatmapDifficultyTags.Freestyle, BeatmapDifficultyTags.Burst, BeatmapDifficultyTags.Sustain, BeatmapDifficultyTags.Stamina })
            {
                if ((Tags & tag) != 0)
                    Add(new BeatmapDifficultyTagPill(tag, info.WindowFor(tag)) { TextSize = TextSize, TextPadding = TextPadding });
            }
            updateVisibility();
        }

        private void updateVisibility() => Alpha = DevelopmentFeatures.DifficultyTags && showTags.Value && Tags != BeatmapDifficultyTags.None ? 1 : 0;
    }
}
