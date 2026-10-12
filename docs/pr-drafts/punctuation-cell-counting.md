# Count punctuation-generated spaces in difficulty and pace

Base: `Sparpp/typebeat:main`
Head: `noetypes:codex/punctuation-cell-counting`

## Description

Without Literate, `W-O-R-D` is played as `w o r d`: seven required cells. Difficulty previously counted four, and the Peak WPM curve omitted the three generated spaces. Count surviving spaces through the same whole-line projection used by gameplay, including collapsed hyphen/space runs, while retaining the authored unit timings and subdivisions.

The change corrects star-rating demand and the underlying Target WPM calculation. The Peak WPM curve now retains generated spaces at their interpolated timing positions; a three-word example correctly contributes 23 cells and qualifies for the curve's 16-cell floor. Difficulty version 7 refreshes cached ratings and stored target pace in existing libraries.

## Validation

- On this branch based directly on `Sparpp/main`: **206 targeted tests passed**, including cell-count, gameplay-target parity, star-rating, raw Target WPM, Peak WPM, Literate, chunked endurance, PP and stored-fact regressions.
- Cases cover `W-O-R-D`, repeated hyphens, hyphens next to authored spaces, edge hyphens, punctuation-only tokens, preserved subdivisions and freestyle weighting, and 0.75x/1x/1.5x curve comparisons.

## Companion update

The server mirrors of the difficulty model and pace curve need the same counting change to retain client/server rating and website pace parity. This branch contains the client-side fix.
