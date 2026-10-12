# Keep gameplay audio calibration out of beatmap editing

Base: `Sparpp/typebeat:main`
Head: `noetypes:codex/editor-audio-offset`

## Description

A player's audio offset currently shifts editor playback and seeks, so authored lyric timing can inherit their gameplay calibration. Construct the editor's framed beatmap clock with offsets disabled: editor audio and displayed time always use the raw track position. Gameplay continues to apply the player's configured offset.

## Validation

- On this branch based directly on `Sparpp/main`: **28 targeted tests passed**, including eight visual offset tests and the editor playback-rate regressions.
- Positive and negative offsets are checked while seeking, playing, changing rate, changing the setting live and replacing the audio source; gameplay offset behavior is also verified.
