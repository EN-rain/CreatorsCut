# Manual QA Runbook

Use this before relying on a new build for private editing.

## A. Five-minute smoke test

1. Launch Rushframe.
2. Confirm Media, Preview, Inspector, Timeline, and Tasks panels are reachable.
3. Import one MP4 and one audio file.
4. Add both to the timeline.
5. Move the video clip.
6. Trim both edges.
7. Split at the playhead.
8. Copy, paste, duplicate, and delete a clip.
9. Undo and redo each operation.
10. Save the project.
11. Close and reopen it.
12. Export a short MP4.
13. Open the output externally and verify video and audio.

Stop and file a High or Blocker bug if project state, source media, or export is wrong.

## B. Timeline input regression

Check:

- Middle mouse pans only the timeline.
- Mouse wheel zooms according to the documented behavior.
- Right-click opens the correct menu.
- Dragging does not move the whole window or trigger Windows behavior.
- Releasing the mouse outside the timeline does not leave a stuck drag.
- Left and right trims affect the correct edge.
- Snap on/off changes behavior.
- Locked tracks/items do not move.
- Delete key does not delete while typing in an Inspector textbox.
- Ctrl+C/X/V and menu commands behave identically.

## C. Inspector regression

For one selected clip:

1. Change X/Y position.
2. Change scale.
3. Rotate.
4. Set opacity to 0.5.
5. Set speed to 2x.
6. Toggle reverse.
7. Adjust brightness, contrast, and saturation.
8. Add one effect.
9. Enable stabilization.
10. Apply.
11. Undo all changes.
12. Redo all changes.
13. Save and reopen.
14. Export and inspect.

Confirm only the selected clip changes.

## D. Panel/workspace regression

- Resize all panel splitters.
- Hide each panel individually.
- Restore from `View > Panels`.
- Close Rushframe and reopen.
- Confirm layout persistence.
- Corrupt or temporarily rename layout settings and verify default fallback.

## E. Media edge cases

Import and inspect:

- Video with audio.
- Silent video.
- Audio-only file.
- PNG with alpha.
- Portrait video.
- File with spaces and Unicode characters.
- Corrupt file.
- File that is moved after import.

Expected: no crash, clear offline/error behavior, correct media kind.

## F. Save/recovery scenario

1. Save a project.
2. Make several edits.
3. Wait for autosave.
4. Force-close Rushframe through Task Manager.
5. Relaunch.
6. Recover or inspect autosave according to current UI.
7. Confirm the original saved project remains valid.

## G. Export regression project

Create a short project containing:

- Two sequential video clips.
- One overlay image.
- One text item if available.
- Position/scale/rotation.
- Opacity.
- One color adjustment.
- One effect.
- One audio item.

Verify externally:

- Output opens.
- Duration is correct.
- Resolution/frame rate are correct.
- Layers appear at intended times.
- Audio is present and synchronized.
- Source media files have unchanged modification dates and hashes where practical.

## H. Cancellation test

Start each available long operation and cancel:

- Proxy generation.
- Stabilization analysis.
- Export.

Confirm UI remains usable and no FFmpeg process remains in Task Manager.

## I. Thirty-minute real-work test

Complete an actual small edit from import through export. Record:

- Crashes.
- Confusing actions.
- Slow operations.
- Preview/export differences.
- Missing functionality.
- Workarounds required.

A build is acceptable for private use only if no Blocker or High defect affects the owner’s intended workflow.

## J. Sign-off record

Record:

```text
Date:
Commit/build:
Windows version:
FFmpeg version:
Project used:
Automated tests result:
Smoke result:
Export video verified:
Export audio verified:
Source files unchanged:
Known Medium/Low issues:
Decision: PASS / FAIL
Tester:
```
