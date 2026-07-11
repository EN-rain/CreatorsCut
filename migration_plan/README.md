# Rushframe Windows Desktop Plan

This folder tracks Rushframe as a private, Windows-only desktop video editor.

## Overall Progress

| Phase | Status |
|---|---|
| Phase 0 - Architecture and media spike | DONE |
| Phase 1 - Pure domain model | DONE |
| Phase 2 - Desktop shell | DONE FOR PRIVATE USE |
| Phase 3 - Custom timeline | DONE FOR PRIVATE USE |
| Phase 4 - Legacy import and basic media | DONE FOR PRIVATE USE |
| Phase 5 - Core manual editor | DONE FOR PRIVATE USE |
| Phase 6 - Composition and animation | DONE FOR PRIVATE USE |
| Phase 7 - Advanced timing, color, effects, and audio | DONE FOR PRIVATE USE |
| Phase 8 - Stabilization and performance | DONE FOR PRIVATE USE |
| Phase 9 - Legacy decommission | DONE FOR PRIVATE USE |

## Target Outcome

Rushframe is a local Windows desktop application with:

- C#/.NET application and editor logic.
- WPF desktop UI.
- A custom multi-track timeline.
- Reliable mouse, keyboard, right-click, drag, trim, and panel behavior.
- CPU-friendly proxy playback and rendering.
- FFmpeg subprocess services behind C# media interfaces.
- Optional local media-intelligence helpers under `rushframe_intelligence/`.
- No browser dependency, no FastAPI runtime, and no `python run.py` launcher.

## Final Target Stack

| Layer | Technology | Responsibility |
|---|---|---|
| Desktop shell and UI | C# 13+, .NET 10, WPF | Windows, panels, menus, timeline interaction, inspector, commands |
| Application core | C# | Projects, timeline model, selection, undo/redo, autosave, jobs |
| Persistence | Versioned JSON | Portable timeline/project documents |
| Media abstraction | C# interfaces | Stable boundary between the editor and media implementation |
| Media implementation | C# process wrapper | FFmpeg and FFprobe subprocesses |
| Codec/filter/export foundation | FFmpeg | Probe, decode, encode, filters, proxies, derivatives, and export |
| Tests | xUnit + integration fixtures | Unit, command, persistence, media, render, migration tests |

## Non-Negotiable Architectural Rule

Do not let WPF controls, FFmpeg commands, or helper script dictionaries become the project model.

The C# domain model is the source of truth. External systems adapt to it through interfaces.

## Read These Documents In Order

1. [01-agent-rules.md](01-agent-rules.md)
2. [02-current-state-and-scope.md](02-current-state-and-scope.md)
3. [03-target-architecture.md](03-target-architecture.md)
4. [04-domain-model-and-file-format.md](04-domain-model-and-file-format.md)
5. [05-media-engine-decision-spike.md](05-media-engine-decision-spike.md)
6. [06-migration-phases.md](06-migration-phases.md)
7. [phases/README.md](phases/README.md)
8. [07-editor-interactions.md](07-editor-interactions.md)
9. [08-feature-roadmap.md](08-feature-roadmap.md)
10. [09-testing-and-quality-gates.md](09-testing-and-quality-gates.md)
11. [11-agent-task-templates.md](11-agent-task-templates.md)
12. [qa_testing_plan/README.md](qa_testing_plan/README.md)

## Current Scope

Complete local editor workflows. Public packaging, signing, installers, hosted web surfaces, and external release tasks are out of scope.

## Definition Of Success

- The owner launches the WPF app locally.
- Manual timeline editing works without browser input conflicts.
- Preview playback uses proxies or draft quality and stays usable without a dedicated GPU.
- Final export uses original media.
- Closing and restoring panels is obvious and deterministic.
- Right-click menus, keyboard commands, undo/redo, and autosave are covered by tests where practical.
- The Python web editor and FastAPI runtime are removed from normal use.
