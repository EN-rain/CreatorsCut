# Dependency Versions and Licenses

## .NET solution dependencies (Phase 0)

| Dependency | Version | License | Link | Type |
|---|---|---|---|---|
| .NET SDK | 10.0.301 | MIT | https://dotnet.microsoft.com | Build tool |
| .NET Runtime | 10.0.x | MIT | https://github.com/dotnet/runtime | Runtime |
| xUnit | 2.9.x | Apache-2.0 | https://xunit.net | Test framework |
| NetArchTest.Rules | 1.3.2 | Apache-2.0 | https://github.com/BenMorris/NetArchTest | Test (architecture) |
| WPF | 10.0.x | MIT | https://github.com/dotnet/wpf | UI framework |

## External runtime dependencies

| Dependency | Version | License | Redistribution notes |
|---|---|---|---|
| FFmpeg | 7.0+ | LGPL/GPL | Bundled as `.tools/bin/ffmpeg.exe`. Must ship `LICENSE` and provide source offer if GPL build. |

## Python / intelligence pipeline dependencies (legacy, preserved)

| Dependency | Version | License | Notes |
|---|---|---|---|
| FastAPI | 0.136.1 | MIT | Web framework |
| Uvicorn | 0.46.0 | BSD-3 | ASGI server |
| Pydantic | 2.12.5 | MIT | Data validation |
| scenedetect | 0.7 | BSD-3 | Scene detection (optional) |
| faster-whisper | 1.2.1 | MIT | Speech transcription (optional) |
| librosa | 0.11.0 | ISC | Music analysis (optional) |

## Third-party notice obligations

- **FFmpeg**: If linked as GPL, must provide complete corresponding source code or a written offer. LGPL build requires attribution only. The exact shipped build determines obligation.
- **NetArchTest.Rules**: Apache 2.0 — retain copyright notice.
- **xUnit**: Apache 2.0 — retain copyright notice.

## Record keeping

All dependency versions are tracked in:
- `pyproject.toml` (Python packages, exact pins)
- `tests/CreatorCut.Domain.Tests/CreatorCut.Domain.Tests.csproj` (test NuGet)
- This document (comprehensive record)

Before shipping v1, generate a `THIRD_PARTY_NOTICES.txt` file containing the full text of all required licenses.
