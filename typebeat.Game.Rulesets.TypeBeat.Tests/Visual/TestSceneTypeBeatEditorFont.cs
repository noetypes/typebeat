// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Testing;
using osuTK.Input;
using typebeat.Game.Graphics;
using typebeat.Game.Graphics.Fonts;
using typebeat.Game.Graphics.Sprites;
using typebeat.Game.Graphics.UserInterface;
using typebeat.Game.Rulesets.TypeBeat;
using typebeat.Game.Rulesets.TypeBeat.Beatmaps;
using typebeat.Game.Rulesets.TypeBeat.Configuration;
using typebeat.Game.Rulesets.TypeBeat.Edit;
using typebeat.Game.Rulesets.TypeBeat.UI;
using typebeat.Game.Tests.Visual;

namespace typebeat.Game.Rulesets.TypeBeat.Tests.Visual
{
    /// <summary>Player gameplay font choices are also used for lyric text in the editor.</summary>
    public partial class TestSceneTypeBeatEditor
    {
        [Test]
        public void TestGameplayLyricFontAppliesToEditorLyricsLive()
        {
            AddUntilStep("compose screen shown", () => Editor.ChildrenOfType<LyricComposeScreen>().Any());
            AddStep("show the original-script editor", () => TypeBeatEditorOperations.SetLineOriginal(EditorBeatmap, firstLine(), "hello source"));
            AddUntilStep("word script editor shown", () => Editor.ChildrenOfType<WordScriptEditor>().Single().WordColumnCount > 0);
            AddStep("choose the bundled gameplay font", () => lyricFontConfig().GetBindable<string>(TypeBeatRulesetSetting.LyricFont).Value = LyricFontManager.OPEN_DYSLEXIC);

            AddUntilStep("line input uses gameplay font", () =>
            {
                var input = Editor.ChildrenOfType<LineListPanel>().Single().ChildrenOfType<OsuTextBox>().FirstOrDefault();
                return input != null && input.FontFamily == LyricFontManager.OPEN_DYSLEXIC
                       && input.ChildrenOfType<OsuSpriteText>().Any(s => s.Font.Family == LyricFontManager.OPEN_DYSLEXIC);
            });

            AddUntilStep("active line preview uses gameplay font", () =>
                Editor.ChildrenOfType<ActiveLineDetailPanel>().Single().ChildrenOfType<FreestyleTextFlow>().First()
                    .ChildrenOfType<OsuSpriteText>().Any(s => s.Font.Family == LyricFontManager.OPEN_DYSLEXIC));

            AddUntilStep("word script inputs use gameplay font", () =>
                Editor.ChildrenOfType<WordScriptEditor>().Single().ChildrenOfType<OsuTextBox>().Count() >= 2
                && Editor.ChildrenOfType<WordScriptEditor>().Single().ChildrenOfType<OsuTextBox>()
                    .All(box => box.FontFamily == LyricFontManager.OPEN_DYSLEXIC
                                && box.ChildrenOfType<OsuSpriteText>().Any(s => s.Font.Family == LyricFontManager.OPEN_DYSLEXIC)));

            AddUntilStep("word timeline labels use gameplay font", () =>
                Editor.ChildrenOfType<LyricTimeline>().Single().ChildrenOfType<OsuSpriteText>()
                    .Any(s => s.Text.ToString() == "hello world" && s.Font.Family == LyricFontManager.OPEN_DYSLEXIC));

            OsuTextBox? focusedScriptBox = null;
            string? scriptTextBeforeFontChange = null;
            AddStep("focus a populated word script input", () =>
            {
                focusedScriptBox = Editor.ChildrenOfType<WordScriptEditor>().Single().ChildrenOfType<OsuTextBox>()
                    .First(box => !string.IsNullOrEmpty(box.Text));
                scriptTextBeforeFontChange = focusedScriptBox.Text;
                InputManager.MoveMouseTo(focusedScriptBox);
                InputManager.Click(MouseButton.Left);
            });
            AddUntilStep("populated word script input focused", () => focusedScriptBox?.HasFocus == true);
            AddStep("switch back to the built-in font while editing", () =>
                lyricFontConfig().GetBindable<string>(TypeBeatRulesetSetting.LyricFont).Value = TypeBeatRulesetConfigManager.LYRIC_FONT_DEFAULT);
            AddUntilStep("same focused input keeps text and updates font", () =>
            {
                if (focusedScriptBox == null)
                    return false;

                var liveBoxes = Editor.ChildrenOfType<WordScriptEditor>().Single().ChildrenOfType<OsuTextBox>();
                string expectedFamily = OsuFont.GetFont(size: focusedScriptBox.FontSize).Family;
                var glyphs = focusedScriptBox.ChildrenOfType<OsuSpriteText>()
                    .Where(sprite => !string.IsNullOrEmpty(sprite.Text.ToString())).ToArray();

                return liveBoxes.Any(box => ReferenceEquals(box, focusedScriptBox))
                       && focusedScriptBox.HasFocus
                       && focusedScriptBox.Text == scriptTextBeforeFontChange
                       && focusedScriptBox.FontFamily == null
                       && glyphs.Length > 0
                       && glyphs.All(sprite => sprite.Font.Family == expectedFamily);
            });
        }

        private TypeBeatRulesetConfigManager lyricFontConfig()
            => (TypeBeatRulesetConfigManager)RulesetConfigs.GetConfigFor(new TypeBeatRuleset())!;
    }
}
