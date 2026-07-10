# Phase 7 — Advanced Timing, Color, Effects, and Audio

## Goal

Implement the advanced manual editing features requested for CreatorCut while keeping all presets editable.

## Timing

- Constant speed from 0.1x to 100x where the backend supports it.
- Preserve or alter pitch as an explicit option.
- Manual speed curve editor.
- Editable presets: bullet, montage, jump cut, hero time, flash in, flash out.
- Freeze frame at arbitrary source time.
- Reverse playback with proxy/cache generation when needed.

## Color

- Brightness, contrast, saturation, exposure, highlights, shadows, whites, tint, vintage.
- Black-and-white conversion.
- Per-channel and master HSL controls.
- Curves editor with editable control points.
- Color-match operation that writes normal editable adjustment values.
- Noise-reduction strength with draft-preview behavior documented.

## Effects and filters

- Registry-driven effect definitions rather than hard-coded buttons.
- Categories: mono, retro, style, food, night, nature, spark, love, lens, distortion, body/motion.
- Adjustable intensity and exposed parameters.
- Motion blur with samples/strength and CPU-quality levels.
- Effects stack can reorder, enable/disable, duplicate, reset, and remove.

## Audio

- Music and SFX library metadata; do not bundle unlicensed content.
- Multiple dedicated audio tracks.
- Extract audio from video as a new referenced asset.
- Gain, fades, pan, mute, solo, and clip normalization option.
- Pitch/EQ presets such as Micro, Low, High, Sweet, and Flashback.
- Presets remain editable through pitch and EQ parameters.
- Waveform regeneration after time or audio effects where required.

## Background jobs

- Reverse, proxy generation, waveform analysis, noise reduction, and expensive effects must report progress and support cancellation.
- Closing a project must safely cancel or detach jobs.

## Tests

- Speed mapping at segment boundaries.
- Freeze/reverse frame correctness.
- Color parameter serialization.
- Effect-stack order parity between preview and export.
- Audio timing and gain comparison.
- Preset application creates editable parameters.

## Exit gate

- Every feature has editable state, undo/redo, persistence, preview, export, and tests.
- Fixed inputs and settings produce deterministic exports.
- Heavy operations never block the WPF UI thread.
