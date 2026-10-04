// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using typebeat.Game.Configuration;
using typebeat.Game.Beatmaps;
using typebeat.Game.Beatmaps.Drawables;
using typebeat.Game.Database;
using typebeat.Game.Graphics.Carousel;
using typebeat.Game.Graphics.UserInterface;
using typebeat.Game.Localisation;
using typebeat.Game.Overlays;
using typebeat.Game.Rulesets;
using typebeat.Game.Screens.Select;
using typebeat.Game.Screens.Select.Filter;
using typebeat.Game.Tests.Visual;
using typebeat.Game.Rulesets.TypeBeat.Beatmaps;

namespace typebeat.Game.Rulesets.TypeBeat.Tests.Visual
{
    /// <summary>
    /// "Use on game intro" is a per-SET toggle: it is offered when right-clicking the set panel (and the
    /// standalone panel, which stands for its set when difficulties are not grouped), and no longer on a
    /// difficulty panel, whose menu is <see cref="SoloSongSelect.GetForwardActions"/>. Ticking it writes
    /// <see cref="BeatmapSetInfo.IntroPoolInclusion"/>.
    /// </summary>
    [TestFixture]
    public partial class TestSceneIntroPoolSetToggle : ScreenTestScene
    {
        [Resolved]
        private RealmAccess realm { get; set; } = null!;

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        // OsuGame provides this to song select's carousel; the test browser's game base does not.
        [Cached(typeof(BeatmapStore))]
        private readonly RealmDetachedBeatmapStore beatmapStore = new RealmDetachedBeatmapStore();

        private SoloSongSelect songSelect = null!;
        private BeatmapSetInfo beatmapSet = null!;

        private PanelBeatmapSet setPanel = null!;
        private PanelBeatmap difficultyPanel = null!;
        private PanelBeatmapStandalone standalonePanel = null!;

        [BackgroundDependencyLoader]
        private void load() => Add(beatmapStore);

        public override void SetUpSteps()
        {
            base.SetUpSteps();

            AddStep("add a two difficulty set", () => beatmapSet = realm.Write(r =>
            {
                var ruleset = r.Find<RulesetInfo>("typebeat")!;
                var set = new BeatmapSetInfo();

                foreach (string name in new[] { "Normal", "Hard" })
                {
                    set.Beatmaps.Add(new BeatmapInfo(ruleset, new BeatmapDifficulty(), new BeatmapMetadata { Title = "intro toggle", Artist = "test" })
                    {
                        DifficultyName = name,
                        BeatmapSet = set,
                        StarRating = name == "Hard" ? 5 : 2,
                        Status = BeatmapOnlineStatus.Ranked,
                        DifficultyTags = name == "Hard" ? BeatmapDifficultyTags.Rhythm | BeatmapDifficultyTags.Stamina : BeatmapDifficultyTags.Freestyle | BeatmapDifficultyTags.Burst,
                        DifficultyTagsVersion = StoredBeatmapFacts.DIFFICULTY_TAGS_VERSION,
                    });
                }

                return r.Add(set).Detach();
            }));

            AddStep("show tags", () => config.SetValue(OsuSetting.ShowDifficultyTags, true));

            AddStep("load song select", () => LoadScreen(songSelect = new SoloSongSelect()));
            AddUntilStep("song select loaded", () => songSelect.IsLoaded);

            AddStep("add panels", () =>
            {
                var beatmap = beatmapSet.Beatmaps.First();

                // Added beside the screen stack rather than replacing it, which would dispose song select.
                Add(new DependencyProvidingContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    CachedDependencies = new (Type, object)[]
                    {
                        (typeof(ISongSelect), songSelect),
                        (typeof(OverlayColourProvider), new OverlayColourProvider(OverlayColourScheme.Blue)),
                    },
                    Child = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Children = new Drawable[]
                        {
                            setPanel = new PanelBeatmapSet { Item = new CarouselItem(new GroupedBeatmapSet(null, beatmapSet)) },
                            difficultyPanel = new PanelBeatmap { Item = new CarouselItem(new GroupedBeatmap(null, beatmap)) },
                            standalonePanel = new PanelBeatmapStandalone { Item = new CarouselItem(new GroupedBeatmap(null, beatmap)) },
                        }
                    }
                });
            });

            AddUntilStep("panels loaded", () => setPanel.IsLoaded && difficultyPanel.IsLoaded && standalonePanel.IsLoaded);
        }

        [Test]
        public void ToggleIsOnTheSetPanelOnly()
        {
            AddAssert("set panel offers the toggle", () => introToggle(setPanel.ContextMenuItems) != null);
            AddAssert("standalone panel offers the toggle", () => introToggle(standalonePanel.ContextMenuItems) != null);
            AddAssert("difficulty panel does not", () => introToggle(difficultyPanel.ContextMenuItems) == null);
            AddAssert("difficulty panel still has its forward actions", () => difficultyPanel.ContextMenuItems!.Length > 0);
        }

        [Test]
        public void SetToggleSlotsBetweenDetailsAndCollections()
        {
            AddAssert("toggle sits directly above collections", () =>
            {
                var items = setPanel.ContextMenuItems!;
                int toggle = Array.FindIndex(items, i => i is ToggleMenuItem t && t.Text.Value.Equals(SongSelectStrings.UseOnGameIntro));
                int collections = Array.FindIndex(items, i => i.Text.Value.Equals(CommonStrings.Collections));

                // toggle, spacer, collections.
                return toggle >= 0 && collections == toggle + 2;
            });
        }

        [Test]
        public void TickingTheSetToggleWritesTheSet()
        {
            AddAssert("no override yet", () => realm.Run(r => r.Find<BeatmapSetInfo>(beatmapSet.ID)!.IntroPoolInclusion) == null);

            // No difficulty declares a beatdrop (these have no files at all), so the toggle shows unticked
            // and ticking it opts the whole set in.
            AddAssert("shown unticked", () => introToggle(setPanel.ContextMenuItems)!.State.Value == false);
            AddStep("tick it", () => introToggle(setPanel.ContextMenuItems)!.Action.Value?.Invoke());

            AddAssert("set opted in", () => realm.Run(r => r.Find<BeatmapSetInfo>(beatmapSet.ID)!.IntroPoolInclusion) == true);
            AddAssert("difficulties untouched", () => realm.Run(r => r.Find<BeatmapSetInfo>(beatmapSet.ID)!.Beatmaps.All(b => b.UserSettings.IntroPoolInclusion == null)));
            AddAssert("reopened menu shows it ticked", () => introToggle(setPanel.ContextMenuItems)!.State.Value);
            AddAssert("standalone panel agrees", () => introToggle(standalonePanel.ContextMenuItems)!.State.Value);
        }

        /// <summary>
        /// Backlog 335: with song select grouped by "Enabled on intro", ticking the toggle moves the set at once. The
        /// toggle writes <see cref="BeatmapSetInfo.IntroPoolInclusion"/>; <see cref="RealmDetachedBeatmapStore"/>'s
        /// subscription on the sets sees the modified set and REPLACES its detached copy, the carousel swaps the new
        /// difficulties into its items, and any change to those items queues a refilter, which regroups.
        /// </summary>
        [Test]
        public void TogglingRegroupsWhileGroupedByIntro()
        {
            AddStep("group by enabled on intro", () => config.SetValue(OsuSetting.SongSelectGroupMode, GroupMode.EnabledOnIntro));

            // No difficulty declares a beatdrop, so before any override the set was never a candidate: "No intro set",
            // not "Not on intro" (which is for sets with an authored beatdrop the user turned off).
            AddUntilStep("set starts under no intro set", () => groupOfSet(), () => Is.EqualTo("No intro set"));

            AddStep("tick it", () => introToggle(setPanel.ContextMenuItems)!.Action.Value?.Invoke());
            AddUntilStep("set moved to enabled on intro", () => groupOfSet(), () => Is.EqualTo("Enabled on intro"));

            AddStep("untick it", () => introToggle(setPanel.ContextMenuItems)!.Action.Value?.Invoke());
            AddUntilStep("set moved back", () => groupOfSet(), () => Is.EqualTo("No intro set"));

            AddStep("restore grouping", () => config.SetValue(OsuSetting.SongSelectGroupMode, GroupMode.None));
        }

        /// <summary>
        /// Backlog 348: the difficulty spread button inherited from osu! is gone from both panels (it grouped its dots
        /// by ruleset, and type!beat has one). The only button left on either panel is the update button, the set
        /// panel's status row holds just the status pill and that button, and the standalone panel's star row holds
        /// just the star rating. Rides on this scene because it already loads a multi-difficulty set in both panels.
        /// </summary>
        [Test]
        public void PanelsCarryNoSpreadControl()
        {
            AddAssert("set panel's only button is the update button",
                () => setPanel.ChildrenOfType<OsuAnimatedButton>().All(b => b is PanelUpdateBeatmapButton));
            AddAssert("standalone panel's only button is the update button",
                () => standalonePanel.ChildrenOfType<OsuAnimatedButton>().All(b => b is PanelUpdateBeatmapButton));

            AddAssert("set status row is status, tags, then update button", () =>
            {
                var row = (FillFlowContainer)setPanel.ChildrenOfType<BeatmapSetOnlineStatusPill>().Single().Parent!;
                return row.Children.Count == 3 && row.Children[0] is BeatmapSetOnlineStatusPill && row.Children[1] is BeatmapDifficultyTagsDisplay && row.Children[2] is PanelUpdateBeatmapButton;
            });

            AddAssert("standalone star row is the star rating alone", () =>
            {
                var row = (FillFlowContainer)standalonePanel.ChildrenOfType<StarRatingDisplay>().Single().Parent!;
                return row.Children.Count == 1;
            });
        }

        [Test]
        public void TagsFollowTheSelectedDifficultyAndToggleTogether()
        {
            BeatmapInfo normal = null!, hard = null!;
            AddStep("get difficulty fixtures", () => { normal = beatmapSet.Beatmaps.First(); hard = beatmapSet.Beatmaps.Last(); });
            AddAssert("set uses selected or hardest difficulty", () => tagsOf(setPanel).Tags == (Beatmap.Value.BeatmapInfo.ID == normal.ID ? normal : hard).DifficultyTags);
            AddAssert("standalone uses its difficulty", () => tagsOf(standalonePanel).Tags == normal.DifficultyTags);
            AddStep("select normal", () => Beatmap.Value = CreateWorkingBeatmap(new TypeBeatBeatmap { BeatmapInfo = normal }));
            AddUntilStep("set tags follow normal", () => tagsOf(setPanel).Tags == normal.DifficultyTags);
            AddUntilStep("title tags follow normal", () => tagsOf(songSelect.ChildrenOfType<BeatmapTitleWedge>().Single()).Tags == normal.DifficultyTags);
            AddStep("select hard", () => Beatmap.Value = CreateWorkingBeatmap(new TypeBeatBeatmap { BeatmapInfo = hard }));
            AddUntilStep("set tags follow hard", () => tagsOf(setPanel).Tags == hard.DifficultyTags);
            AddUntilStep("title tags follow hard", () => tagsOf(songSelect.ChildrenOfType<BeatmapTitleWedge>().Single()).Tags == hard.DifficultyTags);
            AddStep("hide tags", () => config.SetValue(OsuSetting.ShowDifficultyTags, false));
            AddUntilStep("all tag rows hidden", () => tagsOf(setPanel).Alpha == 0 && tagsOf(standalonePanel).Alpha == 0 && tagsOf(songSelect.ChildrenOfType<BeatmapTitleWedge>().Single()).Alpha == 0);
            AddStep("show tags", () => config.SetValue(OsuSetting.ShowDifficultyTags, true));
            AddUntilStep("all tag rows shown", () => tagsOf(setPanel).Alpha == 1 && tagsOf(standalonePanel).Alpha == 1 && tagsOf(songSelect.ChildrenOfType<BeatmapTitleWedge>().Single()).Alpha == 1);
        }

        [Test]
        public void ShowTagsButtonControlsVisibility()
        {
            ShearedToggleButton button = null!;
            bool converts = false;
            AddStep("find show tags button", () =>
            {
                button = songSelect.ChildrenOfType<FilterControl>().Single().ChildrenOfType<ShearedToggleButton>().Single(b => b.Text.ToString() == "Show tags");
                converts = config.Get<bool>(OsuSetting.ShowConvertedBeatmaps);
            });
            AddStep("collect transient bindables", () => { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); });
            AddUntilStep("button loaded and ready", () => button.Active.Value && button.Action != null);
            AddStep("toggle off", () => button.Action!.Invoke());
            AddAssert("button disabled tags in config", () => !config.Get<bool>(OsuSetting.ShowDifficultyTags));
            AddUntilStep("tag row hidden", () => tagsOf(standalonePanel).Alpha == 0);
            AddAssert("converts setting unchanged", () => config.Get<bool>(OsuSetting.ShowConvertedBeatmaps) == converts);
            AddAssert("conversion filtering unchanged", () => songSelect.ChildrenOfType<FilterControl>().Single().CreateCriteria().AllowConvertedBeatmaps == converts);
            AddStep("toggle on", () => button.Action?.Invoke());
            AddUntilStep("tag row shown", () => tagsOf(standalonePanel).Alpha == 1);
        }

        [Test]
        public void TagsPushUpdateButtonToTheRight()
        {
            float setX = 0, standaloneX = 0;
            AddStep("show update buttons", () =>
            {
                foreach (var button in new[] { setPanel.ChildrenOfType<PanelUpdateBeatmapButton>().Single(), standalonePanel.ChildrenOfType<PanelUpdateBeatmapButton>().Single() })
                {
                    button.FinishTransforms();
                    button.Alpha = 1;
                }
            });
            AddUntilStep("tag rows have a width", () => tagsOf(setPanel).DrawWidth > 0 && tagsOf(standalonePanel).DrawWidth > 0);
            AddStep("record update positions", () =>
            {
                setX = setPanel.ChildrenOfType<PanelUpdateBeatmapButton>().Single().X;
                standaloneX = standalonePanel.ChildrenOfType<PanelUpdateBeatmapButton>().Single().X;
            });
            AddAssert("tags follow status", () => tagsOf(setPanel).X >= setPanel.ChildrenOfType<BeatmapSetOnlineStatusPill>().Single().DrawWidth);
            AddAssert("update follows tags", () => setX >= tagsOf(setPanel).X + tagsOf(setPanel).DrawWidth && standaloneX >= tagsOf(standalonePanel).X + tagsOf(standalonePanel).DrawWidth);
            AddStep("hide tags", () => config.SetValue(OsuSetting.ShowDifficultyTags, false));
            AddUntilStep("buttons reclaim tag width", () => setPanel.ChildrenOfType<PanelUpdateBeatmapButton>().Single().X < setX && standalonePanel.ChildrenOfType<PanelUpdateBeatmapButton>().Single().X < standaloneX);
            AddStep("show tags", () => config.SetValue(OsuSetting.ShowDifficultyTags, true));
        }

        private static BeatmapDifficultyTagsDisplay tagsOf(Drawable drawable) => drawable.ChildrenOfType<BeatmapDifficultyTagsDisplay>().Single();

        private string? groupOfSet()
        {
            var carousel = songSelect.ChildrenOfType<BeatmapCarousel>().SingleOrDefault();

            return carousel?.GetCarouselItems()?
                           .Select(i => i.Model)
                           .OfType<GroupedBeatmapSet>()
                           .FirstOrDefault(g => g.BeatmapSet.ID == beatmapSet.ID)?
                           .Group?.Title.ToString();
        }

        private static ToggleMenuItem? introToggle(MenuItem[]? items)
            => items?.OfType<ToggleMenuItem>().SingleOrDefault(i => i.Text.Value.Equals(SongSelectStrings.UseOnGameIntro));
    }
}
