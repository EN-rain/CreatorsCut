# CreatorCut Migration Phase Index

This folder contains the executable migration sequence. Smaller agents must work on only one phase or one task within a phase at a time.

## Required order

1. `00-architecture-and-media-spike.md`
2. `01-domain-model-and-edit-commands.md`
3. `02-desktop-shell-and-workspace.md`
4. `03-custom-timeline-ui.md`
5. `04-legacy-import-and-basic-media.md`
6. `05-core-manual-editor.md`
7. `06-composition-and-animation.md`
8. `07-advanced-timing-color-effects-audio.md`
9. `08-stabilization-performance-release.md`
10. `09-legacy-decommission.md`

## Rule

Do not skip a phase because later UI work appears easier. A later phase may begin only after the previous phase's exit criteria pass, unless the task is a clearly isolated research spike that cannot modify production code.

## Phase status

| Phase | Status | Main outcome |
|---|---|---|---|
| 0 | (done) | Architecture and media-backend decision |
| 1 | (done) | Domain model, 9 edit commands, undo/redo, JSON serialization |
| 2 | (done) | 6 docked panels, workspace layout, menu system, 11 tests |
| 3 | (done) | Timeline control: playhead, selection, edge trim, zoom/pan, fade display |
| 4 | (done) | LegacyImporter reads project.json/timelines/registries, 7 golden-fixture tests |
| 5 | (done) | All edit commands, ripple/snap, track ops, markers, context menus wired, autosave wired |
| 6 | (done) | Keyframes (6 easings), transitions, blend modes, masks, chroma key, JSON enum serialization |
| 7 | (done) | SpeedCurve, ColorCorrection, EffectRegistry (10 built-in), EffectStack commands |
| 8 | (done) | StabilizationSettings, CachePolicy/Service, AutosaveService, background autosave in MainWindow |
| 9 | (done) | MigrationService (backup/restore/import), ProjectRepository, migration dialog in OpenProject |

## Before accepting a task

Read:

1. `../01-agent-rules.md`
2. `../03-target-architecture.md`
3. `../04-domain-model-and-file-format.md`
4. The exact phase file assigned to you
5. `../09-testing-and-quality-gates.md`

## Definition of phase completion

A phase is complete only when:

- Every required deliverable exists.
- Required tests pass.
- No placeholder control is presented as functional.
- Persistence and undo behavior are defined for every destructive edit.
- Preview and export behavior are consistent where the phase includes media output.
- Documentation and status are updated.
