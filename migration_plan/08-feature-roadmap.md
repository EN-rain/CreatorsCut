# Feature Roadmap and Acceptance Criteria

> **Status:** ✅ DONE FOR PRIVATE USE — Core private-use editing features are implemented in Rushframe; public-product hardening remains out of scope unless requested.

This roadmap prevents agents from treating a visible control as a finished feature.

## Priority levels

- **P0:** Required for usable manual editing.
- **P1:** Required for the intended complete editor.
- **P2:** Advanced feature after the core editor is stable.

## Timeline and clip editing

| Feature | Priority | Completion criteria |
|---|---:|---|
| Select/multi-select | P0 | Mouse and keyboard selection, visible state, command targeting, tests |
| Move clips | P0 | Drag across valid tracks, snapping, undo, persistence, preview/export |
| Edge trim | P0 | Left/right trim, source limits, tooltip, undo |
| Split | P0 | Toolbar/menu/context/shortcut invoke one command; source ranges preserved |
| Cut/copy/paste | P0 | Clipboard format, stable IDs, undo, cross-sequence rules documented |
| Duplicate/delete | P0 | Context menu and shortcuts, undo |
| Ripple delete | P0 | Gap closure on configured tracks, deterministic behavior |
| Merge | P1 | Only compatible contiguous clips; define whether destructive or compound |
| Speed 0.1x–100x | P1 | Duration/source mapping, audio policy, preview/export parity |
| Speed curves | P1 | Editable curve points; presets generate curves but do not lock them |
| Freeze frame | P1 | Creates explicit still segment referencing a frame/time |
| Reverse | P1 | Preview/export, cache strategy, audio policy |

## Transform and compositing

| Feature | Priority | Completion criteria |
|---|---:|---|
| Position/scale/rotation | P0 | Direct preview handles and inspector fields; keyframe-ready model |
| Anchor/opacity/crop | P0 | Persisted and rendered |
| Overlay/PIP | P0 | Add image/video to higher track, transform freely |
| Layer ordering | P0 | Track/item ordering reflected in preview and export |
| Blend modes | P1 | Supported-mode registry, CPU fallback, unresolved mode handling |
| Masks | P1 | Shape and polygon masks, feather/invert, keyframe support |
| Chroma key | P1 | Manual color, tolerance, softness/spill controls |

## Color correction

| Feature | Priority | Completion criteria |
|---|---:|---|
| Basic adjustments | P1 | Brightness, contrast, saturation, exposure, highlights, shadows, whites, tint |
| Black and white | P1 | Adjustable or explicit effect, preview/export parity |
| Vintage/filter intensity | P1 | Non-destructive effect instance with adjustable strength |
| HSL | P2 | Per-range hue/saturation/luminance model and UI |
| Curves | P2 | Editable points, channel selection, serialized curve |
| Color match | P2 | Explicit source/reference behavior; no fake one-click result |
| Noise reduction | P2 | Strength control, draft bypass, cancellable render behavior |

## Text and titles

| Feature | Priority | Completion criteria |
|---|---:|---|
| Create/edit text | P0 | Timeline item, canvas editing, inspector, export |
| Font/color/size | P0 | Font resolution and missing-font fallback |
| Outline/shadow | P1 | Adjustable parameters |
| Templates | P1 | Template creates editable properties |
| Animation | P1 | In/out/loop definitions and timing |
| Timeline duration drag | P0 | Edge trim behavior for text items |

## Stickers and overlays

| Feature | Priority | Completion criteria |
|---|---:|---|
| Import PNG/image | P0 | Transparent images retain alpha |
| Basic built-in shapes | P1 | Local bundled assets with license record |
| Sticker library | P1 | Search/categories; no online dependency required |
| Transform/duration/order | P0 | Same reusable transform and timeline systems |

## Transitions

| Feature | Priority | Completion criteria |
|---|---:|---|
| Cross dissolve | P0 | Explicit transition object, editable duration |
| Slide/zoom/blur/split | P1 | Parameterized and categorized |
| Whip pan/mask/MG | P2 | CPU draft mode and fallback behavior |
| Manual placement | P0 | User adds/removes transition; never auto-assigned silently |

## Effects and filters

Use a registry rather than hardcoding one inspector per effect.

Each effect definition provides:

- Stable type ID.
- Display name/category.
- Parameter schema and ranges.
- Default values.
- Preview cost.
- Backend capability requirements.
- Export implementation.

P1 categories: basic, retro, lens, distortion, lighting. P2 includes heavier body/motion effects.

## Keyframes

| Feature | Priority | Completion criteria |
|---|---:|---|
| Add/delete/move | P1 | Diamond markers and inspector controls |
| Copy/paste | P1 | Type compatibility validation |
| Hold/linear/Bezier | P1 | Persisted interpolation |
| Curve editor | P2 | Tangent editing, zoom/pan, multi-property visibility |
| Smooth operation | P2 | Deterministic tangent calculation and undo |

## Audio

| Feature | Priority | Completion criteria |
|---|---:|---|
| Multiple audio clips | P0 | Dedicated tracks and independent placement |
| Gain/mute/solo | P0 | Playback/export and meters |
| Waveforms | P0 | Cached, zoom-aware, proxy-independent timing |
| Fade in/out | P0 | Handles and inspector values |
| Extract audio | P1 | New timeline item references source stream |
| SFX/music library | P1 | Local assets/import workflow; licensing tracked |
| Pitch/EQ presets | P1 | Presets create editable parameters |
| Voice filters | P2 | Clearly described pitch/EQ effects, not AI generation |

## Stabilization

P2 because it requires analysis and caching.

Completion requires:

- Background analysis job.
- Progress and cancellation.
- Strength control.
- Crop/edge policy.
- Cached analysis invalidated when source interpretation changes.
- Preview/export parity within documented quality differences.

## Feature implementation template

For every feature create a task set in this order:

1. Domain schema and invariants.
2. Editing command and undo.
3. JSON migration/versioning.
4. Backend capability and renderer.
5. Preview integration.
6. Inspector/timeline/canvas UI.
7. Context menu and shortcut where relevant.
8. Automated tests.
9. Documentation and known CPU cost.

Do not reverse this order by starting with buttons.
