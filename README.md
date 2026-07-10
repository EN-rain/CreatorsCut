# CreatorCut

.NET 10 WPF desktop video editor — migrated from Python/React orchestrator.

## Build

```
dotnet build CreatorCut.slnx
dotnet test tests/CreatorCut.Domain.Tests
dotnet test tests/CreatorCut.Desktop.Tests
dotnet test tests/CreatorCut.LegacyImport.Tests
```

103 tests, 0 warnings, 0 errors.

## Solution (10 projects)

| Project | Role |
|---|---|
| `CreatorCut.Domain` | Pure domain model, 9 edit commands, undo/redo, JSON serialization |
| `CreatorCut.Application` | Application-layer commands (copy/paste, set property, migration service) |
| `CreatorCut.Infrastructure` | Autosave, cache, effect registry, project repository |
| `CreatorCut.Desktop` | WPF shell — 6 docked panels, timeline control, menus, workspace layout |
| `CreatorCut.LegacyImport` | Reads legacy `project.json` / timelines / media registries |
| `CreatorCut.Media.Abstractions` | Media service interfaces |
| `CreatorCut.Media.Native` | FFmpeg subprocess service |
| `CreatorCut.Domain.Tests` | 85 unit tests — domain, commands, serialization, infra |
| `CreatorCut.Desktop.Tests` | 11 unit tests — panels, workspace layout |
| `CreatorCut.LegacyImport.Tests` | 7 golden-fixture tests |

## Architecture

Domain → nothing; Application → Domain + LegacyImport + Media.Abstractions; Infrastructure → Domain; Desktop → Application + Infrastructure.

## Phase status

| Phase | Status | Description |
|---|---|---|
| 0–4 | done | Architecture spike, domain model, WPF shell, timeline control, legacy import |
| 5 | done | All editing commands, ripple/snap, track ops, markers, context menus, autosave |
| 6 | done | Keyframes (6 easing types), transitions, blend modes, masks, chroma key |
| 7 | done | Speed curves, color correction, effect registry (10 built-in categories) |
| 8 | done | Stabilization settings, cache policy/service, background autosave |
| 9 | done | Migration service (backup/restore), project repository, migration dialog |

## Media engine

FFmpeg subprocess (spike candidate C). Probes media, seeks frames, renders/transforms/overlays via CLI.
