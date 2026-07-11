# Project Recovery, Reliability, and Performance Tests

## 1. Save and persistence

Test:

- New project save.
- Save existing project repeatedly.
- Save after every supported command.
- Save with no media.
- Save with offline media.
- Save with effects, masks, transitions, text, audio, markers, and stabilization settings.
- Save to paths with Unicode, spaces, apostrophes, and long names.
- Save failure caused by read-only path or unavailable drive.

Verify:

- Project remains valid after failure.
- Existing valid project is not replaced by partial data.
- IDs and time values remain exact.
- Unknown extension data follows documented preservation policy.

## 2. Autosave and crash recovery

Required scenarios:

1. Autosave after edit.
2. Multiple autosaves without changes.
3. Close normally after autosave.
4. Force-kill Rushframe after autosave.
5. Corrupt newest autosave while older valid autosave exists.
6. Autosave directory unavailable.
7. Autosave while export runs.
8. Autosave during rapid edits.

Expected:

- Main project is never silently overwritten by recovery data.
- Recovery identifies project, timestamp, and source.
- Invalid autosave does not prevent app startup.
- Recovery restores a self-consistent project.

## 3. Legacy migration and backup

Test:

- Valid legacy project.
- Missing timeline/registry/task files.
- Unknown fields.
- Missing source media.
- Repeated migration of same project.
- Migration interrupted midway.
- Backup and restore.
- Source legacy folder hash before/after migration.

Expected:

- Source legacy data is unchanged.
- Report lists mapped, skipped, unknown, and failed data.
- Offline assets remain represented.
- Failure can be retried safely.

## 4. Offline and changed media

Test:

- Source moved.
- Source renamed.
- Source deleted.
- Source replaced by different file at same path.
- Proxy exists while original is offline.
- Original returns after being offline.

Expected:

- Project opens.
- Offline state is visible.
- Relink operation does not change clip timing/properties.
- Export refuses or clearly handles missing originals.
- Source replacement detection policy is documented.

## 5. Cache tests

Cover proxy, thumbnail, waveform, and temporary render caches.

Test:

- Cache miss and hit.
- Duplicate requests.
- Concurrent requests for same derivative.
- Cache size limit.
- Old-entry eviction.
- Locked file during cleanup.
- Corrupt cached derivative.
- Manual clear cache.
- Project close while cache job runs.

Expected:

- Cache corruption never corrupts project/source.
- Missing cache regenerates.
- Size limits are enforced.
- App remains usable when cleanup fails.

## 6. Cancellation and process lifecycle

For each FFmpeg/FFprobe operation:

- Cancel before start.
- Cancel immediately after start.
- Cancel at approximately 50%.
- Cancel near completion.
- Start a new job after cancellation.
- Close app during job.

Verify:

- Child process exits.
- No orphan FFmpeg process remains.
- Partial output policy is enforced.
- UI thread remains responsive.
- Retry succeeds.

## 7. Timeline performance

Synthetic workloads:

- 1,000 items on 10 tracks.
- 5,000 items for stress observation.
- 20 tracks.
- Long timeline of 2 hours.
- Dense overlapping clips.
- Many markers.

Measure:

- Initial render time.
- Pan/zoom latency.
- Drag/trim latency.
- Hit-test latency.
- UI-thread stalls over 50 ms.
- Memory before/after 10 minutes of interaction.

Private-use target:

- 1,000-item timeline remains interactively usable.
- Normal drag/trim/pan does not visibly freeze the UI.
- Memory returns near baseline after closing a large project.

## 8. Media performance on CPU-only machine

Measure:

- Probe p50/p95.
- Thumbnail p50/p95.
- Waveform generation time.
- 540p and 720p proxy generation speed.
- Export speed for 30-second and 5-minute projects.
- Cancellation latency.
- CPU and memory peaks.

Acceptance:

- Background work does not block timeline input indefinitely.
- User can cancel heavy work.
- Rushframe gives visible progress or activity state.
- CPU-only export completes correctly even if slower than real time.

## 9. Soak tests

Run:

- Five-minute repeated play/seek loop.
- 100 project open/close cycles.
- 100 exports of a tiny fixture.
- 500 undo/redo operations.
- One-hour editor idle with autosave.
- One-hour mixed editing session.

Monitor:

- Process memory.
- Handle count.
- Child processes.
- File locks.
- Log growth.
- Cache growth.
- Unhandled exceptions.

## 10. Disk and filesystem faults

Test:

- Low disk during export.
- Low disk during autosave.
- Output file already open.
- Cache directory deleted during operation.
- External drive disconnected.
- Antivirus-style temporary file lock.

Expected:

- Actionable error.
- No project/source corruption.
- Retry possible after fault clears.

## 11. Privacy checks

Because Rushframe is private/local:

- No media, project data, filenames, or logs are uploaded without explicit future implementation.
- Logs avoid embedding full private media contents.
- Temporary files remain local and are cleaned according to policy.
- Crash/error reports are local unless owner explicitly exports them.
