# Phase 1 — Domain Model and Edit Commands

## Goal

Create an editor model that is independent of WPF, FFmpeg, MLT, files, and databases.

## Required model

- Stable typed IDs for projects, sequences, tracks, items, assets, effects, and keyframes.
- Integer/rational media time; do not store authoritative edit positions as UI pixels.
- Tracks for video, image, text, audio, and overlays.
- Clips with source range, timeline range, transform, opacity, enabled state, and effect references.
- Markers, transitions, keyframes, masks, and project metadata as extensible models.

## Required commands

- Add, remove, move, trim, split, duplicate, copy, paste.
- Track add/remove/reorder.
- Property edit commands.
- Compound transactions for drag operations.
- Undo and redo preserving original IDs.

## Rules

- Array indexes are never persistent identities.
- UI selection is not stored inside the domain object.
- Commands validate before mutation.
- Failed commands leave the model unchanged.
- Every destructive command has a deterministic inverse.
- Unknown extension fields must survive serialization when possible.

## Tests

- Split preserves source duration.
- Trim rejects invalid ranges.
- Move validates track compatibility.
- Delete/undo restores exact identity and placement.
- Save/load round trip is equivalent.
- Compound command is atomic.
- Time conversion remains stable over repeated operations.

## Exit gate

- Domain tests run without WPF or any native media DLL.
- Undo/redo covers all implemented commands.
- Versioned project serialization is stable.
- Architecture tests prevent infrastructure references from entering Domain.
