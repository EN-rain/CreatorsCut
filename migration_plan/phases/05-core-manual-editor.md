# Phase 5 — Core Manual Editor

**Current status: (DONE FOR PRIVATE USE)**

## Goal

Deliver a complete basic editor that can replace the legacy editor for normal short-form manual editing.

## Required tracks

- Video.
- Image.
- Text.
- Audio/music.
- SFX.

## Required editing

- Import and drag media to timeline.
- Add, move, trim, split, cut, copy, paste, duplicate, delete, replace.
- Ripple mode on/off.
- Snapping on/off and configurable snap targets.
- Track lock, mute, solo, rename, reorder, duplicate, delete.
- Multi-select and compound edits.
- Markers and marker editing.

## Required visual properties

- Position X/Y.
- Scale X/Y and linked scale.
- Rotation.
- Anchor point.
- Opacity.
- Crop left/right/top/bottom.
- Fit, fill, reset, and center actions.

## Required audio properties

- Timeline start, source in/out, duration.
- Gain/volume.
- Fade in/out.
- Mute and solo.
- Waveform display.
- Multiple independent audio clips.

## Required text properties

- Text content.
- Font family, size, weight, alignment.
- Fill color.
- Outline color/width.
- Shadow color, offset, blur, opacity.
- Timeline duration.

## Reliability

- Autosave using atomic replacement.
- Crash recovery journal.
- Missing-media recovery.
- Undo/redo across every edit.
- Preview quality options: 360p, 540p, 720p, full.
- Final export always resolves originals.

## Acceptance scenario

A user can import three videos, one image, one music file, and one SFX; arrange and trim them; split clips; add styled text; reposition and crop an image; mix audio; save; reopen; preview; and export without using the legacy editor.

## Exit gate

- Model, commands, persistence, preview, export, undo, and tests exist for every listed feature.
- No disabled control is shown as available.
- Acceptance scenario passes on a clean Windows machine.
