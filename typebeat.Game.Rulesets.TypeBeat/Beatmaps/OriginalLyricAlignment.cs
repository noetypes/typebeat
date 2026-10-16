// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using typebeat.Game.Rulesets.TypeBeat.Gameplay;
using typebeat.Game.Rulesets.TypeBeat.Import;

namespace typebeat.Game.Rulesets.TypeBeat.Beatmaps
{
    /// <summary>Only splits a linked original where both romanized halves match its script.</summary>
    internal static class OriginalLyricAlignment
    {
        internal static bool Matches(string original, string text, string? language)
        {
            string input = new string(text.Where(c => char.IsLetterOrDigit(c) || c == '\'').Select(char.ToLowerInvariant).ToArray());
            var word = LyricOriginals.RomaniseWord(original, language);
            if (!word.Flagged && new string(word.Text.Where(c => char.IsLetterOrDigit(c) || c == '\'').Select(char.ToLowerInvariant).ToArray()) == input)
                return true;
            if (!JapaneseReading.IsJapanese(language))
                return false;
            string surface = new string(original.Where(char.IsLetterOrDigit).ToArray());
            string kana = JapaneseRomaji.Hiragana(string.Concat(JapaneseReading.Read(surface).Select(p => p.Kana)));
            return kana.Length > 0 && (JapaneseRomaji.Matches(kana, input) & JapaneseRomaji.Match.Complete) != 0
                   || JapaneseLyricGenerator.HasDictionaryReading(surface, input);
        }

        internal static int? SourceCut(string original, string text, int cut, string? language)
        {
            if (cut <= 0 || cut >= text.Length)
                return null;
            var word = LyricOriginals.RomaniseWord(original, language);
            if (!word.Flagged && string.Equals(word.Text, text, StringComparison.OrdinalIgnoreCase)
                && word.SourceIndexOfTextSplit(cut) is int mapped && mapped > 0 && mapped < original.Length)
                return mapped;
            if (JapaneseReading.IsJapanese(language) && Matches(original, text, language))
            {
                string kana = JapaneseRomaji.Hiragana(string.Concat(JapaneseReading.Read(original).Select(p => p.Kana)));
                string spelling = new string(text.ToLowerInvariant().Where(c => char.IsLetter(c) || c == '\'').ToArray());
                if ((JapaneseRomaji.Matches(kana, spelling) & JapaneseRomaji.Match.Complete) == 0)
                    kana = JapaneseRomaji.ReadAuthored(spelling) ?? kana;
                string[] readings = JapaneseCharacterReadings.Split(original, kana);
                for (int i = 1; i < readings.Length; i++)
                {
                    string prefix = string.Concat(readings.Take(i)), suffix = string.Concat(readings.Skip(i));
                    if ((JapaneseRomaji.Matches(prefix, text[..cut].ToLowerInvariant()) & JapaneseRomaji.Match.Complete) != 0
                        && (JapaneseRomaji.Matches(suffix, text[cut..].ToLowerInvariant()) & JapaneseRomaji.Match.Complete) != 0)
                        return i;
                }
            }
            for (int i = 1; i < original.Length; i++)
            {
                if (char.IsLowSurrogate(original[i]) || char.GetUnicodeCategory(original[i]) == System.Globalization.UnicodeCategory.NonSpacingMark)
                    continue;
                if (Matches(original[..i], text[..cut], language) && Matches(original[i..], text[cut..], language))
                    return i;
            }
            return null;
        }
    }
}
