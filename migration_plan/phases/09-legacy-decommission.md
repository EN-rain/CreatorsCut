# Phase 9 — Legacy Decommission

## Goal

Retire the Python/React editor only after the desktop editor safely replaces supported workflows.

## Preconditions

All must be true:

- Desktop import supports the documented legacy project versions.
- Core CreatorCut workflows pass acceptance tests.
- Preview/export parity is acceptable for supported features.
- Stable Windows installer and upgrade path exist.
- Backups and rollback instructions are documented.
- User explicitly approves retirement.

## Required work

- Freeze the legacy editor except for critical data-loss/security fixes.
- Publish a supported/unsupported migration matrix.
- Run migration against copies of real projects.
- Preserve the legacy source and project backups for a defined retention period.
- Add a desktop prompt that detects legacy projects and offers copy-based migration.
- Provide export of migration logs and errors.
- Remove startup dependencies on Python/web services from the desktop release.
- Archive, rather than immediately delete, legacy implementation code.
- Update all documentation and launch commands.

## Rollback plan

- Keep the last known-good legacy release available locally.
- Never downgrade or overwrite desktop project files in place.
- Store project-format version and application version.
- Before an irreversible migration, create a verified backup.
- Document how to reopen the original legacy project.

## Validation

- Fresh installation with no Python runtime.
- Open a new project, migrated project, and large project.
- Save/reopen/export each.
- Uninstall/reinstall without losing user projects.
- Failed migration leaves the original intact.
- Compare representative outputs against approved references.

## Prohibited

- Do not delete the legacy editor because the new UI merely opens.
- Do not claim parity from button presence.
- Do not migrate the only copy of a user project.
- Do not remove fallback access before user approval.

## Exit gate

- Migration success criteria and known limitations are published.
- Rollback rehearsal succeeds.
- Legacy normal workflows are covered or explicitly declared unsupported.
- User approves decommission.
- Legacy code is archived and excluded from normal release packaging.
