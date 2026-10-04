// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework;
using osu.Framework.Input;
using osu.Framework.Testing;
using osuTK.Input;
using typebeat.Game.Beatmaps;
using typebeat.Game.Graphics.Sprites;
using typebeat.Game.Replays.Legacy;
using typebeat.Game.Rulesets.TypeBeat.Beatmaps;
using typebeat.Game.Rulesets.TypeBeat.Configuration;
using typebeat.Game.Rulesets.TypeBeat.Gameplay;
using typebeat.Game.Rulesets.TypeBeat.Mods;
using typebeat.Game.Rulesets.TypeBeat.Objects;
using typebeat.Game.Rulesets.TypeBeat.Replays;
using typebeat.Game.Rulesets.TypeBeat.Scoring;
using typebeat.Game.Rulesets.TypeBeat.UI;
using typebeat.Game.Tests.Visual;

namespace typebeat.Game.Rulesets.TypeBeat.Tests.Visual
{
    public partial class TestSceneTypeBeatJapaneseInput : PlayerTestScene
    {
        protected override bool HasCustomSteps => true;
        protected override Ruleset CreatePlayerRuleset() => new TypeBeatRuleset();
        private TypeBeatPlayfield playfield => (TypeBeatPlayfield)Player.DrawableRuleset.Playfield;
        private TypingEngine engine => playfield.Engine;
        private LyricStage stage => Player.ChildrenOfType<LyricStage>().Single();
        private LyricLineDisplay display => stage.DisplayAt(0)!;
        private LyricLine? testLine;
        private EmulatedTextInput os => ((KeycapManualInputManager)InputManager).TextInput;
        private TypeBeatRulesetConfigManager config => (TypeBeatRulesetConfigManager)RulesetConfigs.GetConfigFor(new TypeBeatRuleset())!;
        private List<TypeBeatReplayFrame> frames => Player.GameplayState.Score.Replay.Frames.OfType<TypeBeatReplayFrame>().ToList();
        private OsuSpriteText ruby(string text) => display.ChildrenOfType<OsuSpriteText>().Single(t => t.Text.ToString() == text);

        public override void SetUpSteps()
        {
            AddStep("reset lyric fixture", () => testLine = null);
            AddStep("word skipping off", () => config.SetValue(TypeBeatRulesetSetting.SpaceSkipsWord, false));
            AddStep("manual newlines off", () => config.SetValue(TypeBeatRulesetSetting.ManualNewlines, false));
            base.SetUpSteps();
        }

        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset)
        {
            var beatmap = new Beatmap { HitObjects = new List<Rulesets.Objects.HitObject>() };
            beatmap.BeatmapInfo.Ruleset = ruleset;
            beatmap.BeatmapInfo.Metadata.Language = BeatmapLanguage.Japanese;
            var line = testLine ?? new LyricLine
            {
                RawText = "tsukitotenshi", Original = "月と天使",
                StartTime = 0, EndTime = 600000, SingEndTime = 300000,
                Units = new[] { new TimedUnit { Text = "tsukitotenshi", Original = "月と天使", StartTime = 0, EndTime = 300000 } },
            };
            beatmap.HitObjects.Add(new TypeBeatHitObject { StartTime = 0, LineIndex = 0, Line = line, Granularity = TimingGranularity.Word });
            return beatmap;
        }

        [TestCase("tuki", "tensi")]
        [TestCase("tsuki", "tenshi")]
        [TestCase("tuki", "tennsi")]
        [TestCase("tsuki", "tennshi")]
        public void TestRomajiTypesJapaneseWithoutImeSubmission(string moon, string angel)
        {
            load();
            AddAssert("Japanese input uses Code with IME disabled", () => os.ActiveProperties,
                () => Is.EqualTo(new TextInputProperties(TextInputType.Code, AllowIme: false)));
            AddAssert("furigana is present", () => ruby("つき").IsLoaded && ruby("てんし").IsLoaded);
            press("t");
            AddUntilStep("partial romaji is visible", () => engine.JapanesePending == "t" && stage.ImeComposition == "t");
            press(moon[1..]);
            AddAssert("月 converted without enter", () => engine.CaretIndex == 1 && engine.Lines[0].Cells[0].State == CellState.Correct);
            press("to" + angel);
            AddAssert("whole Japanese line typed correctly", () => engine.Lines[0].Cells.All(c => c.State == CellState.Correct) && engine.Mistypes == 0);
            AddAssert("replay stores raw romaji and its input flag", () =>
                frames.Single(f => f.IsConfigExtended).JapaneseRomajiInput
                && new string(frames.Where(f => !f.IsConfig && !f.IsConfigExtended).Select(f => f.Character).ToArray()) == moon + "to" + angel);
            AddAssert("legacy replay reproduces the play", reDerivedMatchesLive);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestKanaAndWordSeamsThroughGameplayAndReplay(bool originalSpaces)
        {
            AddStep("use mixed kanji and kana word originals", () => testLine = new LyricLine
            {
                RawText = "tsuki to tenshi", Original = originalSpaces ? "月 と テンシ" : null,
                StartTime = 0, EndTime = 600000, SingEndTime = 300000,
                Units = new[]
                {
                    new TimedUnit { Text = "tsuki", Original = "月", StartTime = 0, EndTime = 100000 },
                    new TimedUnit { Text = "to", Original = "と", StartTime = 100000, EndTime = 200000 },
                    new TimedUnit { Text = "tenshi", Original = "テンシ", StartTime = 200000, EndTime = 300000 },
                },
            });
            load();
            AddAssert("only original spaces are typeable", () => engine.Lines[0].DisplayText == (originalSpaces ? "月 と テンシ" : "月とテンシ"));
            AddAssert("scoring objects use the same cell stream", () => Player.GameplayState.Beatmap.HitObjects.OfType<TypeBeatHitObject>().Single().NestedHitObjects.OfType<TypeBeatCharObject>().Count()
                == engine.Lines[0].Cells.Count(c => c.IsTypeable));
            press("tuki");
            if (originalSpaces)
                AddStep("type the first original space", () => InputManager.Key(Key.Space));
            press("to");
            if (originalSpaces)
                AddStep("type the second original space", () => InputManager.Key(Key.Space));
            press("tennshi");
            AddAssert("kanji, hiragana and katakana are correct", () => engine.Lines[0].Cells.All(c => c.State == CellState.Correct) && engine.Mistypes == 0);
            AddAssert("spaced or joined play replays exactly", reDerivedMatchesLive);
        }

        [Test]
        public void TestPendingBackspaceAndFuriganaLayout()
        {
            load();
            press("ts");
            AddStep("backspace the pending s", () => InputManager.Key(Key.BackSpace));
            AddUntilStep("only t remains", () => engine.JapanesePending == "t");
            press("uki");
            AddAssert("the pending erase is recorded", () => frames.Count(f => f.IsBackspace) == 1);
            AddAssert("ruby is small and above the kanji", () =>
                ruby("つき").Font.Size < display.ChildrenOfType<OsuSpriteText>().Single(t => t.Text.ToString() == "月").Font.Size
                && ruby("つき").ScreenSpaceDrawQuad.BottomLeft.Y < display.CellScreenPosition(0).Y - display.LineHeight * 0.25f);
            AddAssert("the erase replays exactly", reDerivedMatchesLive);
        }

        [Test]
        public void TestRetypeSelectionErasesPendingRomajiAndTheSelectedJapanese()
        {
            load();
            press("zt");
            AddAssert("next reading has a pending t", () => engine.CaretIndex == 1 && engine.JapanesePending == "t");
            AddStep("select back with the recovery shortcut", () =>
            {
                Key modifier = RuntimeInfo.OS == RuntimeInfo.Platform.macOS ? Key.LWin : Key.ControlLeft;
                InputManager.PressKey(modifier);
                InputManager.Key(Key.A);
                InputManager.ReleaseKey(modifier);
            });
            AddAssert("the first Japanese character is selected", () => playfield.CurrentRetypeSelection is TypeBeatPlayfield.RetypeSelection { StartCell: 0, EndCell: 1 });
            press("t");
            AddAssert("typing restarted at the selection anchor", () => engine.CaretIndex == 0 && engine.JapanesePending == "t" && playfield.CurrentRetypeSelection == null);
            press("uki");
            AddAssert("the pending letter and the selected character were both erased", () => frames.Count(f => f.IsBackspace) == 2);
            AddAssert("the recovery replays exactly", reDerivedMatchesLive);
        }

        [Test]
        public void TestReciteAndFlashlightAlsoHideFurigana()
        {
            load(recite: true);
            AddUntilStep("untyped kanji and ruby are hidden", () => display.CellAlpha(0) == 0 && ruby("つき").Alpha == 0 && ruby("てんし").Alpha == 0);
            press("tuki");
            AddUntilStep("the typed kanji reading is revealed", () => ruby("つき").Alpha > 0.8f);
            AddAssert("the unread compound remains hidden", () => ruby("てんし").Alpha == 0);
            AddStep("hide the line for Flashlight", () => display.HideForFlashlight());
            AddUntilStep("ruby hides with its kanji", () => ruby("つき").Alpha == 0);
        }

        private void load(bool recite = false)
        {
            AddStep("load Japanese Polyglot", () => LoadPlayer(recite
                ? new Rulesets.Mods.Mod[] { new TypeBeatModPolyglot(), new TypeBeatModRecite() }
                : new Rulesets.Mods.Mod[] { new TypeBeatModPolyglot() }));
            AddUntilStep("player and line loaded", () => Player.IsLoaded && Player.Alpha == 1 && engine.ActiveLineIndex == 0);
        }

        private void press(string text)
        {
            foreach (char c in text)
                AddStep($"press {c}", () => InputManager.Key(Enum.Parse<Key>(char.ToUpperInvariant(c).ToString())));
        }

        private bool reDerivedMatchesLive()
        {
            var map = Player.GameplayState.Beatmap;
            var replayed = TypeBeatReplayScorer.CreateEngine(map, map.HitObjects.OfType<TypeBeatHitObject>().ToList(), Player.GameplayState.Mods, RateWindowRule.ScaledByRate);
            var dummy = new Beatmap();
            foreach (var frame in frames)
            {
                var legacy = frame.ToLegacy(dummy);
                var decoded = new TypeBeatReplayFrame();
                decoded.FromLegacy(new LegacyReplayFrame(legacy.Time, legacy.MouseX, legacy.MouseY, legacy.ButtonState), dummy);
                decoded.Time = legacy.Time;
                ReplayEngineFeed.Apply(replayed, decoded);
            }
            return engine.Lines[0].Cells.Zip(replayed.Lines[0].Cells).All(p => p.First.State == p.Second.State && p.First.TypedChar == p.Second.TypedChar && p.First.JudgedDelta == p.Second.JudgedDelta)
                   && engine.Score == replayed.Score && engine.Mistypes == replayed.Mistypes && engine.Combo == replayed.Combo;
        }
    }
}
