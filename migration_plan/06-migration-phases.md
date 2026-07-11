# Migration Phases

Each phase must pass before the next begins. This plan is scoped to the owner's local Windows desktop workflow.

## Phase 0 - Architecture and media spike

> **Status: DONE**

- Create the .NET solution skeleton.
- Add architecture tests for dependency direction.
- Run the media engine spike from `05-media-engine-decision-spike.md`.
- Decide WPF docking strategy.
- Record dependency versions for local development.

Exit criteria:

- `dotnet build` succeeds.
- Domain has no WPF/native/database references.
- Media backend decision is written and supported by measurements.
- A minimal desktop executable opens and shuts down cleanly.

## Phase 1 - Pure domain model and editing commands

> **Status: DONE**

- IDs and canonical media time.
- Project, sequence, tracks, media assets, clips, markers.
- Selection-independent edit commands.
- Add/remove/move/trim/split/duplicate commands.
- Undo/redo stack.
- Validation and typed errors.
- Versioned JSON serialization.

## Phase 2 - Desktop shell and workspace

> **Status: DONE FOR PRIVATE USE**

- Main WPF window.
- Docked Media, Preview, Inspector, Timeline, Tasks/Campaign panels.
- Close, restore, resize, and reset panels.
- Save workspace layout separately from project data.
- Menus and command routing.
- Keyboard focus management.
- Crash-safe settings storage.

## Phase 3 - Custom timeline UI

> **Status: DONE FOR PRIVATE USE**

- Custom-rendered timeline surface.
- Track headers, ruler, playhead, markers, clips, waveforms, thumbnails.
- Selection, drag move, trim, snapping, scrolling, zooming.
- Context menus and keyboard commands.
- Drag/drop from Media panel.
- Visual feedback for invalid drops.

## Phase 4 - Legacy import and basic media

> **Status: DONE FOR PRIVATE USE**

- Import old project metadata where supported.
- Probe media.
- Generate thumbnails, waveforms, and proxies.
- Basic preview and export.
- Offline media relink workflow.

## Phase 5 - Core manual editor

> **Status: DONE FOR PRIVATE USE**

- Video/image/audio/text tracks.
- Add, move, trim, split, cut, copy, paste, duplicate, delete.
- Ripple option and snapping.
- Position, scale, rotation, anchor, opacity, crop.
- Multiple audio clips, gain, fades, mute/solo.
- Text creation and basic styling.
- Markers.
- Proxy preview and original-quality export.
- Autosave and crash recovery.

## Phase 6 - Composition and animation

> **Status: DONE FOR PRIVATE USE**

- Overlay tracks and picture-in-picture.
- Layer ordering.
- Blend modes.
- Masks.
- Chroma key.
- Keyframes and easing data.
- Transitions.
- Text animations.

## Phase 7 - Advanced timing, color, effects, and audio

> **Status: DONE FOR PRIVATE USE**

- Constant and curve speed.
- Freeze frame.
- Reverse.
- Basic color correction.
- HSL and curves data.
- Filter/effect registry.
- Audio extraction.
- Pitch and EQ presets.

## Phase 8 - Stabilization and performance

> **Status: DONE FOR PRIVATE USE**

- Stabilization analysis and strength controls.
- Cache lifecycle.
- Memory and CPU profiling.
- Long-project performance.
- Render recovery.
- Local media-intelligence tab.

Public packaging, installers, signing, hosted deployment, and external publishing are out of scope.

## Phase 9 - Legacy decommission

> **Status: DONE FOR PRIVATE USE**

The legacy Python/FastAPI/React editor, agent plugin surface, launcher scripts, sample agent projects, and publish artifacts were removed from the normal repository surface.
