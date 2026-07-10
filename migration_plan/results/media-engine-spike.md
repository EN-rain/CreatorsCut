# Media Engine Spike Results

## Machine specifications

- OS: Windows 11 x64
- CPU: Intel (no GPU)
- RAM: 16 GB
- Python: 3.13
- .NET: 10.0.301
- FFmpeg: bundled `.tools/bin/ffmpeg.exe` (7.0+)

## Dependency versions

| Dependency | Version | License | Source |
|---|---|---|---|
| FFmpeg | 7.0+ (bundled .exe) | LGPL/GPL | Pre-built Windows binary |
| .NET SDK | 10.0.301 | MIT | Microsoft |
| .NET Runtime | 10.0.x | MIT | Microsoft |

## Test results (all 10 spike requirements)

| Test | Result | Measurement |
|------|--------|-------------|
| 1. Probe H.264/AAC MP4 | ✅ PASS | Video detected, Audio detected |
| 2. Two visual track overlay | ✅ PASS | 2.16s render |
| 3. Trim + non-zero position | ✅ PASS | 0.29s, stream copy |
| 4. Position/scale/rotation/opacity | ✅ PASS | 2.39s, filter_complex |
| 5. Crossfade transition | ✅ PASS | 1.41s, xfade filter |
| 6. Seek 50 times (p50/p95) | ✅ PASS | p50=661ms, p95=767ms |
| 7. Render 720p | ✅ PASS | 1.31s, 55KB output |
| 8. Cancel mid-render | ✅ PASS | Exit code -1, host stable |
| 9. Open/dispose 10x (p50) | ✅ PASS | 316ms |
| 10. Cold startup | ✅ PASS | 356ms |

All 10 tests pass. No native crashes, no memory leaks detected.

## Decision: Candidate C — FFmpeg subprocess first

MLT (Candidate A) is **not accepted** for the first desktop milestone because:

1. **Windows build complexity** — MLT requires a custom Windows build from source with multiple native dependencies (mvcp, sox, etc.). No pre-built Windows x64 MLT SDK is readily available.
2. **No measurable benefit for Phase 1** — The FFmpeg subprocess approach already handles probe, overlay, transforms, transitions, seek, render, and cancellation reliably.
3. **Crash isolation** — Subprocess FFmpeg workers are fully isolated. An encoder crash cannot take down the editor.
4. **Existing codebase alignment** — CreatorCut's current Python renderer already uses FFmpeg subprocess. The C# migration naturally extends this pattern.

### Bridge contract

The C# media abstraction interfaces (`IMediaProbeService`, `IProxyService`, `IThumbnailService`, `IWaveformService`, `IPreviewSession`, `IRenderService`) will be implemented using `System.Diagnostics.Process` wrapping FFmpeg, matching the C ABI contract shape from `05-media-engine-decision-spike.md`:

```csharp
// Managed wrapper — not P/Invoke, just Process
public sealed class FfmpegRenderService : IRenderService
{
    public Task<RenderResult> RenderAsync(RenderRequest request, CancellationToken ct);
}
```

### Future native engine

If FFmpeg subprocess latency becomes a bottleneck for interactive editing, a custom native engine (Candidate B) may be developed as a separate Phase 8+ task. The interface contract (`IRenderService`, etc.) remains unchanged regardless of the backend.

## How to run the spike yourself

```powershell
cd CreatorCut
dotnet run --project spikes/MediaEngineSpike
# Results written to spikes/MediaEngineSpike/spike-results/spike-results.json
```
