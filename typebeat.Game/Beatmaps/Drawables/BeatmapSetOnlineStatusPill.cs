// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Extensions;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Localisation;
using typebeat.Game.Graphics;
using typebeat.Game.Localisation;
using typebeat.Game.Overlays;
using osuTK.Graphics;

namespace typebeat.Game.Beatmaps.Drawables
{
    public partial class BeatmapSetOnlineStatusPill : BeatmapPill, IHasTooltip
    {
        /// <summary>
        /// Whether to show <see cref="BeatmapOnlineStatus.None"/> as "unknown" instead of fading out.
        /// </summary>
        public bool ShowUnknownStatus { get; init; }

        /// <summary>
        /// Whether changing status performs transition transforms.
        /// </summary>
        public bool Animated { get; init; } = true;

        public BeatmapOnlineStatus Status
        {
            get => status;
            set
            {
                if (status == value)
                    return;

                status = value;

                if (IsLoaded)
                    updateState();
            }
        }

        private BeatmapOnlineStatus status;

        private const double animation_duration = 400;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        [Resolved(CanBeNull = true)]
        private OverlayColourProvider? colourProvider { get; set; }

        public BeatmapSetOnlineStatusPill()
        {
            Alpha = 0;

            Status = BeatmapOnlineStatus.None;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            updateState();
            FinishTransforms(true);
        }

        private void updateState()
        {
            double duration = Animated ? animation_duration : 0;

            if (Status == BeatmapOnlineStatus.None && !ShowUnknownStatus)
            {
                this.FadeOut(duration, Easing.OutQuint);
                return;
            }

            // The autosize animation on this component is intended to animate horizontal sizing only.
            // To avoid vertical autosize animating from zero to non-zero, only apply the duration
            // after we have a valid size.
            if (Height > 0)
            {
                AutoSizeDuration = (float)duration;
                AutoSizeEasing = Easing.OutQuint;
            }

            this.FadeIn(duration, Easing.OutQuint);

            // Handle the case where transition from hidden to non-hidden may cause
            // a fade from a colour that doesn't make sense (due to not being able to see the previous colour).
            if (Alpha == 0)
                duration = 0;

            Color4 statusTextColour;

            if (colourProvider != null)
                statusTextColour = status == BeatmapOnlineStatus.Graveyard ? colourProvider.Background1 : colourProvider.Background3;
            else
                statusTextColour = status == BeatmapOnlineStatus.Graveyard ? colours.GreySeaFoamLight : Color4.Black;

            PillText.FadeColour(statusTextColour, duration, Easing.OutQuint);
            Background.FadeColour(OsuColour.ForBeatmapSetOnlineStatus(Status) ?? colourProvider?.Light1 ?? colours.GreySeaFoamLighter, duration, Easing.OutQuint);

            PillText.Text = Status.GetLocalisableDescription().ToUpper();
        }

        public LocalisableString TooltipText
        {
            get
            {
                switch (Status)
                {
                    case BeatmapOnlineStatus.LocallyModified:
                        return SongSelectStrings.LocallyModifiedTooltip;
                }

                return string.Empty;
            }
        }
    }
}
