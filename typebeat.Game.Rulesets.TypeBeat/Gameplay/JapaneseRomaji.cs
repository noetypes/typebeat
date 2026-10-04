// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using typebeat.Game.Rulesets.TypeBeat.Beatmaps;

namespace typebeat.Game.Rulesets.TypeBeat.Gameplay
{
    /// <summary>
    /// An incremental Japanese input grammar. Paths consume kana rather than a fixed Hepburn spelling,
    /// so alternate spellings, split small kana and doubled consonants share one target reading.
    /// </summary>
    internal static class JapaneseRomaji
    {
        [Flags]
        internal enum Match { Invalid = 0, Prefix = 1, Complete = 2 }

        private static readonly Dictionary<string, string[]> spellings = createSpellings();

        internal static string Hiragana(string text)
        {
            var result = new StringBuilder(text.Length);
            foreach (char c in text.Normalize(NormalizationForm.FormKC))
                result.Append(c is >= 'ァ' and <= 'ヶ' ? (char)(c - 0x60) : c);
            return result.ToString();
        }

        internal static int MoraLength(string text, int index)
        {
            int length = index + 1 < text.Length && spellings.ContainsKey(text.Substring(index, 2)) ? 2 : 1;
            if (text[index] is 'っ' or 'ん' && index + 1 < text.Length
                && (spellings.ContainsKey(text[index + 1].ToString()) || text[index + 1] is 'っ' or 'ん'))
                length = 1 + MoraLength(text, index + 1);
            return length;
        }

        internal static Match Matches(string kana, string input, string following = "")
        {
            var memo = new Dictionary<(int, int), Match>();
            return walk(0, 0);

            Match walk(int at, int typed)
            {
                if (typed == input.Length)
                    return at == kana.Length ? Match.Complete : Match.Prefix;
                if (at == kana.Length)
                    return Match.Invalid;
                if (memo.TryGetValue((at, typed), out Match cached))
                    return cached;

                Match result = Match.Invalid;
                foreach (var (length, spelling) in edges(kana, at, following))
                {
                    int remaining = input.Length - typed;
                    int compare = Math.Min(remaining, spelling.Length);
                    if (!input.AsSpan(typed, compare).SequenceEqual(spelling.AsSpan(0, compare)))
                        continue;
                    result |= remaining < spelling.Length ? Match.Prefix : walk(at + length, typed + spelling.Length);
                }
                memo[(at, typed)] = result;
                return result;
            }
        }

        private static IEnumerable<(int Length, string Spelling)> edges(string kana, int at, string following)
        {
            char c = kana[at];
            if (c == 'ん')
            {
                yield return (1, "nn");
                yield return (1, "xn");
                yield return (1, "n'");
                string next = at + 1 < kana.Length ? kana[(at + 1)..] : following;
                // At a word/line boundary n is complete but can still become nn. The engine
                // keeps that ambiguity pending until the next key or an explicit confirmation.
                // Inside a reading, vowels and y still need nn/n'/xn to disambiguate the nasal.
                if (next.Length == 0 || char.IsWhiteSpace(next[0])
                    || edges(next, 0, "").Any(e => isConsonant(e.Spelling[0])))
                    yield return (1, "n");
                yield break;
            }

            if (c == 'っ')
            {
                yield return (1, "xtu");
                yield return (1, "ltu");
                yield return (1, "xtsu");
                yield return (1, "ltsu");
                if (at + 1 < kana.Length)
                {
                    foreach (var (length, spelling) in edges(kana, at + 1, following))
                    {
                        if (isConsonant(spelling[0]) && spelling[0] != 'n')
                            yield return (1 + length, spelling[0] + spelling);
                        if (spelling.StartsWith("ch", StringComparison.Ordinal))
                            yield return (1 + length, "t" + spelling);
                    }
                }
                yield break;
            }

            for (int length = Math.Min(2, kana.Length - at); length > 0; length--)
            {
                if (spellings.TryGetValue(kana.Substring(at, length), out string[]? options))
                {
                    foreach (string spelling in options)
                        yield return (length, spelling);
                }
            }
        }

        /// <summary>Decode a mapper's phonetic reading when it differs from the dictionary.</summary>
        internal static string? ReadAuthored(string text)
        {
            // A map's surrounding punctuation is typed separately under Literate. It must not
            // invalidate an otherwise explicit kanji reading (e.g. jigokugoku!). Keep the IME's
            // nasal apostrophe and the long-vowel hyphen as part of the reading.
            text = text.ToLowerInvariant().Trim(Typeability.PUNCTUATION.Where(c => c is not '\'' and not '-').ToArray());
            var kana = new StringBuilder();
            for (int at = 0; at < text.Length;)
            {
                if (text[at] == 'n' && (at + 1 == text.Length || text[at + 1] == '\'' || isConsonant(text[at + 1])))
                {
                    kana.Append('ん');
                    at += at + 1 < text.Length && (text[at + 1] == '\'' || text[at + 1] == 'n'
                        && (at + 2 == text.Length || isConsonant(text[at + 2]))) ? 2 : 1;
                    continue;
                }
                if (at + 1 < text.Length && isConsonant(text[at]) && text[at] != 'n'
                    && (text[at] == text[at + 1] || text.AsSpan(at).StartsWith("tch")))
                {
                    kana.Append('っ');
                    at++;
                    continue;
                }

                string? bestKana = null;
                int bestLength = 0;
                foreach (var pair in spellings)
                {
                    foreach (string spelling in pair.Value)
                    {
                        if (spelling.Length > bestLength && text.AsSpan(at).StartsWith(spelling, StringComparison.Ordinal))
                        {
                            bestKana = pair.Key;
                            bestLength = spelling.Length;
                        }
                    }
                }
                if (bestKana == null)
                    return null;
                kana.Append(bestKana);
                at += bestLength;
            }
            return kana.Length > 0 ? kana.ToString() : null;
        }

        private static bool isConsonant(char c) => c is >= 'a' and <= 'z' && !"aiueoy".Contains(c);

        private static Dictionary<string, string[]> createSpellings()
        {
            var result = new Dictionary<string, string[]>();
            add("あ:a い:i,yi う:u,wu,whu え:e お:o か:ka,ca き:ki く:ku,cu,qu け:ke こ:ko,co "
                + "さ:sa し:si,shi,ci す:su せ:se,ce そ:so た:ta ち:ti,chi つ:tu,tsu て:te と:to "
                + "な:na に:ni ぬ:nu ね:ne の:no は:ha ひ:hi ふ:hu,fu へ:he ほ:ho "
                + "ま:ma み:mi む:mu め:me も:mo や:ya ゆ:yu よ:yo ら:ra り:ri る:ru れ:re ろ:ro わ:wa を:wo "
                + "が:ga ぎ:gi ぐ:gu げ:ge ご:go ざ:za じ:zi,ji ず:zu ぜ:ze ぞ:zo だ:da ぢ:di づ:du で:de ど:do "
                + "ば:ba び:bi ぶ:bu べ:be ぼ:bo ぱ:pa ぴ:pi ぷ:pu ぺ:pe ぽ:po ゔ:vu ゐ:wyi ゑ:wye ー:- "
                + "ぁ:xa,la ぃ:xi,li,xyi,lyi ぅ:xu,lu ぇ:xe,le,xye,lye ぉ:xo,lo ゃ:xya,lya ゅ:xyu,lyu ょ:xyo,lyo ゎ:xwa,lwa ゕ:xka,lka ゖ:xke,lke");
            foreach (var (kana, head) in new[] { ("き", "k"), ("ぎ", "g"), ("に", "n"), ("ひ", "h"), ("び", "b"), ("ぴ", "p"), ("み", "m"), ("り", "r") })
            {
                foreach (var (small, vowel) in new[] { ("ゃ", "a"), ("ゅ", "u"), ("ょ", "o"), ("ぇ", "e") })
                    result[kana + small] = new[] { head + "y" + vowel };
            }
            add("しゃ:sya,sha しゅ:syu,shu しぇ:sye,she しょ:syo,sho "
                + "じゃ:zya,jya,ja じゅ:zyu,jyu,ju じぇ:zye,jye,je じょ:zyo,jyo,jo "
                + "ちゃ:tya,cya,cha ちゅ:tyu,cyu,chu ちぇ:tye,cye,che ちょ:tyo,cyo,cho ぢゃ:dya ぢゅ:dyu ぢょ:dyo "
                + "いぇ:ye うぁ:wha うぃ:wi,whi うぇ:we,whe うぉ:who "
                + "ふぁ:fa ふぃ:fi,fyi ふぇ:fe,fye ふぉ:fo ふゃ:fya ふゅ:fyu ふょ:fyo "
                + "ゔぁ:va ゔぃ:vi,vyi ゔぇ:ve,vye ゔぉ:vo ゔゃ:vya ゔゅ:vyu ゔょ:vyo "
                + "つぁ:tsa つぃ:tsi つぇ:tse つぉ:tso てぃ:thi てゅ:thu でぃ:dhi でゅ:dhu とぅ:twu どぅ:dwu "
                + "くぁ:kwa,qa,qwa くぃ:kwi,qi,qwi,qyi くぇ:kwe,qe,qwe,qye くぉ:kwo,qo,qwo くゃ:qya くゅ:qyu くょ:qyo "
                + "ぐぁ:gwa ぐぃ:gwi ぐぇ:gwe ぐぉ:gwo すぃ:swi ずぃ:zwi");
            return result;

            void add(string rows)
            {
                foreach (string row in rows.Split(' '))
                {
                    string[] parts = row.Split(':');
                    result.Add(parts[0], parts[1].Split(','));
                }
            }
        }
    }
}
