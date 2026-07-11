# Media Engine Decision Spike

> **Status:** ✅ DONE — FFmpeg subprocess backend is implemented and documented in `migration_plan/results/media-engine-spike.md`.

## Purpose

Do not commit the product to MLT until a Windows x64 proof of concept passes. This phase decides the native media backend with measured evidence.

## Candidates

### Candidate A: MLT + FFmpeg

Use MLT for timeline composition and FFmpeg underneath for codecs and filters.

Potential benefits:

- Existing non-linear editing concepts.
- Multi-track composition.
- Filters, transitions, and animated properties.
- Reduced amount of custom composition code.

Risks:

- Windows packaging and native dependency complexity.
- C# bridge maintenance.
- Documentation/examples may not match the exact build.
- Threading and frame ownership must be verified.
- Some requested features may still need custom implementation.

### Candidate B: FFmpeg/libav native engine

Build a thin custom composition engine using FFmpeg libraries.

Benefits:

- Maximum codec/filter control.
- Fewer framework assumptions.
- Clear export path.

Risks:

- Large development effort for seeking, playback, synchronization, transitions, and editable graphs.

### Candidate C: FFmpeg subprocess engine first

Generate proxy previews and exports using isolated FFmpeg processes.

Benefits:

- Lowest integration risk.
- Crash isolation.
- Easy diagnostics.

Risks:

- Not sufficient alone for responsive frame-accurate manual editing.
- Preview latency.

## Mandatory spike application

Create `spikes/MediaEngineSpike/` outside production projects. It must:

1. Open one H.264/AAC MP4.
2. Open one image and one WAV/MP3 file.
3. Build two visual tracks and two audio tracks.
4. Trim and place clips at non-zero timeline positions.
5. Apply position, scale, rotation, opacity, and one blend/overlay operation.
6. Apply one transition.
7. Seek repeatedly to arbitrary frame positions.
8. Play 30 seconds with synchronized audio.
9. Render a 720p output.
10. Cancel a render without crashing.
11. Dispose and reopen the project ten times.
12. Run on a machine without a dedicated GPU.

## Measurements

Record:

- Cold startup time.
- Time to first preview frame.
- Seek latency: p50 and p95 over at least 50 seeks.
- Playback dropped frames at 540p and 720p proxy quality.
- Idle memory.
- Playback memory after five minutes.
- Export speed relative to media duration.
- Native crashes or leaks.
- Installer/runtime size.

## Pass criteria for MLT

MLT is accepted only if:

- It builds reproducibly for Windows x64.
- Runtime dependencies can be legally redistributed and packaged.
- The C# bridge can create, seek, play, stop, and dispose sessions repeatedly.
- No uncontrolled native crash occurs in the test loop.
- Audio/video synchronization remains acceptable.
- Required clip transforms and basic transitions work.
- A 540p proxy preview is usable on the target CPU-only machine.
- Error details can be surfaced to C#.

## Failure decision

If MLT fails any blocking criterion:

1. Do not patch around it inside WPF.
2. Select Candidate C for the first desktop milestone.
3. Keep `IMediaPreviewService` and `IRenderService` backend-neutral.
4. Implement proxy-based preview via FFmpeg workers.
5. Begin a separate custom native engine only after the desktop command/timeline model is stable.

## Bridge contract

The spike should test a small API shape, not expose MLT types:

```c
cc_engine_handle cc_engine_create(const cc_engine_config* config);
cc_status cc_engine_load_timeline(cc_engine_handle, const char* timeline_json);
cc_status cc_engine_seek(cc_engine_handle, int64_t time_num, int64_t time_den);
cc_status cc_engine_read_video_frame(cc_engine_handle, cc_video_frame* out_frame);
cc_status cc_engine_start_playback(cc_engine_handle);
cc_status cc_engine_stop_playback(cc_engine_handle);
cc_status cc_engine_render(cc_engine_handle, const cc_render_request* request);
void cc_engine_destroy(cc_engine_handle);
const char* cc_engine_last_error(cc_engine_handle);
```

All buffers need documented format, ownership, stride, lifetime, and release functions.

## Deliverable

Create `migration_plan/results/media-engine-spike.md` with:

- Exact dependency versions.
- Build steps.
- Machine specifications.
- Measurements.
- Failures.
- License notes.
- Final decision: MLT, FFmpeg-worker-first, or custom native engine.

No production media implementation begins before this decision is recorded.

---

**Status: ✅ DONE — see [results/media-engine-spike.md](results/media-engine-spike.md)**
