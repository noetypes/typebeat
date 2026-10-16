# Prepared pull requests

## Combined development update

The current combined PR draft targets `Sparpp/typebeat:main` from `noetypes:codex/polyglot-editor-difficulty-updates`:
[Improve original-script editing, Polyglot gameplay and difficulty ratings](polyglot-editor-difficulty-updates.md).
This is the combined review branch; no PR is opened for this update.

## Historical focused drafts

The descriptions below record the earlier focused PR preparation. The original-lyrics and punctuation-cell-counting PRs have since been merged into Sparpp/main.

Those three PR heads were prepared directly from `Sparpp/typebeat:main` at `6a487badae1e337aea9f8d66282fb0fd92bb2320`. They were prepared for independent review.

| Topic | Prepared head on noetypes/typebeat | Description |
| --- | --- | --- |
| Original lyrics, Japanese conversion and Polyglot | `codex/original-lyrics-japanese-polyglot` | [PR draft](original-lyrics-japanese-polyglot.md) |
| Editor audio offset | `codex/editor-audio-offset` | [PR draft](editor-audio-offset.md) |
| Punctuation cell counting | `codex/punctuation-cell-counting` | [PR draft](punctuation-cell-counting.md) |

At the time, pending development work was retained on `codex/fix-subdivision-timing`, including difficulty experiments, indicators, Peak WPM flooring and song-select display changes.

Each prepared branch passed its targeted checks: original lyrics/Japanese/Polyglot 1,226; editor offset 28; cell counting 206. The combined development checkout previously passed 5,012 nonvisual tests with 32 fixture-dependent skips. Its desktop Debug executable was rebuilt successfully and the game/ruleset assembly copies were verified after preparing the branches.
