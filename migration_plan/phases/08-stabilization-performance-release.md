# Phase 8 - Stabilization and Performance

**Current status: DONE FOR PRIVATE USE**

## Goal

Make Rushframe dependable on CPU-only Windows machines and ready for sustained local projects.

## Stabilization

- Analyze motion in a cancellable background job.
- Manual stabilization-strength control.
- Crop/zoom compensation control.
- Analysis cache keyed by media fingerprint and settings.
- Clear invalidation when source media changes.
- Draft preview may use reduced-resolution analysis; final export uses the selected final-quality settings.

## CPU and Memory Work

- Profile before optimizing.
- Reusable frame buffers and bounded queues.
- Proxy-first preview policy.
- Thumbnail, waveform, decoded-frame, and render caches with explicit size limits.
- Eviction policy documented and tested.
- Avoid per-frame managed allocations in hot paths.
- Keep media work off the WPF UI thread.
- Expose preview resolution and dropped-frame status.

## Long-Project Reliability

- Projects with hours of media and thousands of timeline items.
- Background autosave without UI stalls.
- Pause/resume/cancel render jobs.
- Recover interrupted exports where technically safe.
- Detect low disk space before proxy/export jobs.
- Handle disconnected, renamed, and replaced media.
- Clean temporary files after crashes.

## Local-Only Scope

No installer, signing, hosted deployment, public publishing, or automatic update work is part of this phase.

## Performance Gates

- UI input remains responsive during proxy generation and export.
- 540p proxy playback for a simple single-track timeline approaches real time on the owner's machine.
- Timeline pan/zoom remains responsive with 1,000 items.
- Memory remains bounded during a 30-minute playback/seek soak.
- Cancelled jobs release files and native resources.

Do not invent numeric success claims before the benchmark machine is recorded.

## Exit Gate

- Stabilization works and is cancellable.
- Cache limits and cleanup pass tests.
- Performance report identifies hardware, media, settings, and measurements.
- Soak and crash-recovery checks pass.
- No known data-loss bug remains open.
