# Generate original lyrics and refine Japanese Polyglot input

Base: `Sparpp/typebeat:main`
Head: `noetypes:codex/original-lyrics-japanese-polyglot`

## Description

Romanized maps that lack originals now offer **Generate lyrics** directly below **Generate timing** in beatmap setup. The selected beatmap language controls availability; English disables the button and retains a hover explanation. Generation adds line and word originals in one undo step, preserves existing originals, romanized text and timings, and rejects results if the map or its language changes during the operation. Language undo also refreshes the setup dropdown.

Japanese conversion runs offline through the bundled IPAdic dictionary, including conjugated forms, compound readings across timing words, context costs and kana fallbacks. Other languages use LRCLIB and align matching original-script lyrics to the map. Dictionary spellings remain suggestions: homophones and an artist's kanji/kana choices require review in the lyric editor. The included Watashi wa Saitei comparison documents those limits and the corrected conversion cases.

Japanese Polyglot play removes authored spaces between Japanese characters while preserving mapper word windows. With Space to Skip enabled, an unfinished or incorrect Japanese reading skips the remainder of its word; completed readings still use space for confirmation. A new replay flag preserves the previous single-character skip behavior for older replays, including playback and Puppeteer transformations.

## Validation

- On this branch based directly on `Sparpp/main`: **1,226 targeted tests passed**, covering generation, Japanese input and word timing, Polyglot, replay era handling, and the setup button UI.
- Tests cover cancellation, stale results, English/language gating, undo, serialization, dictionary conversion without network access, and Japanese word skip/reclaim behavior.
