// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using typebeat.Game.Graphics.Sprites;
using typebeat.Game.Online.Leaderboards;
using typebeat.Game.Overlays;
using typebeat.Game.Rulesets.Scoring;
using typebeat.Game.Scoring;
using typebeat.Game.Screens.Select;
using typebeat.Game.Tests.Visual;

namespace typebeat.Game.Rulesets.TypeBeat.Tests.Visual
{
    public partial class TestSceneTypeBeatReplayGrades : OsuTestScene
    {
        [Cached]
        private readonly OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Purple);

        protected override Ruleset CreateRuleset() => new TypeBeatRuleset();

        [TestCase(ScoreRank.X, 0.90, 0, ScoreRank.A)]
        [TestCase(ScoreRank.A, 0.99, 0, ScoreRank.X)]
        [TestCase(ScoreRank.S, 0.97, 3, ScoreRank.A)]
        [TestCase(ScoreRank.X, 0.60, 40, ScoreRank.C)]
        public void SelectorRowAndTooltipShowCurrentGrade(ScoreRank storedRank, double accuracy, int misses, ScoreRank expected)
        {
            ScoreInfo score = null!;
            BeatmapLeaderboardScore row = null!;
            BeatmapLeaderboardScore.LeaderboardScoreTooltip.TotalScoreRankPanel tooltip = null!;

            AddStep("load replay row and tooltip", () =>
            {
                score = new ScoreInfo(ruleset: new TypeBeatRuleset().RulesetInfo)
                {
                    Rank = storedRank,
                    Accuracy = accuracy,
                    Statistics = new Dictionary<HitResult, int> { [HitResult.Great] = 100 - misses, [HitResult.Miss] = misses },
                };
                Child = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Children = new Drawable[]
                    {
                        row = new BeatmapLeaderboardScore(score, sheared: false),
                        tooltip = new BeatmapLeaderboardScore.LeaderboardScoreTooltip.TotalScoreRankPanel(),
                    },
                };
            });
            AddUntilStep("replay controls loaded", () => row.IsLoaded && tooltip.IsLoaded);
            AddStep("show tooltip grade", () => tooltip.Score = score);

            string letter = DrawableRank.GetRankLetter(expected);
            AddUntilStep("row displays current letter grade", () => row.ChildrenOfType<OsuSpriteText>().Any(text => text.Text.ToString() == letter));
            AddUntilStep("tooltip displays the same grade", () => tooltip.ChildrenOfType<OsuSpriteText>().Any(text => text.Text.ToString() == letter));
            AddAssert("historical rank is preserved", () => score.Rank == storedRank);
        }
    }
}
