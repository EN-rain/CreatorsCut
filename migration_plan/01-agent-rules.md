# Agent Execution Rules

> **Status:** ✅ READY — Rules are specification-only and enforced by architecture tests. No changes needed.

This document is mandatory for every agent working on the migration.

## 1. Never guess

Before changing code, inspect:

- The task document for the current phase.
- The existing implementation being replaced.
- The relevant interface and tests.
- Any migration compatibility requirements.

If a requirement is not specified, do not invent behavior. Add a clearly marked question or TODO to the phase document and stop that subtask.

## 2. Keep boundaries clean

Allowed dependency direction:

```text
Rushframe.Desktop (WPF)
        ↓
Rushframe.Application
        ↓
Rushframe.Domain
        ↑
Rushframe.Infrastructure
Rushframe.Media
Rushframe.LegacyImport
```

Rules:

- `Rushframe.Domain` depends on no UI, database, FFmpeg, MLT, or Python package.
- WPF views never invoke FFmpeg directly.
- View models never edit SQLite directly.
- Native pointers never escape the media adapter layer.
- Timeline documents never store absolute temporary-cache paths.
- A render engine never mutate the domain timeline without returning an explicit result.

## 3. One task, one responsibility

A small agent should receive one narrowly defined task, for example:

- Implement `SplitClipCommand` in the domain layer.
- Add a WPF context menu that invokes existing commands.
- Add legacy JSON import tests.

Do not assign “build the timeline” or “add all effects” as one task.

## 4. Required task workflow

For every task:

1. Read the task scope and acceptance criteria.
2. Identify the exact projects and files involved.
3. Write or update tests first when practical.
4. Implement the smallest complete change.
5. Run targeted tests.
6. Run the phase-level verification command.
7. Report changed files, tests, assumptions, and remaining risks.

## 5. No fake implementation

The following are forbidden unless a task explicitly requests a placeholder:

- Buttons that do nothing.
- Disabled controls presented as complete.
- Hardcoded sample timelines in production code.
- Fake progress values.
- Silent catches that hide media failures.
- Returning success when FFmpeg/MLT failed.
- UI-only features with no persistent model or render behavior.

A feature is not implemented until its UI, command, model, persistence, preview behavior, export behavior, and tests agree.

## 6. Feature-completeness matrix

For each editor feature, agents must verify these columns:

| Concern | Required question |
|---|---|
| Domain | Is the feature represented in the timeline model? |
| Command | Can it be applied and undone? |
| Persistence | Does save/reload preserve it? |
| UI | Can the user discover and edit it? |
| Preview | Does draft playback show it? |
| Export | Does final render include it? |
| Tests | Are success, failure, and undo cases covered? |

## 7. CPU-first constraints

Assume the user has no dedicated GPU.

- Never require CUDA, NVENC, Metal, or Vulkan.
- Hardware acceleration may be optional, never mandatory.
- Preview defaults to proxy or reduced resolution.
- Do not decode full-resolution media merely to draw thumbnails.
- Avoid per-frame managed allocations.
- Use bounded caches and cancellation tokens.
- Heavy effects must expose draft-quality behavior or be bypassable during playback.

## 8. Native interop rules

- Prefer a small stable C ABI over exposing third-party native types.
- Every native resource needs deterministic disposal.
- Use `SafeHandle` in C# for owned native handles.
- Document ownership for every pointer and buffer.
- Native exceptions must not cross ABI boundaries.
- Return typed error codes plus retrievable error text.
- Verify x64 first. Do not add x86 support unless required.

## 9. Migration safety

- Never overwrite a legacy project in place.
- Import into a new desktop project directory.
- Preserve the original project path in migration metadata.
- Generate a migration report with warnings.
- Unknown fields must be preserved in an extension bag when possible.
- Migration must be repeatable and idempotent.

## 10. Source-control discipline

- Keep changes focused.
- Do not mix architecture refactors with feature work.
- Do not mass-format unrelated files.
- Do not add a dependency without documenting why and its license.
- Do not delete the legacy implementation until its replacement phase has passed.

## 11. Required completion report

Every agent response must include:

```text
Task:
Files changed:
Behavior implemented:
Tests run:
Result:
Assumptions:
Known limitations:
Next dependency:
```

## 12. Stop conditions

Stop and report instead of guessing when:

- MLT behavior differs from documentation.
- A codec/license requirement is unclear.
- A timeline operation would corrupt source references.
- The requested UI has no matching domain command.
- A native crash cannot be isolated.
- A schema change lacks a migration path.
