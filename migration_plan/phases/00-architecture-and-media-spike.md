# Phase 0 — Architecture and Media Spike

**Current status: (DONE)**

## Goal

Prove the Windows desktop architecture before building the editor. No production feature work belongs here.

## Decisions to validate

- C#/.NET and WPF for the application shell.
- Pure C# domain and application layers.
- MLT through a thin native bridge only if the Windows spike passes.
- FFmpeg fallback if MLT packaging, playback, licensing, or stability is unacceptable.
- SQLite for indexes/settings and versioned JSON for portable project data.

## Required deliverables

- `Rushframe.sln` with Domain, Application, Infrastructure, Desktop, MediaBridge, and test projects.
- Dependency-direction tests.
- Minimal desktop window that starts and exits cleanly.
- Media spike report containing exact versions, commands, CPU use, memory use, deployment size, and failures.
- Licensing inventory.
- Written backend decision: `MLT_ACCEPTED` or `FFMPEG_CUSTOM_BACKEND`.

## Spike scenarios

1. Open H.264 MP4 with audio.
2. Play, pause, seek, and loop.
3. Compose two clips sequentially.
4. Add one overlay and one audio track.
5. Apply one transform and one dissolve.
6. Preview at 540p on CPU.
7. Export a deterministic MP4.
8. Package on a clean Windows machine.

## Prohibited

- No final timeline UI.
- No fake effect library.
- No dependency chosen only from documentation claims.
- No backend decision without measurements.

## Exit gate

- Solution builds.
- Architecture tests pass.
- Desktop shell opens.
- One media backend is accepted with documented evidence.
- Installer/native dependency feasibility is proven.
