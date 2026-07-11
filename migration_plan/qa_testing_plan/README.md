# Rushframe QA Testing Plan

This folder is the authoritative QA plan for the private Windows-only Rushframe desktop editor.

## Purpose

Verify that Rushframe is safe and usable for the owner’s real editing workflow. Private use reduces distribution and multi-user requirements, but it does not reduce correctness requirements for projects, source files, timeline edits, preview, audio, and exports.

## Status labels

- **(AUTOMATED)** — covered by a repeatable automated test.
- **(MANUAL)** — must be checked by operating the desktop editor.
- **(PARTIAL)** — some paths are tested, but important cases remain.
- **(NOT COVERED)** — no adequate test exists yet.
- **(BLOCKED)** — feature implementation is incomplete, so full QA cannot pass.

A feature is not considered QA-complete because a model, button, or command exists.

## Documents

1. `01-qa-strategy-and-gates.md` — scope, test levels, environments, severity, release gates.
2. `02-editor-functional-test-matrix.md` — exhaustive editor and timeline test cases.
3. `03-media-preview-export-tests.md` — import, probe, proxy, playback, effects, audio, and render tests.
4. `04-project-recovery-performance-tests.md` — persistence, autosave, corruption, cache, performance, soak, and cancellation.
5. `05-automation-plan.md` — what to automate, fixture strategy, test project layout, and command lines.
6. `06-manual-qa-runbook.md` — step-by-step private-use smoke, regression, and release checklists.
7. `07-bug-report-template.md` — required format for defects found by agents or the owner.
8. `08-current-coverage-audit.md` — current code/test coverage and known gaps.
9. `qa-run-2026-07-10.md` — latest executed QA evidence for this pass.

## Highest-risk areas

1. Source media must never be overwritten.
2. Save/autosave/undo must not corrupt or lose edits.
3. Timeline operations must affect the selected clip and correct track only.
4. Preview and export must not materially disagree without a documented limitation.
5. Exported duration, video, and audio must be correct.
6. Long FFmpeg jobs must be cancellable without freezing Rushframe.
7. Missing, malformed, unsupported, or offline media must produce actionable errors instead of crashes.

## Minimum private-use release gate

Before using a build for real work:

- Release build succeeds.
- All automated tests pass.
- Private-use smoke suite passes.
- Save, reopen, autosave recovery, undo/redo, and export pass with a real sample project.
- Export contains correct video and audio.
- No Blocker or High bugs remain in the workflows the owner uses.

## Standard verification commands

```powershell
dotnet build Rushframe.slnx -c Release
dotnet test Rushframe.slnx -c Release
```

Run media integration and manual suites whenever media processing, timeline behavior, project persistence, or WPF input handling changes.
