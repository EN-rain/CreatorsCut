# Phase 3 — Custom Timeline UI

## Goal

Implement the high-performance manual editing surface. Do not create one heavyweight WPF control per clip.

## Required behavior

- Custom-drawn or virtualized ruler, tracks, clips, thumbnails, waveforms, markers, keyframes, and playhead.
- Horizontal and vertical scrolling.
- Wheel scroll; Shift+wheel horizontal scroll; Ctrl+wheel zoom around cursor.
- Middle-mouse pan without affecting the application window.
- Single, additive, range, and marquee selection.
- Drag move between compatible tracks.
- Left/right edge trim with live feedback.
- Snapping to playhead, markers, cuts, and clip edges.
- Drag/drop from Media panel.
- Hover tooltip that does not resize, blur, or cover clip text.
- Visible invalid-drop and collision feedback.

## Context menus

Clip: Cut, Copy, Split at Playhead, Duplicate, Delete, Detach Audio, Replace Media, Properties.

Empty track: Paste, Add Track Above/Below, Add Text, Add Audio, Add Marker.

Track header: Rename, Mute, Solo, Lock, Duplicate, Delete, Move Up/Down.

## Architecture

- Convert pixels to canonical media time through one viewport service.
- Dispatch domain commands; do not mutate models from mouse handlers.
- Coalesce drag updates into one undo transaction.
- Keep selection and hover in presentation state.

## Performance tests

- 1,000 synthetic items.
- Continuous zoom and pan.
- Drag while thumbnails/waveforms load.
- Memory remains bounded after repeated project opens.

## Exit gate

- All timeline operations are undoable.
- Context-menu and keyboard commands call the same application commands.
- Timeline remains responsive at the synthetic load target.
