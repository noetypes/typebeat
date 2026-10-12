# Prepared pull requests

These descriptions and branches are prepared locally. No GitHub pull requests have been created.

All three PR heads start directly from `Sparpp/typebeat:main` at `6a487badae1e337aea9f8d66282fb0fd92bb2320`. They can be reviewed independently.

| Topic | Prepared head on noetypes/typebeat | Description |
| --- | --- | --- |
| Original lyrics, Japanese conversion and Polyglot | `codex/original-lyrics-japanese-polyglot` | [PR draft](original-lyrics-japanese-polyglot.md) |
| Editor audio offset | `codex/editor-audio-offset` | [PR draft](editor-audio-offset.md) |
| Punctuation cell counting | `codex/punctuation-cell-counting` | [PR draft](punctuation-cell-counting.md) |

All pending development work is retained on `codex/fix-subdivision-timing`, including difficulty experiments, indicators, Peak WPM flooring and song-select display changes.

Each prepared branch passed its targeted checks: original lyrics/Japanese/Polyglot 1,226; editor offset 28; cell counting 206. The combined development checkout previously passed 5,012 nonvisual tests with 32 fixture-dependent skips. Its desktop Debug executable was rebuilt successfully and the game/ruleset assembly copies were verified after preparing the branches.
