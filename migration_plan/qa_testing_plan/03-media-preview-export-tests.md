# Media, Preview, and Export Test Plan

## 1. Probe tests

For every supported fixture verify:

- File path returned unchanged.
- Duration within 1 frame or 20 ms tolerance.
- Correct stream count and stream kinds.
- Codec names populated.
- Width, height, frame rate, channels, and sample rate correct where applicable.
- File size correct.
- Cancellation stops FFprobe and returns control.
- Missing/corrupt files produce actionable errors.

## 2. Proxy generation

Test:

- Landscape, portrait, square, odd-width, silent, and audio/video inputs.
- Requested max height is respected and width remains even.
- Aspect ratio is preserved.
- Audio remains present when source has audio.
- Existing output replacement behavior is deterministic.
- Output folder creation.
- Cancellation removes or clearly marks partial output.
- Source is never modified.
- Unicode and quoted paths work.

## 3. Thumbnail generation

Test timestamps:

- Zero.
- Middle.
- Exact final frame boundary.
- Beyond duration.
- Negative request.

Verify image exists, is decodable, and approximately matches the expected source frame.

## 4. Waveform generation

Test mono, stereo, silent, short, and compressed audio.

Verify:

- Correct requested dimensions.
- Silent audio produces a flat waveform.
- Non-silent audio contains visible activity.
- No-audio video returns a clear error or empty-state result.

## 5. Source preview

Manual tests:

- Video loads, starts, pauses, seeks, and stops.
- Audio-only media previews without stale video.
- Switching assets stops previous playback.
- Missing media clears the player and shows an error.
- Repeated switching does not leak file locks.
- Files remain movable/deletable after preview stops.
- Preview controls remain responsive with long media.

Important limitation: source preview is not composed timeline preview. QA must not treat source playback as proof that effects/layers are previewed correctly.

## 6. Timeline export baseline

Create a 5–10 second deterministic project and verify:

- Export file exists and is decodable.
- Resolution and frame rate match sequence settings.
- Duration is within one frame tolerance.
- Pixel format is broadly compatible.
- Cancellation stops the FFmpeg child process.
- Export failure does not report success.
- Source files are unchanged.

## 7. Video composition matrix

Test each separately, then representative combinations:

- One source clip.
- Sequential clips with gaps.
- Overlapping clips on different tracks.
- Position X/Y.
- Scale up/down.
- Rotation.
- Opacity 0, 0.5, 1.
- Reverse.
- Constant speed 0.5x, 2x.
- Fade in/out.
- Text layer.
- Blend modes.
- Rectangle mask.
- Chroma key.
- Basic color correction.
- Every built-in effect.
- Stabilization/deshake path.

For each feature verify:

1. Model persists after save/reopen.
2. FFmpeg graph includes the intended operation.
3. Output visually matches a golden image or perceptual threshold.
4. Disabled effect/property does not alter output.
5. Undo restores previous output behavior.

## 8. Audio export matrix — critical

Current timeline export must be treated as **failing this section** while it uses `-an` or otherwise omits required audio.

Required cases:

- Source video audio retained.
- Audio-only timeline item mixed into video output.
- Multiple overlapping audio items.
- Audio start offset.
- Source trim.
- Per-clip gain.
- Fade in/out.
- Mute track.
- Solo track.
- Speed with pitch-preserve policy.
- Reverse audio policy.
- Gap/silence handling.
- Mixed sample rates/channels.
- Output duration matches video timeline.
- No clipping beyond defined loudness limit.

Measurements:

- Audio stream exists.
- Duration tolerance.
- Integrated loudness range.
- Peak level.
- Expected tone/frequency present in test fixtures.

## 9. Text tests

When text authoring is implemented:

- Empty, normal, multiline, Unicode, emoji, apostrophe, colon, backslash.
- Position, size, fill, opacity, alignment.
- Missing font fallback.
- Long text clipping/wrapping.
- Text start/end timing.
- Multiple text layers.
- Save/reopen.

## 10. Transition tests

When transition export is implemented:

- Every transition at valid duration.
- Duration longer than adjacent clip handles.
- Transition at timeline start/end.
- Different resolutions/frame rates.
- Audio transition/crossfade policy.
- Undo, save/reopen, export.

## 11. Golden render methodology

Do not require byte-identical MP4 output across FFmpeg versions.

Compare:

- ffprobe metadata.
- Frame count/duration.
- Selected decoded frames using perceptual similarity.
- Expected region colors/alpha for synthetic fixtures.
- Audio duration, tone presence, loudness, and peak bounds.

Commit tiny generated fixtures or generate them deterministically during tests using FFmpeg lavfi sources.

## 12. Error and cancellation tests

Test:

- FFmpeg executable missing.
- Unsupported codec.
- Corrupt input.
- Output path denied.
- Disk full simulation where practical.
- Child process non-zero exit.
- Cancellation during proxy, waveform, thumbnail, stabilization, and export.
- Closing app during a running job.

Expected:

- UI remains responsive.
- Error includes operation and useful FFmpeg stderr summary.
- No success notification.
- Partial files are removed or clearly identified.
- Retry works without restarting Rushframe.
