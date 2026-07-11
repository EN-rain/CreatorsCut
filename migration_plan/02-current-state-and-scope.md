# Current State and Migration Scope

> **Status:** UP TO DATE - reflects the local Windows desktop codebase after legacy web/API removal.

## Current Implementation

Rushframe currently contains:

- A local .NET/WPF desktop application.
- C# domain, application, infrastructure, media, and desktop projects.
- FFmpeg-based probe, cache, preview support, and export services.
- A custom WPF multi-track editor surface.
- Optional local media-intelligence helpers under `rushframe_intelligence/`.
- Automated .NET tests.

Important locations:

| Area | Current location |
|---|---|
| Desktop editor | `src/Rushframe.Desktop/` |
| Domain model | `src/Rushframe.Domain/` |
| Media services | `src/Rushframe.Media.Native/` |
| Local media intelligence helper | `rushframe_intelligence/` |
| Tests | `tests/` |
| Main architecture document | `Agent_Video_Editor_Architecture_v10_task_campaign_hardened.md` |

## What Must Be Preserved

- Project creation and project metadata.
- Registered video, audio, and supporting assets.
- Timeline editing data.
- Preview rendering and final export.
- User ownership of raw media and explicit import.

## What Is Intentionally Replaced

- FastAPI/web services.
- React as the production editor UI.
- Python dictionaries as the primary timeline model.
- FFmpeg command construction spread across application logic.
- Full-preview re-render as the only way to inspect edits.

The old Python/FastAPI application has been removed from the normal repository surface.

## Product Scope

Rushframe is a manual Windows video editor for local, single-user work.

## Required Manual Editor Capabilities

### Timeline

- Multi-track video, overlay, text, sticker, music, voice, and SFX tracks.
- Select, move, trim, split, cut, copy, paste, duplicate, delete, and arrange items.
- Snapping, ripple options, markers, zoom, horizontal pan, and vertical track scrolling.
- Right-click menus and keyboard shortcuts.
- Undo and redo for every destructive edit.

### Clip Processing

- Transform, crop/mask, opacity, blend mode, speed, reverse, freeze frame, and stabilization.
- Text/caption authoring and styling.
- Color correction and simple effects.
- Transitions and fades.
- Audio gain, fades, mute/solo behavior, extraction, and mixed export.

### Media

- Import local video, audio, image, subtitle, font, and supporting asset files.
- Relink offline media.
- Generate thumbnails, proxies, and waveforms.
- Export final edits through local FFmpeg.

### Local Intelligence

- Run optional scene, transcript, music/audio, and Gemini-frame analysis from the desktop tab.
- Store imported scene/transcript analysis inside `.rushframe` project serialization.
- Convert detected scenes into clickable timeline markers.
- Convert transcript segments into editable `AI Captions` text clips mapped through source trim and playback speed.
- Reapplying analysis replaces prior generated markers/captions for the same media asset and remains undoable.
- Keep analysis local unless the owner explicitly enables an external API key.
