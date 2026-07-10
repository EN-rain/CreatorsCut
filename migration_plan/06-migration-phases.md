# Migration Phases

Each phase must pass before the next begins.

## Phase 0 — Architecture and media spike

### Tasks

- ✅ Create the .NET solution skeleton.
- ✅ Add architecture tests for dependency direction.
- ✅ Run the media engine spike from `05-media-engine-decision-spike.md`.
- ✅ Decide WPF docking strategy.
- ✅ Decide installer format after a minimal native dependency packaging test.
- ✅ Record dependency versions and licenses.

### Exit criteria

- ✅ `dotnet build` succeeds.
- ✅ Domain has no WPF/native/database references. (verified by 7 architecture tests)
- ✅ Media backend decision is written and supported by measurements.
- ✅ A minimal desktop executable opens and shuts down cleanly.

## Phase 1 — Pure domain model and editing commands

> **Status:** ✅ COMPLETE — Domain model, IDs, commands, undo stack, typed errors, serialization.

### Tasks

Implement:

- ✅ IDs and canonical media time. (ProjectId, SequenceId, TrackId, TimelineItemId, MediaAssetId, EffectInstanceId, KeyframeId, MediaTime record struct with operators)
- ✅ Project, sequence, tracks, media assets, clips, markers. (Project, Sequence, Track, TimelineItem, MediaAsset, Marker)
- ✅ Selection-independent edit commands. (IEditCommand interface + EditResult with typed DomainError hierarchy)
- ✅ Add/remove/move/trim/split/duplicate commands. (SplitClipCommand, TrimClipCommand, MoveClipCommand, DeleteClipCommand, DuplicateClipCommand, AddClipCommand)
- ✅ Undo/redo stack. (UndoRedoStack, LinkedList-backed, max depth 100)
- ✅ Validation and typed errors. (DomainError, ItemNotFoundError, TrackNotFoundError, InvalidOperationError, ValidationError; TrackCompatibility rules)
- ✅ Versioned JSON serialization. (ProjectSerializer with MediaTimeConverter, save/reload round-trip)

### Required tests

- ✅ Split preserves total source range.
- ✅ Trim cannot produce zero/negative duration.
- ✅ Delete and undo restore the same IDs and positions.
- ✅ Move between tracks validates media compatibility.
- ✅ Save/reload is equivalent.
- ✅ Unknown extension data survives round-trip where supported.

### Exit criteria

- ✅ Domain tests pass without FFmpeg or WPF. (37/37 domain tests pass)
- ✅ No item identity uses an array index. (all typed IDs)
- ✅ All destructive commands are undoable.

## Phase 2 — Desktop shell and workspace

> **Status:** ⬜ NOT STARTED

### Tasks

- ⬜ Main WPF window.
- ⬜ Docked Media, Preview, Inspector, Timeline, Tasks/Campaign panels.
- ⬜ Close, restore, move, resize, and reset panels.
- ⬜ Save workspace layout separately from project data.
- ⬜ Menus and command routing.
- ⬜ Keyboard focus management.
- ⬜ Crash-safe settings storage.

### Interaction requirements

- Closing a panel always leaves a visible restore path through `View > Panels`.
- Right-click on panels must not invoke Windows/browser behavior outside the app.
- Middle mouse over timeline pans timeline only.
- Middle mouse over preview pans preview only when zoomed.
- Mouse wheel behavior follows `07-editor-interactions.md`.

### Exit criteria

- Workspace operations have UI tests.
- The app never starts with all editing panels unreachable.
- Layout corruption falls back to a default layout.

## Phase 3 — Custom timeline UI

> **Status:** ⬜ NOT STARTED

### Tasks

- ⬜ Virtualized/custom-drawn timeline surface.
- ⬜ Track headers, ruler, playhead, markers, clips, waveforms, thumbnails.
- ⬜ Selection and marquee selection.
- ⬜ Drag move, edge trim, snapping, scrolling, zooming.
- ⬜ Context menus.
- ⬜ Keyboard commands.
- ⬜ Drag/drop from Media panel.
- ⬜ Visual feedback for invalid drops.

### Exit criteria

- Timeline handles a synthetic project with at least 1,000 items without creating 1,000 heavyweight WPF controls.
- Input actions map to application commands.
- Undo/redo works for drag edits.
- Hover text remains readable and stable.

## Phase 4 — Legacy import and basic media

> **Status:** ⬜ NOT STARTED

### Tasks

- ⬜ Import current project metadata, registries, timelines, tasks, campaigns, and audit data.
- ⬜ Probe media.
- ⬜ Generate thumbnails, waveforms, and proxies.
- ⬜ Basic preview and export.
- ⬜ Offline media relink workflow.

### Exit criteria

- Import never modifies the source legacy project.
- Migration report lists mapped, skipped, and unknown fields.
- Imported basic video/music timelines preview and export correctly.

## Phase 5 — Core manual editor release

> **Status:** ⬜ NOT STARTED

### Features

- ⬜ Video/image/audio/text tracks.
- ⬜ Add, move, trim, split, cut, copy, paste, duplicate, delete.
- ⬜ Ripple option and snapping.
- ⬜ Position, scale, rotation, anchor, opacity, crop.
- ⬜ Multiple audio clips, gain, fades, mute/solo.
- ⬜ Text creation and basic styling.
- ⬜ Markers.
- ⬜ Proxy preview and original-quality export.
- ⬜ Autosave and crash recovery.

### Exit criteria

- All features have model, command, persistence, preview, export, and test coverage.
- A user can complete a short manual edit without the legacy editor.

## Phase 6 — Composition and animation

> **Status:** ⬜ NOT STARTED

### Features

- ⬜ Overlay tracks and picture-in-picture.
- ⬜ Layer ordering.
- ⬜ Blend modes.
- ⬜ Masks.
- ⬜ Chroma key.
- ⬜ Keyframes and easing editor.
- ⬜ Transitions.
- ⬜ Text animations.

### Exit criteria

- Preview/export parity tests exist for representative compositions.
- Ten visual layers are supported by the model.
- CPU fallback and draft preview behavior are documented.

## Phase 7 — Advanced timing, color, effects, and audio

> **Status:** ⬜ NOT STARTED

### Features

- ⬜ Constant and curve speed.
- ⬜ Freeze frame.
- ⬜ Reverse.
- ⬜ Basic color correction.
- ⬜ HSL and curves.
- ⬜ Filter/effect registry.
- ⬜ Motion blur draft behavior.
- ⬜ SFX/music library integration.
- ⬜ Audio extraction.
- ⬜ Pitch and EQ presets.

### Exit criteria

- Presets remain editable after application.
- Heavy effects are cancellable and do not freeze the UI.
- Final exports are deterministic for fixed inputs/settings.

## Phase 8 — Stabilization and optimization

> **Status:** ⬜ NOT STARTED

### Tasks

- ⬜ Stabilization analysis and strength controls.
- ⬜ Cache lifecycle.
- ⬜ Memory and CPU profiling.
- ⬜ Long-project performance.
- ⬜ Render recovery.
- ⬜ Installer/update hardening.

### Exit criteria

- Stabilization is a background cancellable job.
- Cache limits are enforced.
- Long-running soak tests pass.

## Phase 9 — Legacy decommission

> **Status:** ⬜ NOT STARTED

The legacy editor can be retired only when:

- Desktop import covers supported existing projects.
- Feature parity for normal CreatorCut workflows is verified.
- The desktop application has stable release packaging.
- Backup documentation exists.
- The user explicitly approves removal.

Until then, do not delete the Python/React application.
