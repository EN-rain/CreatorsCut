# Phase 2 — Desktop Shell and Workspace

**Current status: (DONE FOR PRIVATE USE)**

## Goal

Build the native Windows application shell before adding advanced editing behavior.

## Required UI

- Main WPF window.
- Docked Media, Preview, Inspector, Timeline, Tasks/Campaign, and Render Queue panels.
- Close, restore, resize, move, float, and reset panels.
- `View > Panels` always provides a restore path.
- Default layout recovery when settings are corrupt.
- Native menus, context menus, command routing, and shortcut service.
- Theme, DPI scaling, file dialogs, drag-and-drop, recent projects, autosaved settings.

## Input ownership

- Middle mouse over timeline pans only timeline.
- Middle mouse over zoomed preview pans only preview.
- Right-click opens Rushframe menus and never browser menus.
- Mouse capture is released on pointer-up, cancellation, deactivation, and lost capture.
- Keyboard shortcuts do not fire while typing unless explicitly allowed.

## Deliverables

- Workspace layout service separate from project data.
- Panel registry with stable panel IDs.
- Command definitions for open/save/import/render/cut/copy/paste/split/delete/undo/redo.
- Error boundary and crash-safe settings writer.

## Tests

- Close and restore every panel.
- Start with damaged layout settings.
- Multi-monitor and DPI layout restore.
- Focus transitions between timeline, preview, inspector, and text fields.
- Mouse capture cleanup.

## Exit gate

- App cannot enter a state where all core panels are unreachable.
- Workspace tests pass.
- No editor feature directly writes window-layout data into the project file.
