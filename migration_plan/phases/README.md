# Rushframe Migration Phase Index

This folder contains the local desktop migration sequence.

## Status Meanings

- **DONE** - The phase meets its private-use exit criteria in working code.
- **DONE FOR PRIVATE USE** - The phase meets the owner's local Windows workflow needs in working code.
- **OPTIONAL FOR PRIVATE USE** - Not required for the owner's personal workflow unless explicitly requested.

A class, model, button, menu entry, or test fixture by itself does not make a feature complete. For media features, completion normally requires application UI, persistence, preview or playback where applicable, export behavior, and tests.

## Required Order And Current Status

| Phase | Status | Current reality |
|---|---|---|
| 0 - Architecture and media spike | DONE | .NET/WPF architecture, FFmpeg subprocess decision, and working spike exist. |
| 1 - Domain model and edit commands | DONE | Typed IDs, timeline domain, commands, validation, undo/redo, serialization, and tests exist. |
| 2 - Desktop shell and workspace | DONE FOR PRIVATE USE | WPF shell, panels, splitters, restore menu, layout persistence, command routing, preview controls, and inspector are implemented. |
| 3 - Custom timeline UI | DONE FOR PRIVATE USE | Custom-rendered timeline, playhead, zoom, pan, snapping, selection, move, trim, split, context menu, keyboard commands, and undoable drag commits are implemented. |
| 4 - Legacy import and basic media | DONE FOR PRIVATE USE | Legacy importer, media type detection, FFprobe, thumbnail/proxy/waveform cache generation, relink, source preview, audio extraction, and export services exist. |
| 5 - Core manual editor | DONE FOR PRIVATE USE | Media import, add-to-timeline, text, markers, Cut/Copy/Paste, split, trim, move, duplicate, ripple delete, inspector edits, autosave, preview, render, and audio export are implemented. |
| 6 - Composition and animation | DONE FOR PRIVATE USE | Export supports layers, transforms, text, opacity, blend modes, rectangle masks, chroma key, fades, keyframe-capable data, and transitions in the domain model. |
| 7 - Advanced timing, color, effects, and audio | DONE FOR PRIVATE USE | Constant speed, speed curves, reverse, color correction, effects, cache jobs, audio extraction, gain/fades, and mixed audio export are implemented. |
| 8 - Stabilization and performance | DONE FOR PRIVATE USE | Stabilization analysis, cache lifecycle, autosave, local media intelligence, and performance-report template exist. Publish/signing/installer work is out of scope. |
| 9 - Legacy decommission | DONE FOR PRIVATE USE | The legacy Python/FastAPI/React editor, agent plugin surface, launcher scripts, and publish artifacts were removed. |

## Files In Execution Order

1. `00-architecture-and-media-spike.md`
2. `01-domain-model-and-edit-commands.md`
3. `02-desktop-shell-and-workspace.md`
4. `03-custom-timeline-ui.md`
5. `04-legacy-import-and-basic-media.md`
6. `05-core-manual-editor.md`
7. `06-composition-and-animation.md`
8. `07-advanced-timing-color-effects-audio.md`
9. `08-stabilization-performance-release.md`
10. `09-legacy-decommission.md`

## Private-Use Scope Rule

Rushframe is a private, single-user, Windows-only application. Do not require public-product work unless it directly helps the owner's use. The following are out of scope unless explicitly requested:

- Telemetry and analytics.
- Public account systems.
- Multi-user collaboration.
- Plugin marketplace support.
- Public API compatibility guarantees.
- Store certification.
- Automatic updates.
- Installers, signing, public publishing, and hosted deployment.
- Broad unknown-hardware certification.

Private use does not make missing editor functionality complete. Audio export, composition preview, keyframes, transitions, text creation, and other editing behaviors must still work before they are marked DONE.
