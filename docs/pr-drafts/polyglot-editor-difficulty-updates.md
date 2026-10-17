# Improve Polyglot gameplay, editing, scoring, and corrections

Base: `Sparpp/typebeat:main`
Head: `noetypes:codex/polyglot-gameplay-editing-scoring`
Status: open, https://github.com/Sparpp/typebeat/pull/43

## Description

This update keeps original-script lyrics aligned with romanized timing, adds Japanese character timing with input-based accuracy, and calculates Polyglot difficulty from the inputs required by the selected language. Retyping an already-judged character now retains the better timing grade. The update also improves editor controls, fixes punctuation timing across pauses, and makes Polyglot ranked for every selectable language except Chinese and Korean.

## Original-script lyrics and subdivisions

- Split or merge linked original lyrics with their romanized words when the readings align. Show original-script `|` markers and spread characters around their mapped timing boundaries.
- Preserve authored romanized subdivisions even when they fall inside one Japanese character. In the original-script view, those subdivisions have gray handles and no invalid text marker. Newly generated import subdivisions follow original-character boundaries when originals are available.
- Allow original-script subdivisions to create or move corresponding romanized boundaries without discarding unrelated romanized cuts. Preserve valid partial matches, such as the boundary between `jigoku` and `goku`, even when other cuts cannot map to individual kanji.
- Support mapper-defined links for custom readings: English `o|pen` can correspond to its corresponding loanword as `オ|ープン` or `オー|プン` based on mapper preference. Persist these links through save/reload, undo, clipboard operations, retiming, word splitting/merging and gameplay. Removing an original-script link leaves its romanized cut intact.
- Allow the addition of empty per-word original-script fields for non-English maps. When generating originals, prioritize an attached `.txt`, `.lrc`, `.elrc` or `.ttml` lyrics file and align it to the map, replacing matched existing originals. Japanese generation uses the bundled dictionary offline.
- Preserve authored Japanese readings during spelling corrections, including alternate kanji readings and compounds spanning timed words. Prefer a researched set of common hiragana spellings for demonstratives, greetings, conjunctions and adverbs, using approved dictionary surface/reading pairs to preserve homophones. Keep English phrases intact during Japanese dictionary conversion, including contractions and punctuation. Convert committed `[ ] . ,` input to `「 」 。 、` while preserving IME composition and Japanese punctuation in the original lyrics.
- Reduce Japanese editor loading and subdivision-display work through bounded reading/alignment caches, direct dictionary lookups and cached handle applicability.

## Japanese Polyglot gameplay and scoring

- Derive character timing from applicable romanized subdivisions and explicit original-script links. Join spaces between Japanese characters for gameplay. Accept the displayed furigana as kana input for kanji, including mixed kana/romaji input, and brighten each completed kana in the furigana. Backspace removes pending progress without awarding grades.
- Grade each accepted input when its character is completed. With normal timing windows, the first input receives Great within **±150 ms of character start**; later inputs receive Great from **150 ms before character start through 150 ms after character end**. Finishing early is allowed, and one character can receive multiple judgements if that character required multiple inputs to type.
- Award combo only on character completion, using its **minimum accepted romaji input count**, independent of the spelling the player used. Map maximum combo and the pp combo bonus use the same minimum count. Accuracy and score use the actual accepted input count; direct kana input receives one grade while retaining the romaji-based combo award. Unfinished or uncorrected characters receive misses based on their minimum input count.
- Allow Enter to skip the rest of the line during an incomplete Japanese reading, clearing pending input while preserving normal deadline misses. Keep Enter confirmation for complete trailing-n readings.
- Use the minimum accepted romaji spelling for Japanese Polyglot autoplay, confirming ambiguous final `n` inputs without an extra romaji input or judgement. Keep autoplay and Puppeteer replay transformations consistent with these rules. Record the new timing, scoring and kana-input behavior in replay configuration while retaining playback of earlier Japanese input eras.

## Judgement correction when retyping

- Compare the timing grade from retyping a backspaced character with its existing grade and retain the higher one. A **Meh** replaced by an in-time **Great** becomes Great; a Great remains Great after a retype as per the current default behaviour.
- Update the existing judgement's accuracy, score and timing feedback when it improves, without awarding extra combo or applying another health award. Repeated retyping cannot duplicate the character's scoring contribution.
- Apply Japanese corrections only after the character is completed again. For the same number of accepted inputs, retain the higher grade for each corresponding input. If a different valid spelling changes the input count, retain the complete attempt with higher accuracy. Combo remains based on the character's minimum romaji input count.
- Record the correction rule in new replays, preserve the original rule for older replays, and restore corrected judgements consistently when rewinding.

## Editor and language controls

- Insert Pause uses **P** and merges words if more than one is selected, carrying pauses across their gaps. **Quantize (Q)** snaps selected words to the grid.
- Keep editor audio at zero gameplay offset, including after timing/rate changes. Testing a map in the editor with **Lyrics: Original** selected enables Polyglot when originals are available.
- Keep punctuation and other required characters on their authored side of pauses with **Hard Rock + Literate**, including punctuation runs, multiple pauses and subdivided words. Preserve existing letter targets.
- Editor and import language menus begin with English, Japanese, Korean, Russian, French and Spanish. The remaining languages are alphabetical, followed by Instrumental and Other.
- Expand the selectable Germanic, Slavic, Uralic and Romance languages without renumbering existing metadata IDs.

## Difficulty, pp and display

- Calculate supported Latin/Cyrillic Polyglot SR and pp from original-script characters. Use a fixed native-keyboard baseline: composed accents add demand at the Literate Shift surcharge rate, native-layout accented letters are exempt, and uppercase letters retain the Literate Shift cost including Cyrillic. Accept plain `e` for `ë` without a compose surcharge.
- Rate Japanese as the minimum accepted romaji input stream at the original characters' timing windows. Unsupported scripts, including Chinese and Korean, retain romanized SR and receive no pp estimate. Recalculate Polyglot Peak, Target and Average WPM from required inputs: minimum accepted romaji for Japanese and native-layout letter/compose inputs for supported Latin/Cyrillic scripts. Refresh all three figures when switching Polyglot and Literate combinations or playback rates, including live custom-rate slider adjustments; unsupported scripts retain romanized pace statistics. Removed Japanese spaces contribute no WPM or SR input, while retained spaces and timed pauses follow the played stream.
- Rank Polyglot at **1.0x** for every dropdown language except Chinese and Korean, which remain unranked and local-only. Use the current map language for mod-selection labels, submission, online leaderboards and ranked pp eligibility.
- Show local pp estimates for other unranked mods while retaining their unranked score status and existing map, pass and rate restrictions.
- Use the published base SR tuning with the typability amendments: score punctuation-separated sequences such as `F. (F) B. (B) I. (I)` through a bigram fallback, lower the typability floor to **z = -5**, and add **1% final SR per negative whole-map z-score** when typability coverage is sufficient, including Literate. For example, z = −1.9 multiplies SR by 1.019.
- Surcharge each shifted symbol under Literate, sharing the surcharge only across identical consecutive symbols.
- Floor displayed Peak WPM at Average WPM. Refresh song-select pace, complexity and rhythm statistics for the selected mods and rate, and prevent stale background results from replacing newer selections.
- Add configurable character-indicator lead times.
