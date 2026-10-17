# Prepared pull requests

## Combined development update

[Improve Polyglot gameplay, editing, scoring, and corrections](https://github.com/Sparpp/typebeat/pull/43) is open against `Sparpp/typebeat:main` from `noetypes:codex/polyglot-gameplay-editing-scoring`.
The [saved description](polyglot-editor-difficulty-updates.md) matches the published PR and omits validation text.

### Local-only exclusions

Experimental difficulty tags and editor preview statistics normalized to 1x remain in the local development checkout and are excluded from the PR branch. The PR retains the shared pace, complexity and rhythm statistics used by the other changes.

The PR branch incorporates upstream `b7e9d292a91b81ae5948e040143bcb4770332934`; head `c95b052ea12a665c2178f6acb9f37cc95424e468`.

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
