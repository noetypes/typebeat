// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using typebeat.Game.Beatmaps;
using typebeat.Game.Overlays;
using typebeat.Game.Rulesets.Mods;
using typebeat.Game.Rulesets.TypeBeat.Beatmaps;
using typebeat.Game.Rulesets.TypeBeat.Mods;
using typebeat.Game.Rulesets.TypeBeat.Objects;
using typebeat.Game.Screens.Select;
using typebeat.Game.Tests.Visual;

namespace typebeat.Game.Rulesets.TypeBeat.Tests.Visual
{
    public partial class TestSceneSongSelectDifficultyMetrics : OsuTestScene
    {
        [Cached]
        private readonly OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Purple);

        private BeatmapTitleWedge.DifficultyDisplay display = null!;

        protected override Ruleset CreateRuleset() => new TypeBeatRuleset();

        [Test]
        public void TestHardRockRefreshesRhythmWithoutChangingRate()
        {
            string plain = string.Empty;
            string hardRock = string.Empty;
            AddStep("show map metrics", () =>
            {
                SelectedMods.Value = Array.Empty<Mod>();
                var map = new TypeBeatBeatmap
                {
                    BeatmapInfo = new BeatmapInfo { Ruleset = Ruleset.Value, DifficultyName = "Metric test" },
                };
                for (int i = 0; i < 12; i++)
                {
                    double start = i * 1000;
                    var line = new LyricLine
                    {
                        RawText = "The bad-cat sat.",
                        StartTime = start,
                        EndTime = start + 1000,
                        SingEndTime = start + 1000,
                        Units = new[] { new TimedUnit { Text = "The bad-cat sat.", StartTime = start, EndTime = start + 1000 } },
                    };
                    map.HitObjects.Add(new TypeBeatHitObject { Line = line, LineIndex = i, StartTime = start });
                }
                Beatmap.Value = CreateWorkingBeatmap(map);
                Child = new Container
                {
                    Width = 700,
                    AutoSizeAxes = Axes.Y,
                    Child = display = new BeatmapTitleWedge.DifficultyDisplay(),
                };
            });
            AddUntilStep("rhythm row loaded", () => metric("Rhythm")?.Value.Content != null);
            AddAssert("Complexity replaces Typability", () => metric("Complexity") != null && metric("Typability") == null);
            AddAssert("Rhythm sits to the right of Complexity", () => metric("Rhythm")!.ScreenSpaceDrawQuad.AABBFloat.Left
                > metric("Complexity")!.ScreenSpaceDrawQuad.AABBFloat.Left);
            AddStep("remember baseline", () => plain = metric("Rhythm")!.Value.Content!);
            AddStep("enable HR at 1x", () => SelectedMods.Value = new Mod[] { new TypeBeatModHardRock() });
            AddUntilStep("rhythm refreshes for HR", () => metric("Rhythm")!.Value.Content != plain);
            AddAssert("HR shows greater rhythm", () => number(metric("Rhythm")!.Value.Content!) > number(plain));
            AddStep("remember HR", () => hardRock = metric("Rhythm")!.Value.Content!);
            AddStep("remove HR at 1x", () => SelectedMods.Value = Array.Empty<Mod>());
            AddUntilStep("baseline restored", () => metric("Rhythm")!.Value.Content == plain);
            AddStep("rapid same-rate HR changes", () =>
            {
                SelectedMods.Value = new Mod[] { new TypeBeatModHardRock() };
                SelectedMods.Value = Array.Empty<Mod>();
                SelectedMods.Value = new Mod[] { new TypeBeatModHardRock() };
            });
            AddUntilStep("latest HR reading wins", () => metric("Rhythm")!.Value.Content == hardRock);
            AddStep("restore no mods", () => SelectedMods.Value = Array.Empty<Mod>());
            AddUntilStep("baseline returns after rapid changes", () => metric("Rhythm")!.Value.Content == plain);
        }

        private BeatmapTitleWedge.StatisticDifficulty? metric(string label)
            => display?.ChildrenOfType<BeatmapTitleWedge.StatisticDifficulty>().SingleOrDefault(s => s.Value.Label.ToString() == label);

        private static double number(string text) => double.Parse(text, CultureInfo.InvariantCulture);
    }
}
