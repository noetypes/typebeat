# Improve original-script editing, Polyglot gameplay and difficulty ratings

Base: `Sparpp/typebeat:main`
Head: `noetypes:codex/polyglot-editor-difficulty-updates`
Status: prepared locally; no pull request opened.

## Description

Original-script lyrics now stay aligned while editing their romanized words. Splitting or merging linked words carries their original characters with them when the readings align. Original-script subdivision markers can be edited directly and update the matching romanized cuts; authored romanized cuts inside a single original character remain intact without displaying an invalid original-script marker. Automatic import subdivisions follow original-character boundaries when available.

The editor can add empty per-word original-script fields, prioritize an attached lyrics file when generating originals, and test the Polyglot version when the original-lyrics view is selected. Japanese spelling corrections preserve authored readings, common dictionary suggestions favor natural kana spellings, and committed `[ ] . ,` input becomes `「 」 。 、` without disrupting IME composition. Japanese Polyglot uses character timings, with independent start and finish judgements for characters needing more than two inputs; uncorrected errors miss both judgements. Replay configuration preserves the previous timing and input eras, including the upstream word-aware replay fix and Puppeteer transformations.

## Editor and language controls

- Insert Pause uses **P** and merges selected words, carrying subdivisions or rests across their gaps. **Quantize (Q)** snaps selected words to the grid.
- Editor preview starts without the player's gameplay audio offset, including after timing/rate changes.
- Editor and import language menus begin with English, Japanese, Korean, Russian, French and Spanish. The remaining languages are alphabetical, followed by Instrumental and Other. Unspecified, Low German and Swiss German are omitted from the menus; stored metadata IDs remain compatible.
- Extend the selectable Germanic, Slavic, Uralic and Romance languages and their native-keyboard letter handling.

## Difficulty, pp and display

- Rate supported Latin/Cyrillic Polyglot lyrics using original-script characters and native-layout modifier costs; rate Japanese using its minimum accepted romaji input stream. Unsupported original scripts, including Chinese and Korean, retain the romanized SR and receive no pp estimate.
- Show local pp estimates for other unranked mods while retaining unranked score status. Accept plain `e` for `ë`.
- Keep the published SR calibration, score punctuation-separated letter sequences through the typability fallback, lower the typability floor to **z = -5**, and add **1% final SR per negative whole-map z-score**, including Literate.
- Charge Shift for each shifted symbol, sharing the surcharge only across identical consecutive symbols.
- Carry the local Peak WPM floor, pace metrics and song-select display work, plus configurable character indicator lead times. Debug builds retain difficulty tags and normalized editor statistics.

Includes the latest `Sparpp/main` merge, including the separate-all-difficulties setting and stored-pp repricing version update.

## Validation

- Full nonvisual suite: **5,239 passed**, **32 skipped** because their local fixtures are unavailable.
- **75 targeted editor/UI tests passed**, covering language selection/import, original-script input and IME, lyric generation, editor offset, pause/quantize controls, Polyglot preview and Japanese gameplay.
- **80 additional autoplay/replay checks passed** after integrating the upstream Japanese input era with character timing, including retention of both flags through Puppeteer transformations and autoplay headers.
- Desktop rebuilt with `dotnet build typebeat.Desktop/typebeat.Desktop.csproj --configuration Debug --framework net8.0`; updated game and ruleset DLLs verified byte-for-byte in the executable's output directory.
- Whitespace/conflict-marker checks passed. Sparpp/main was verified at `5c3a6f042e3844dd1c608a6ff3040f4f2161d059` before pushing.
