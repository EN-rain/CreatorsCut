# Phase 9 - Legacy Decommission

**Current status: DONE FOR PRIVATE USE**

## Goal

Remove the old Python/FastAPI/React editor and agent-publish surface from the normal repository.

## Completed Removal

- Removed `apps/editor-web/`.
- Removed `packages/agent-plugin/`.
- Removed `orchestrator/`.
- Removed old launcher scripts and `run.py`.
- Removed Python package metadata and web/API dependencies.
- Removed sample agent project data.
- Removed publish/package scripts and generated publish artifacts.
- Moved the useful local media-intelligence helper into `rushframe_intelligence/`.

## Remaining Local Compatibility

- The .NET `Rushframe.LegacyImport` project remains because it imports old project files into the desktop model.
- Optional media intelligence remains local and is launched by the desktop tab.
- Public packaging, signing, deployment, and automatic updates are out of scope.
