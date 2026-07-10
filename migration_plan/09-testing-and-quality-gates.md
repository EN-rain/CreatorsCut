# Testing and Quality Gates

> **Status:** 🟡 PARTIAL — Architecture tests exist (7 tests in `CreatorCut.Domain.Tests`). Full test pyramid is pending per-phase implementation.

## Test pyramid

### Domain unit tests

Fast tests for:

- Time calculations.
- Split/trim/move/ripple behavior.
- Track compatibility.
- Speed mappings.
- Keyframe interpolation.
- Undo/redo.
- Serialization invariants.

These tests must not load WPF, SQLite, FFmpeg, or MLT.

### Application tests

Test command handlers with fakes:

- Save/autosave scheduling.
- Job cancellation.
- Selection updates.
- Error propagation.
- Revision checks.
- Preview invalidation.

### Infrastructure tests

Test:

- SQLite migrations.
- Atomic file replacement.
- Crash recovery.
- Cache cleanup.
- Offline media detection.
- Settings corruption fallback.

### Media integration tests

Use small committed/generated fixtures:

- Video with audio.
- Silent video.
- WAV and compressed audio.
- PNG with alpha.
- Variable-frame-rate source where practical.
- Odd dimensions and uncommon frame rate.

Test probe, thumbnail, waveform, proxy, seek, preview graph, and final render.

### Desktop interaction tests

Test at minimum:

- Panel close/restore/reset.
- Context menus.
- Shortcut focus safety.
- Middle-mouse timeline pan.
- Preview pan/zoom.
- Timeline clip hover text stability.
- Drag move and trim.
- Pointer-capture cancellation.
- Undo after drag.

## Golden render tests

For deterministic simple timelines, render short outputs and compare:

- Duration.
- Stream metadata.
- Selected frame hashes or perceptual similarity.
- Audio duration and loudness bounds.
- Presence of expected alpha/composition results.

Do not require byte-identical encoded files across codec versions unless the encoder and settings are fully pinned.

## Migration tests

Maintain sanitized legacy project fixtures.

Verify:

- Import does not mutate source files.
- IDs remain stable on repeated import where designed.
- Unknown fields produce warnings rather than silent deletion.
- Imported source ranges and transforms match legacy behavior.
- Missing files create offline assets, not crashes.

## Performance tests

Required synthetic workloads:

1. 1,000 timeline items.
2. 20 tracks.
3. Ten visible visual layers.
4. Five-minute audio waveform.
5. Repeated 100-seek sequence.
6. Five-minute playback soak.
7. Repeated project open/close.
8. Export cancellation and restart.

Track:

- UI-thread stalls over 50 ms.
- Memory growth.
- Seek p50/p95.
- Dropped frames.
- Cache size.
- Export completion/cancellation reliability.

## CPU-only test profile

At least one verification machine/profile must disable optional hardware acceleration.

Default acceptance target for core editing:

- 540p proxy playback usable at normal speed for a basic one- or two-layer edit.
- Timeline interactions remain responsive while background proxy generation runs.
- Heavy effects may drop frames but must not freeze input.
- Final export completes correctly using CPU.

## Quality gates per pull request

Every production change must pass:

```text
dotnet format --verify-no-changes
dotnet build -c Release
dotnet test -c Release
```

Native/media changes additionally run the relevant integration suite and leak/crash loop.

## Phase gate checklist

Before closing a phase:

- All acceptance criteria are checked.
- No placeholder button is presented as functional.
- New dependencies and licenses are documented.
- Schema changes include migration tests.
- User-visible errors are actionable.
- Cancellation is tested for long-running work.
- Logs contain enough context without exposing private media content unnecessarily.
- Known limitations are recorded.

## Bug severity

### Blocker

- Project corruption.
- Source media overwritten.
- App cannot open.
- Export silently incorrect.
- Native crash during ordinary operations.

### High

- Undo loses edits.
- Preview/export materially disagree.
- Closed panel cannot be restored.
- Input action triggers unrelated window behavior.
- Timeline operation modifies wrong clip.

### Medium

- Visual feedback wrong but data remains correct.
- Performance regression with workaround.
- Shortcut conflict.

### Low

- Cosmetic inconsistency.
- Tooltip wording.

Blockers and high-severity bugs prevent phase completion.
