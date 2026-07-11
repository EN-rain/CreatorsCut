# QA Automation Plan

## Goals

Automate deterministic behavior and reserve manual QA for visual quality, mouse feel, playback feel, and OS-specific interactions.

## Proposed test projects

```text
/tests
  Rushframe.Domain.Tests
  Rushframe.Application.Tests
  Rushframe.Infrastructure.Tests
  Rushframe.Media.Tests
  Rushframe.Desktop.Tests
  Rushframe.EndToEnd.Tests
  Fixtures/
```

Do not place real private media in the repository. Generate synthetic fixtures or use sanitized tiny files.

## Priority 1 — immediate automation

1. FFmpeg media-service integration tests.
2. Timeline export metadata tests.
3. Audio-presence regression test.
4. Main command-routing tests for Cut/Copy/Paste/Delete/Split.
5. Drag move and trim command integration tests.
6. Inspector property apply/undo/save tests.
7. Autosave and corrupt-recovery tests.
8. Missing-media and invalid-project tests.

## Priority 2

- Proxy/thumbnail/waveform tests.
- Cache eviction/corruption tests.
- FFmpeg cancellation and orphan-process tests.
- Workspace corruption fallback.
- Timeline 1,000-item performance benchmark.
- Golden render comparisons.

## Priority 3

- Automated WPF input tests.
- Full end-to-end launch/import/edit/save/export flow.
- Local clean-machine launch tests if the app is copied to another Windows machine.

## Fixture generation

Use FFmpeg lavfi to generate deterministic sources:

- Color bars with timestamp overlay.
- Solid red/green/blue clips.
- Moving test pattern.
- Sine tones at known frequencies.
- Silence.
- Stereo channels with different tones.
- PNG with transparent regions.

Keep fixtures 1–5 seconds where possible.

## Media assertions

Create test helpers for:

- Running FFprobe and parsing JSON.
- Reading stream presence and metadata.
- Extracting a frame at a known timestamp.
- Computing perceptual image difference.
- Measuring audio peak, loudness, and tone presence.
- Detecting orphan FFmpeg processes started by the test.

## Test naming

Use:

```text
Method_State_ExpectedResult
```

Examples:

```text
SplitClip_PlayheadAtClipStart_ReturnsValidationError
ExportTimeline_WithAudioTrack_ContainsAudioStream
WorkspaceLayout_CorruptJson_LoadsDefaultLayout
```

Reference matrix IDs in comments or traits:

```csharp
[Trait("QA", "EDT-012")]
```

## Isolation rules

- Every filesystem test uses a unique temporary directory.
- Tests must not read or modify the owner’s AppData.
- Tests must not depend on test execution order.
- FFmpeg processes receive cancellation timeouts.
- Clean all generated outputs in `finally` blocks.
- Never modify source fixtures.

## UI automation boundaries

Automate stable observable behavior:

- Command invocation.
- Panel visibility.
- Selection and inspector state.
- Context menu presence.
- Keyboard shortcut routing.

Keep manual:

- Smoothness.
- Cursor feel.
- Visual alignment.
- Perceived preview quality.
- Complex drag timing where automation is flaky.

## CI/local execution groups

### Fast

```powershell
dotnet test Rushframe.slnx -c Release --filter "Category!=Media&Category!=UI&Category!=Performance"
```

### Media

```powershell
dotnet test Rushframe.slnx -c Release --filter "Category=Media"
```

### UI

Run interactively on Windows with a visible desktop session.

### Performance

Run manually or on a stable owner-machine baseline. Store measurements with date, commit, CPU, RAM, and FFmpeg version.

## Failure artifacts

On failure retain:

- Test project JSON.
- FFmpeg arguments.
- Sanitized stderr.
- Output probe JSON.
- Expected and actual selected frames.
- Difference image where useful.
- Timing and environment metadata.

Do not retain large or private source media automatically.

## Automation completion rule

A test is not complete if it only checks that no exception occurred. Assert the resulting model, file, stream, duration, frame, audio, or UI state.
