# QA Strategy and Quality Gates

## Test levels

### 1. Domain unit tests

Fast, deterministic tests with no WPF, FFmpeg, file dialogs, or real media.

Cover:

- Media time arithmetic and boundaries.
- Track compatibility.
- Add, delete, move, trim, split, duplicate, ripple delete.
- Cut/copy/paste behavior.
- Marker, track, text, transition, effect, and property commands.
- Undo/redo identity preservation.
- Serialization round trips.
- Invalid operations and typed errors.

### 2. Application/service tests

Use fakes or temporary folders.

Cover:

- Project open/save orchestration.
- Autosave scheduling and shutdown.
- Migration backup/restore behavior.
- Selection-to-inspector updates.
- Command routing.
- FFmpeg process cancellation and error propagation.
- Cache lifecycle.

### 3. Media integration tests

Run real FFmpeg/FFprobe against tiny deterministic fixtures.

Cover:

- Probe.
- Thumbnail generation.
- Waveform generation.
- Proxy generation.
- Timeline export.
- Audio/video stream presence.
- Filters and composition.
- Cancellation.

### 4. WPF interaction tests

Cover UI-specific behavior:

- Focus and shortcuts.
- Mouse capture.
- Timeline drag/trim/pan/zoom.
- Context menus.
- Panel close/restore.
- Inspector input validation.
- Media add-to-timeline.

Use automated UI tests where stable; retain a manual check for input behavior that automation cannot reliably reproduce.

### 5. End-to-end tests

Operate Rushframe as the owner would:

1. Launch.
2. Import media.
3. Add media to timeline.
4. Edit.
5. Save.
6. Close/reopen.
7. Export.
8. Inspect output.

## Test environments

Minimum profiles:

### Profile A — Owner machine

- Windows 11.
- CPU-only expectations.
- No dedicated GPU required.
- Real FFmpeg tools used by Rushframe.
- The owner’s normal display scaling and mouse.

### Profile B — Clean private installation

- Fresh Windows user profile or clean VM.
- No development environment assumptions.
- No FFmpeg on PATH unless Rushframe deliberately depends on PATH.
- Empty AppData and cache directories.

### Profile C — Stress profile

- Low available disk space.
- Long paths and Unicode filenames.
- Offline/missing media.
- Limited memory and concurrent background jobs.

## Test data matrix

Maintain small fixtures for:

- H.264/AAC MP4.
- H.264 video without audio.
- Audio-only MP3, WAV, AAC, and FLAC.
- PNG with alpha.
- JPEG.
- Portrait, landscape, and square video.
- 23.976, 24, 25, 29.97, 30, and 60 FPS.
- Variable-frame-rate video.
- Odd dimensions.
- Very short clip under one second.
- Long clip.
- Corrupt/truncated media.
- Unsupported extension.
- Unicode, spaces, apostrophes, and long file paths.

## Severity

### Blocker

- Source media is modified or deleted.
- Project cannot open after a normal save.
- Autosave overwrites the only valid project with corrupt data.
- Export reports success but is unreadable or materially wrong.
- Ordinary edit crashes the process.

### High

- Undo/redo loses or alters unrelated edits.
- Timeline operation modifies the wrong clip or track.
- Export loses required audio.
- Preview/export materially disagree in a core workflow.
- Closed panel cannot be restored.
- Background job freezes the UI indefinitely.

### Medium

- Feature works with a reliable workaround.
- Non-core effect differs slightly from expected output.
- Performance degradation without data loss.
- Shortcut conflict or confusing selection behavior.

### Low

- Cosmetic defects.
- Minor text, spacing, or tooltip issues.

## Private-use release gates

### Gate A — Commit gate

- Relevant unit tests pass.
- Changed code builds.
- No new placeholder control is presented as functional.

### Gate B — Editor regression gate

Required when changing timeline, inspector, media panel, commands, persistence, preview, or export:

- Full automated test suite passes.
- Editor smoke suite passes.
- Save/reopen and undo/redo pass.
- One real export is inspected.

### Gate C — Real-project gate

Before using a build for important private editing:

- Back up the existing project.
- Complete a representative short-form edit.
- Close and reopen Rushframe twice.
- Export with original sources.
- Verify duration, visual layers, text, effects, and audio.
- Confirm source files are unchanged.

## Exit criteria for QA completion

A phase or feature is QA-complete only when:

- Happy path, boundary, invalid-input, cancellation, persistence, and undo behavior are covered where applicable.
- Preview and export have been compared.
- Tests identify the exact feature and expected result.
- Failures produce actionable messages.
- No unresolved Blocker or High defects remain in supported private workflows.
