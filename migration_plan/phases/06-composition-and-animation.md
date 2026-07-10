# Phase 6 — Composition and Animation

## Goal

Add layered compositing and manual animation without breaking CPU-only usability.

## Layer system

- At least ten visual layers in the model.
- Explicit track/layer order.
- Enable, disable, lock, solo, duplicate, and reorder.
- Picture-in-picture video and image overlays.
- Per-layer transform, opacity, crop, and blend mode.

## Masks

- Rectangle/linear.
- Circle/ellipse.
- Mirror/symmetry.
- Star/polygon.
- Manual split mask for side-by-side layouts.
- Invert, feather, expansion, position, scale, and rotation.
- Mask data is editable and keyframeable.

## Chroma key

- Manual color picker.
- Similarity/intensity.
- Edge softness.
- Spill/shadow suppression.
- Preview and export must use the same values.

## Keyframes

- Position, scale, rotation, anchor, opacity, mask properties, effect intensity, and supported text properties.
- Add, move, edit, delete, smooth, copy, and paste individual keyframes.
- Diamond markers on timeline/property rows.
- Linear, ease-in, ease-out, ease-in-out, hold, and custom Bezier easing.
- Deterministic interpolation at exact media times.

## Transitions

- Apply manually between compatible adjacent clips.
- Cross dissolve, slide, zoom, blur, wipe/split, whip-pan approximation, and mask-based transition foundation.
- Duration and parameters editable.
- Transition cannot exceed available handles without a clear validation error.

## Text animation

- Separate in, loop, and out animation slots.
- User controls duration, offset, easing, and parameters.
- Presets create editable keyframes/effects; presets are not opaque behavior.

## CPU rules

- Draft preview may lower resolution or skip expensive frames, but must display a clear quality indicator.
- Never silently alter final export quality.
- Heavy graphs render asynchronously and remain cancellable.

## Exit gate

- Preview/export parity tests cover stacked overlays, masks, chroma key, transitions, and keyframes.
- Ten-layer project opens, edits, saves, and exports.
- All animation data survives save/reload exactly.
