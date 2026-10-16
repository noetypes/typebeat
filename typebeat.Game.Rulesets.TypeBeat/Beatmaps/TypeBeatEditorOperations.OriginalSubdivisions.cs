// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using typebeat.Game.Rulesets.TypeBeat.Import;
using typebeat.Game.Rulesets.TypeBeat.Objects;
using typebeat.Game.Screens.Edit;

namespace typebeat.Game.Rulesets.TypeBeat.Beatmaps
{
    public static partial class TypeBeatEditorOperations
    {
        public static string? OriginalSubdivisionLanguage(EditorBeatmap beatmap, LyricLine line)
            => LyricOriginals.RomanisationLanguage(beatmap.Metadata.Language, new[] { OriginalCaption(line) });

        private static LyricLine normalizeOriginalSubdivisions(EditorBeatmap beatmap, LyricLine line)
        {
            bool captionMatchesWords = line.Original == null
                                       || string.Concat(line.Units.Select(u => string.Concat((u.Original ?? u.Text).Where(c => !char.IsWhiteSpace(c)))))
                                       == string.Concat(line.Original.Where(c => !char.IsWhiteSpace(c)));
            var originals = !string.IsNullOrWhiteSpace(line.Original) && (!captionMatchesWords || line.Units.Any(u => u.Original == null))
                ? OriginalLyricGenerator.MatchWords(line, line.Original, beatmap.Metadata.Language) : new string?[line.Units.Count];
            var units = line.Units.Select((u, i) =>
                originals[i] != null && originals[i] != u.Original ? LrcParser.WithOriginal(u, originals[i]) : u).ToArray();
            return units.Where((u, i) => !ReferenceEquals(u, line.Units[i])).Any() ? rebuild(line, units: units) : line;
        }

        /// <summary>Reconcile older maps on editor entry, in one undoable change.</summary>
        public static void NormalizeOriginalSubdivisions(EditorBeatmap beatmap)
        {
            var changes = OrderedLines(beatmap).Select(h => (Object: h, Line: normalizeOriginalSubdivisions(beatmap, h.Line)))
                .Where(c => !ReferenceEquals(c.Object.Line, c.Line)).ToArray();
            if (changes.Length == 0)
                return;
            beatmap.BeginChange();
            foreach (var change in changes)
            {
                change.Object.Line = change.Line;
                beatmap.Update(change.Object);
            }
            syncGranularity(beatmap, keepAuthoredWords: true);
            beatmap.EndChange();
        }

        /// <summary>The original caption with markers mapped from the linked words, retaining its whitespace.</summary>
        public static string? OriginalPipeDisplayText(LyricLine line, string? language = null)
        {
            string? caption = OriginalCaption(line);
            if (caption == null)
                return null;
            language ??= LyricOriginals.DetectLanguage(new[] { caption });
            var insertions = new List<int>();
            int cursor = 0;
            foreach (var slot in WordSlots(line))
            {
                string text = slot.Original ?? slot.Text;
                int start = caption.IndexOf(text, cursor, StringComparison.Ordinal);
                if (start < 0 || caption[cursor..start].Any(c => !char.IsWhiteSpace(c)))
                    return caption;
                if (slot.UnitIndex is int index && slot.Original != null)
                {
                    var projected = OriginalLyricSubdivisions.Project(line.Units[index], language);
                    insertions.AddRange(Gameplay.PausedWord.Cuts(projected, projected.StartTime, projected.EndTime, Typeability.IsPolyglotTypeCell).Select(c => start + c));
                }
                cursor = start + text.Length;
            }
            var result = new StringBuilder(caption);
            foreach (int position in insertions.Distinct().OrderByDescending(c => c))
                result.Insert(position, Typeability.SPLIT_MARKER);
            return result.ToString();
        }

        private static TimedUnit applyOriginalPipes(TimedUnit unit, string original, IReadOnlyList<int> pipes, string? language)
        {
            var visible = unit.Original == null ? Array.Empty<int>()
                : Gameplay.PausedWord.Cuts(OriginalLyricSubdivisions.Project(unit, language), unit.StartTime, unit.EndTime,
                    Typeability.IsPolyglotTypeCell).ToArray();
            unit = LrcParser.WithOriginal(unit, original);
            // Editing the spelling, or attaching originals, must preserve romaji-only cuts.
            if (pipes.SequenceEqual(visible))
                return unit;
            // Source marker edits own the shared cuts. Filter a copy first so moving a
            // visible source cut retains its time even when hidden romaji-only cuts exist.
            unit = OriginalLyricSubdivisions.AlignAutomaticCuts(unit, language);
            var mapped = pipes.Select(p => OriginalLyricSubdivisions.TextCut(unit, p, language)).Where(p => p.HasValue).Select(p => p!.Value).Distinct().ToArray();
            return applyPipes(new[] { unit }, new[] { unit.Text }, new[] { (IReadOnlyList<int>)mapped }, out _)[0];
        }

        private static (string Text, IReadOnlyList<int> Cuts) originalMarkers(string text)
        {
            var result = new StringBuilder();
            var cuts = new List<int>();
            foreach (char c in LyricOriginals.CollapseWhitespace(text))
            {
                if (c == Typeability.SPLIT_MARKER)
                    cuts.Add(result.Length);
                else
                    result.Append(c);
            }
            return (result.ToString(), cuts);
        }

        private static bool sameOriginalWord(TimedUnit left, TimedUnit right)
            => left.Text == right.Text && left.Original == right.Original
               && left.SyllableBoundaries.SequenceEqual(right.SyllableBoundaries)
               && left.SyllableSplits.SequenceEqual(right.SyllableSplits) && left.Pauses.SequenceEqual(right.Pauses);

        private static string updatedOriginalCaption(LyricLine line, List<(TimedUnit? Unit, UnromanisedWord? Pending)> entries, string caption)
        {
            var slots = WordSlots(line);
            if (slots.Count != entries.Count)
                return caption;
            var result = new StringBuilder();
            int cursor = 0;
            for (int i = 0; i < slots.Count; i++)
            {
                string before = slots[i].Original ?? slots[i].Text;
                int at = caption.IndexOf(before, cursor, StringComparison.Ordinal);
                if (at < 0 || caption[cursor..at].Any(c => !char.IsWhiteSpace(c)))
                    return caption;
                result.Append(caption[cursor..at]);
                result.Append(entries[i].Unit?.Original ?? entries[i].Pending?.Original ?? entries[i].Unit?.Text);
                cursor = at + before.Length;
            }
            return result.Append(caption[cursor..]).ToString();
        }

        private static IReadOnlyList<int>? bisectOriginalSplit(TimedUnit unit, int segment, string? language)
        {
            var splits = Gameplay.SyllableSegments.SplitsFor(unit).ToList();
            if (segment < 0 || segment > splits.Count)
                return null;
            int lo = segment == 0 ? 0 : splits[segment - 1];
            int hi = segment == splits.Count ? unit.Text.Length : splits[segment];
            var candidates = Enumerable.Range(lo + 1, Math.Max(0, hi - lo - 1))
                .Where(c => OriginalLyricSubdivisions.SourceCut(unit, c, language).HasValue).ToArray();
            if (candidates.Length == 0)
                return null;
            splits.Insert(segment, candidates[(candidates.Length - 1) / 2]);
            return splits;
        }
    }
}
