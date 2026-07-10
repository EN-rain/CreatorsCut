# Current State and Migration Scope

> **Status:** ✅ UP TO DATE — Accurately reflects the Python/FastAPI codebase. No changes needed.

## Current implementation

CreatorCut currently contains:

- A local Python application.
- FastAPI web routes and a React/TypeScript editor.
- Python project, task, campaign, media registry, timeline, and rendering managers.
- FFmpeg-based preview and export.
- A browser-rendered multi-panel editor.
- Existing project folders under `projects/`.
- Automated Python tests and frontend build checks.

Important existing locations:

| Area | Current location |
|---|---|
| React editor | `apps/editor-web/frontend/` |
| FastAPI editor server | `apps/editor-web/server.py` |
| Domain-like Python managers | `orchestrator/` |
| FFmpeg renderer | `orchestrator/engines/ffmpeg_engine.py` |
| Tests | `tests/` |
| Project data | `projects/` |
| Main architecture document | `Agent_Video_Editor_Architecture_v10_task_campaign_hardened.md` |

Agents must inspect the actual files before assuming their exact schema or behavior.

## What must be preserved

The migration must preserve these product concepts even if their implementation changes:

- Project creation and project metadata.
- Campaign descriptions and requirements.
- User-created tasks and agent task lifecycle.
- Registered video, audio, and supporting assets.
- Timeline versions and conflict protection.
- Preview rendering and final export.
- Auditability of agent actions.
- Manual editing after an agent produces a draft.
- User ownership of raw media and explicit import.

## What is intentionally replaced

The following are replaced for the desktop product:

- Browser-dependent panel and input behavior.
- FastAPI as the normal UI host.
- React as the production desktop editor UI.
- Python dictionaries as the primary timeline model.
- FFmpeg command construction spread across application logic.
- Full-preview re-render as the only way to inspect edits.

The old application remains available during migration for comparison and project import until decommission criteria are met.

## Product scope

CreatorCut is both:

1. A manual Windows video editor.
2. An agent-assisted editing platform where agents operate on explicit tasks and produce editable timelines.

The desktop editor must not become only an automation dashboard. Manual editing is a first-class requirement.

## Required manual editor capabilities

The final product scope includes:

### Timeline

- Multi-track video, overlay, text, sticker, music, voice, and SFX tracks.
- Select, move, trim, split, cut, copy, paste, duplicate, delete, and arrange items.
- Snapping, ripple options, markers, zoom, horizontal pan, and vertical track scrolling.
- Right-click menus and keyboard shortcuts.
- Undo and redo for every destructive edit.

### Clip processing

- Speed from 0.1x to 100x.
- Custom speed curves and named presets.
- Freeze frames and reverse playback.
- Source-audio volume and audio detachment.

### Composition

- Position, scale, rotation, anchor, opacity, and crop.
- Overlays and picture-in-picture.
- Masks, blend modes, layer ordering, and chroma key.
- At least ten simultaneous visual layers in the model; performance may degrade gracefully on CPU.

### Color and effects

- Basic corrections, HSL, curves, monochrome, noise reduction, filters, effects, and motion blur.
- Preview-quality fallback for expensive CPU effects.

### Text and stickers

- Editable text layers with styling, outline, shadow, templates, timing, and animation.
- Built-in and imported sticker assets.

### Transitions

- Explicit transitions placed between compatible clips.
- Duration, parameters, and preview/export parity.

### Keyframes

- Keyframes for animatable properties.
- Add, move, delete, copy, paste, smooth, hold, and custom easing.
- A reusable animation model, not feature-specific arrays.

### Audio

- Multiple music, voice, source-audio, and SFX clips.
- Trim, position, gain, fades, extraction, pitch/EQ presets, mute, solo, waveform display.

### Stabilization

- User-controlled strength.
- CPU background analysis with cached results.
- Optional crop compensation.

## Explicit non-goals for the first desktop release

Do not include these in the initial release unless a later plan explicitly adds them:

- macOS or Linux support.
- Cloud rendering.
- Collaborative simultaneous editing.
- Mobile editing.
- Mandatory GPU features.
- AI-generated voice or media.
- A public plugin marketplace.
- Professional broadcast interchange guarantees such as full AAF compatibility.

## Compatibility principle

The desktop application may support more capable timelines than the old web application. Legacy import is one-way initially:

```text
Legacy project → Desktop project
```

Do not promise desktop-to-legacy export unless separately designed and tested.
