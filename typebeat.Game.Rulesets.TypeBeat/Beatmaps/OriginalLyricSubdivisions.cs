// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using typebeat.Game.Rulesets.TypeBeat.Gameplay;

namespace typebeat.Game.Rulesets.TypeBeat.Beatmaps
{
    /// <summary>Projects one set of timed cuts between linked romanized and original-script words.</summary>
    internal static class OriginalLyricSubdivisions
    {
        internal static int? SourceCut(TimedUnit unit, int cut, string? language)
        {
            if (unit.Original == null)
                return null;
            int? mapped = OriginalLyricAlignment.SourceCut(unit.Original, unit.Text, cut, language);
            return mapped is int index && StringInfo.ParseCombiningCharacters(unit.Original).Contains(index) ? index : null;
        }

        internal static int? TextCut(TimedUnit unit, int sourceCut, string? language)
        {
            for (int cut = 1; cut < unit.Text.Length; cut++)
            {
                if (SourceCut(unit, cut, language) == sourceCut)
                    return cut;
            }
            return null;
        }

        /// <summary>Filter a copy for original-script display or automatically generated import cuts only.</summary>
        internal static TimedUnit AlignAutomaticCuts(TimedUnit unit, string? language)
        {
            if (string.IsNullOrEmpty(unit.Original) || unit.SyllableBoundaries.Count == 0 && unit.Pauses.Count == 0)
                return unit;
            var cuts = SyllableSegments.SplitsFor(unit);
            var kept = cuts.Select((cut, i) => (Cut: cut, Index: i))
                           .Where(c => c.Index < unit.SyllableBoundaries.Count && SourceCut(unit, c.Cut, language).HasValue).ToArray();
            var pauses = unit.Pauses.Where(p => SourceCut(unit, p.SplitChar, language).HasValue).ToArray();
            if (kept.Length == unit.SyllableBoundaries.Count && pauses.Length == unit.Pauses.Count)
                return unit;
            return copy(unit, unit.Text, kept.Select(c => unit.SyllableBoundaries[c.Index]).ToArray(), kept.Select(c => c.Cut).ToArray(), pauses);
        }

        /// <summary>The original-script runs use exactly the same timing spans as their romanized runs.</summary>
        internal static TimedUnit Project(TimedUnit source, string? language)
        {
            if (source.Original == null)
                return source;
            if (source.Original.Length == 0)
                return copy(source, string.Empty, Array.Empty<double>(), Array.Empty<int>(), Array.Empty<WordPause>());
            var unit = AlignAutomaticCuts(source, language);
            var splits = SyllableSegments.SplitsFor(unit).Select(c => SourceCut(unit, c, language)!.Value).ToArray();
            var pauses = unit.Pauses.Select(p => p with { SplitChar = SourceCut(unit, p.SplitChar, language)!.Value }).ToArray();
            return copy(unit, unit.Original!, unit.SyllableBoundaries, splits, pauses);
        }

        internal static string PipeText(TimedUnit unit, string? language, bool original)
        {
            unit = original ? Project(unit, language) : unit;
            var cuts = PausedWord.Cuts(unit, unit.StartTime, unit.EndTime, original ? Typeability.IsPolyglotTypeCell : null);
            return string.Join(Typeability.SPLIT_MARKER, SyllableSegments.SegmentTexts(unit.Text, cuts));
        }

        private static TimedUnit copy(TimedUnit unit, string text, IReadOnlyList<double> boundaries, IReadOnlyList<int> splits, IReadOnlyList<WordPause> pauses)
            => new TimedUnit
            {
                Text = text, Original = unit.Original, StartTime = unit.StartTime, EndTime = unit.EndTime,
                Source = unit.Source, Confidence = unit.Confidence,
                SyllableBoundaries = boundaries, SyllableSplits = splits, Pauses = pauses,
            };
    }
}
