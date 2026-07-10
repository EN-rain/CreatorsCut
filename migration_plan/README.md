# CreatorCut Windows Desktop Migration Plan

This folder is the authoritative execution plan for migrating CreatorCut from the current local web editor into a Windows-only desktop video editor.

## Overall progress

| Phase | Status |
|---|---|
| **Phase 0** — Architecture and media spike | 🟡 **Partial** (solution skeleton ✅, arch tests ✅, build ✅, desktop opens ✅ — media spike ❌, docking ❌, installer ❌, licenses ❌) |
| **Phase 1** — Pure domain model | ⏳ Not started |
| **Phase 2** — Desktop shell | ⏳ Not started |
| **Phase 3** — Custom timeline | ⏳ Not started |
| **Phase 4** — Legacy import | ⏳ Not started |
| **Phase 5** — Core manual editor | ⏳ Not started |
| **Phase 6** — Composition | ⏳ Not started |
| **Phase 7** — Advanced features | ⏳ Not started |
| **Phase 8** — Stabilization | ⏳ Not started |
| **Phase 9** — Legacy decommission | ⏳ Not started |

## Target outcome

CreatorCut becomes a Windows desktop application with:

- C#/.NET application and editor logic.
- WPF desktop UI.
- A custom multi-track timeline.
- Reliable mouse, keyboard, right-click, drag, trim, and panel behavior.
- CPU-friendly proxy playback and rendering.
- A native media boundary for FFmpeg and, only if proven viable, MLT.
- Existing agent/task/campaign workflows preserved behind clean interfaces.
- No browser dependency and no requirement to run `python run.py` manually.

## Final target stack

| Layer | Technology | Responsibility |
|---|---|---|
| Desktop shell and UI | C# 13+, .NET 10, WPF | Windows, docking, menus, timeline interaction, inspector, commands |
| Application core | C# | Projects, timeline model, selection, undo/redo, autosave, jobs |
| Persistence | SQLite + versioned JSON | Project index, cache metadata, portable timeline documents |
| Media bridge | C ABI or C++/CLI bridge | Stable boundary between managed C# and native media code |
| Composition engine | MLT only if the Phase 0 spike passes | Multi-track playback, filters, transitions, property animation |
| Codec/filter/export foundation | FFmpeg | Decode, encode, mux, audio processing, export, fallback composition |
| Audio playback | WASAPI through NAudio or a small native component | Low-latency playback, scrubbing, metering |
| Tests | xUnit + integration fixtures | Unit, command, persistence, media, render, migration tests |
| Packaging | WiX Toolset or MSIX | Windows installer, runtime files, upgrades |

## Non-negotiable architectural rule

Do not let WPF controls, FFmpeg commands, MLT objects, SQLite rows, or legacy Python dictionaries become the project model.

The C# domain model is the source of truth. All external systems adapt to it through interfaces.

## Read these documents in order

1. [01-agent-rules.md](01-agent-rules.md)
2. [02-current-state-and-scope.md](02-current-state-and-scope.md)
3. [03-target-architecture.md](03-target-architecture.md)
4. [04-domain-model-and-file-format.md](04-domain-model-and-file-format.md)
5. [05-media-engine-decision-spike.md](05-media-engine-decision-spike.md)
6. [06-migration-phases.md](06-migration-phases.md) — condensed phase summary
7. [phases/README.md](phases/README.md) — detailed per-phase execution files
8. [07-editor-interactions.md](07-editor-interactions.md)
9. [08-feature-roadmap.md](08-feature-roadmap.md)
10. [09-testing-and-quality-gates.md](09-testing-and-quality-gates.md)
11. [10-packaging-and-release.md](10-packaging-and-release.md)
12. [11-agent-task-templates.md](11-agent-task-templates.md)

## Execution rule

An agent may implement only tasks from the current phase. Do not jump forward because a later feature looks easy. Every phase has explicit entry and exit criteria.

**Current phase: Phase 0** (partial — 4 of 8 exit sub-items done)

## Definition of success

The migration is complete only when:

- A user launches `CreatorCut.exe` directly.
- Existing CreatorCut projects can be imported without destroying their source data.
- Manual timeline editing works without browser input conflicts.
- Preview playback uses proxies or draft quality and stays usable without a dedicated GPU.
- Final export uses original media.
- Closing and restoring panels is obvious and deterministic.
- Right-click menus, keyboard commands, undo/redo, and autosave are covered by automated tests.
- The Python web editor is no longer required for normal use.
