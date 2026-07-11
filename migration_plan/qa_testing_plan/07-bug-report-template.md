# Rushframe Bug Report Template

Use one report per defect.

## Required fields

```text
Title:
QA ID(s):
Severity: Blocker / High / Medium / Low
Build or commit:
Windows version:
FFmpeg version:
CPU/RAM:
Feature area:

Preconditions:
1.

Steps to reproduce:
1.
2.
3.

Expected result:

Actual result:

Reproduction rate: Always / Often / Sometimes / Once

Project/media fixture:
Use sanitized or generated fixture names. Do not attach private media unless explicitly intended.

Logs/error text:

Screenshots/video:

Source media modified: Yes / No / Unknown
Project data lost/corrupt: Yes / No / Unknown
Workaround:

Suspected files/components:

Regression: Yes / No / Unknown
Last known good build:
```

## Good title format

```text
[Timeline][High] Left trim changes the wrong clip after horizontal pan
[Export][Blocker] Render reports success but output contains no audio
[Workspace][High] Inspector cannot be restored after layout corruption
```

## Rules for agents

- Reproduce before editing code when possible.
- Do not label a symptom as the root cause without evidence.
- Include exact expected state changes.
- State whether undo, save/reopen, preview, and export are affected.
- Add or update a regression test for every fixed Blocker or High defect.
- Do not close the bug because a button responds; verify the resulting project and export.
