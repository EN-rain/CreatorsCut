# Agent Task Templates

> **Status:** ✅ READY — Templates are specification-only and available for use when Phase 1+ tasks are delegated.

Use these templates when delegating work to smaller agents. Replace bracketed fields. Do not remove acceptance criteria.

## Template A — Domain command

```text
Task: Implement [CommandName] in CreatorCut.Domain/Application.

Read first:
- migration_plan/01-agent-rules.md
- migration_plan/03-target-architecture.md
- migration_plan/04-domain-model-and-file-format.md
- Existing related command and tests

Scope:
- Implement only [specific behavior].
- Do not add UI, FFmpeg, database, or native code.
- Use stable IDs, not array indexes.
- Return typed validation errors.
- Make the operation undoable.

Required cases:
- [normal case]
- [boundary case]
- [invalid case]
- Undo restores exact IDs and values.
- Save/reload preserves the result if serialization changes.

Acceptance:
- Targeted tests pass.
- Full Domain/Application tests pass.
- No forbidden project dependency added.

Report:
- Files changed
- Tests run
- Assumptions
- Known limitation
```

## Template B — WPF interaction

```text
Task: Add [interaction] to [timeline/preview/panel].

Read first:
- migration_plan/01-agent-rules.md
- migration_plan/07-editor-interactions.md
- Existing application command invoked by this interaction

Scope:
- UI/input wiring only unless an explicitly missing command is part of this task.
- Invoke the existing command; do not duplicate edit logic in the view.
- Handle pointer capture, cancellation, window deactivation, and keyboard focus.
- Do not introduce OS-wide hooks.

Acceptance:
- Correct behavior for left/middle/right mouse as specified.
- No action when the pointer is outside the target surface.
- Escape cancels transient operation.
- Undo works after commit.
- Text-entry focus does not trigger timeline shortcuts.
- Interaction tests pass.

Do not:
- Add a button with no working command.
- Use array indexes as item identity.
- swallow exceptions silently.
```

## Template C — Native/media backend

```text
Task: Implement [media capability] behind [interface].

Read first:
- migration_plan/01-agent-rules.md
- migration_plan/03-target-architecture.md
- migration_plan/05-media-engine-decision-spike.md
- Recorded media-engine decision

Scope:
- Implement the abstraction without leaking MLT/FFmpeg/native types.
- x64 Windows only.
- CPU path mandatory.
- Cancellation mandatory for long-running work.
- Document buffer ownership and disposal.

Acceptance:
- Success and failure return typed results.
- Repeated create/use/dispose loop does not crash.
- Cancellation leaves no orphan worker/process/handle.
- Integration fixture test passes.
- Error details reach C# logs/UI.
- No per-frame unbounded managed allocations.
```

## Template D — Legacy importer

```text
Task: Import [legacy data area] into the desktop model.

Read first:
- migration_plan/01-agent-rules.md
- migration_plan/02-current-state-and-scope.md
- migration_plan/04-domain-model-and-file-format.md
- Actual legacy schema and tests

Rules:
- Source project is read-only.
- Import into a new destination.
- Preserve source path and migration warnings.
- Unknown values must not be silently invented.
- Missing files become offline assets.
- Import must be repeatable.

Acceptance:
- Sanitized fixture imports successfully.
- Missing/unknown fields produce explicit warnings.
- Legacy source files remain byte-identical.
- Imported project saves and reopens.
```

## Template E — Feature vertical slice

Use only after the underlying architecture phase permits it.

```text
Task: Implement [feature] as a complete vertical slice.

Required order:
1. Domain model/invariants.
2. Command and undo.
3. Persistence/schema migration.
4. Media backend preview/export.
5. UI and discoverability.
6. Context menu/shortcut if specified.
7. Tests and documentation.

Feature-specific acceptance:
- [list exact visible behavior]
- Preview and export agree.
- Reopen preserves settings.
- Undo/redo works.
- Invalid inputs produce actionable errors.
- CPU draft mode behavior documented.

Not complete if:
- Only UI exists.
- Only export exists without preview.
- Settings disappear after reload.
- Undo is missing.
```

## Template F — Bug fix

```text
Task: Fix [exact reproducible bug].

Reproduction:
1. [step]
2. [step]
3. Observed: [behavior]
4. Expected: [behavior]

Requirements:
- Identify root cause before editing.
- Add a failing regression test when possible.
- Fix the smallest responsible layer.
- Do not patch symptoms with arbitrary delays or global event suppression.
- Verify adjacent input/state behavior.

Acceptance:
- Regression test fails before and passes after.
- Existing targeted suite passes.
- No unrelated behavior changes.
```

## Task sizing guide

Good small-agent tasks:

- One command plus tests.
- One serializer/migration.
- One context menu using existing commands.
- One media service method and fixture.
- One panel and its view model.
- One cache policy.

Tasks that must be split:

- Build the full timeline.
- Implement all effects.
- Migrate the entire application.
- Add complete keyframing.
- Replace FFmpeg.
- Make it like CapCut.

## Dependency declaration

Every task must state:

```text
Depends on:
Blocks:
Files allowed to modify:
Files forbidden to modify:
Verification command:
```

This prevents two agents from implementing conflicting models or editing the same boundary simultaneously.
