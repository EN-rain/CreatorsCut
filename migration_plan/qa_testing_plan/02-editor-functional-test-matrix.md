# Editor Functional Test Matrix

Use unique IDs in bug reports and automated test names.

## A. Application startup and workspace

| ID | Test | Expected result | Type | Current coverage |
|---|---|---|---|---|
| APP-001 | Launch with empty AppData | Default workspace opens; required panels are reachable | Manual + UI automation | Partial |
| APP-002 | Launch with valid saved layout | Panel visibility and sizes restore | Automated + manual | Partial |
| APP-003 | Launch with corrupt layout JSON | Default layout loads; app does not crash | Automated | Partial |
| APP-004 | Close every optional panel | `View > Panels` remains available | Manual/UI | Partial |
| APP-005 | Restore each closed panel | Correct panel returns in usable size | Manual/UI | Partial |
| APP-006 | Resize Media, Timeline, Inspector, Tasks | Sizes change smoothly and remain valid | Manual | Not covered |
| APP-007 | Close while autosave/background work runs | App shuts down without hanging or corrupting project | Integration/manual | Not covered |
| APP-008 | Repeated open/close 25 times | No accumulating process, handle, or memory leak | Soak | Not covered |

## B. Project lifecycle

| ID | Test | Expected result | Type | Current coverage |
|---|---|---|---|---|
| PRJ-001 | Create/open empty project | Valid sequence exists; no fake production clip is added | Automated/manual | Verify implementation |
| PRJ-002 | Save new project | `.rushframe` file is written atomically | Integration | Partial |
| PRJ-003 | Save over existing project | New valid file replaces previous version; backup/recovery policy applies | Integration | Partial |
| PRJ-004 | Open saved project | Tracks, item IDs, properties, media, markers, effects restore | Automated | Partial |
| PRJ-005 | Save, close, reopen, save again | No data drift after repeated round trips | Automated | Partial |
| PRJ-006 | Open malformed project JSON | Actionable error; app remains usable | Integration/manual | Not covered |
| PRJ-007 | Open unsupported future schema | Refuse safely or preserve unknown data according to schema rules | Automated | Partial |
| PRJ-008 | Cancel Open/Save dialog | No project state changes | Manual/UI | Not covered |
| PRJ-009 | Save to read-only/unavailable folder | Clear error; current in-memory project remains valid | Manual/integration | Not covered |
| PRJ-010 | Path with spaces, Unicode, apostrophe, long path | Open/save succeeds | Integration | Not covered |

## C. Media panel and import

| ID | Test | Expected result | Type | Current coverage |
|---|---|---|---|---|
| MED-001 | Import supported video | Correct Video kind and duration after probe | Integration | Partial |
| MED-002 | Import MP3/WAV/AAC/FLAC | Correct Audio kind | Automated/integration | Partial |
| MED-003 | Import PNG/JPEG/WebP/BMP | Correct Image kind | Automated/integration | Partial |
| MED-004 | Import multiple mixed files | All valid files appear once; failures do not cancel valid imports | Manual/integration | Not covered |
| MED-005 | Import duplicate path twice | Defined duplicate policy is applied consistently | Automated/manual | Not covered |
| MED-006 | Import corrupt media | Asset is rejected or marked with actionable error; no crash | Integration | Not covered |
| MED-007 | FFprobe unavailable | Import remains usable with fallback/clear warning | Integration | Partial behavior exists |
| MED-008 | Add selected video to timeline | Video track receives correctly linked clip at playhead | Automated/UI | Partial |
| MED-009 | Add selected audio to timeline | Audio track receives correctly linked item | Automated/UI | Partial |
| MED-010 | Add selected image to timeline | Overlay/image item gets expected default duration | Automated/UI | Partial |
| MED-011 | Add media when compatible track is locked | New or alternate compatible track is used; locked track unchanged | Automated | Not covered |
| MED-012 | Preview selected video/audio | Source media loads and controls behave correctly | Manual/UI | Partial |
| MED-013 | Preview missing media | Clear offline message; no stale previous media playback | Manual | Not covered |

## D. Selection and playhead

| ID | Test | Expected result | Type | Current coverage |
|---|---|---|---|---|
| SEL-001 | Click clip body | Only correct clip becomes selected | UI/manual | Partial |
| SEL-002 | Click empty timeline | Selection clears or follows documented behavior | UI/manual | Partial |
| SEL-003 | Click ruler | Playhead moves to pointer time | UI/manual | Partial |
| SEL-004 | Drag playhead | Playhead follows pointer and clamps to valid time | UI/manual | Partial |
| SEL-005 | Select overlapping clips on different tracks | Hit-testing selects top/correct track under pointer | UI/manual | Not covered |
| SEL-006 | Select after zoom/pan | Hit-testing remains accurate | UI/manual | Not covered |
| SEL-007 | Inspector follows selection | Correct item values appear; no previous-item values leak | UI/manual | Partial |
| SEL-008 | Delete selected clip | Correct item removed; unrelated items unchanged | Automated/UI | Partial |
| SEL-009 | Multi-select/marquee | Either works as specified or is clearly unavailable | Manual | Blocked/not implemented |

## E. Timeline navigation

| ID | Test | Expected result | Type | Current coverage |
|---|---|---|---|---|
| NAV-001 | Mouse wheel over timeline | Timeline zoom follows documented behavior only | UI/manual | Partial |
| NAV-002 | Middle-mouse drag timeline | Timeline pans; window/site does not minimize or navigate | UI/manual | Partial |
| NAV-003 | Release middle mouse outside control | Capture releases; panning stops | UI/manual | Not covered |
| NAV-004 | Alt-tab during pan/drag | Operation cancels safely; no stuck cursor/capture | Manual | Not covered |
| NAV-005 | Zoom around pointer | Time under cursor remains stable within tolerance | Automated/UI | Partial |
| NAV-006 | Maximum/minimum zoom | Zoom clamps; no overflow, negative scale, or freeze | Automated/UI | Partial |
| NAV-007 | Horizontal pan before zero | Offset clamps to zero | Automated | Partial |
| NAV-008 | Large project pan/zoom | Interaction remains responsive | Performance | Not covered |

## F. Add, move, trim, split, duplicate, delete

| ID | Test | Expected result | Type | Current coverage |
|---|---|---|---|---|
| EDT-001 | Add clip at playhead | Correct track, start, source, and duration | Automated | Partial |
| EDT-002 | Drag clip horizontally | Command commits correct new start | Automated/UI | Partial |
| EDT-003 | Drag clip vertically to compatible track | Item moves track and keeps ID/properties | Automated/UI | Partial |
| EDT-004 | Drag to incompatible track | Drop rejected with visible feedback; model unchanged | Automated/UI | Partial/not covered visually |
| EDT-005 | Drag locked clip/track | No change; clear feedback | Automated/UI | Not covered |
| EDT-006 | Move before time zero | Start clamps/rejects according to domain rule | Automated | Partial |
| EDT-007 | Undo/redo drag move | Exact original/new track and time restore | Automated/UI | Partial |
| EDT-008 | Trim right edge | Duration/source mapping updates correctly | Automated/UI | Partial |
| EDT-009 | Trim left edge | Timeline start, source start, and duration update correctly | Automated/UI | Partial |
| EDT-010 | Trim below minimum duration | Operation clamps/rejects; never zero/negative | Automated | Covered domain |
| EDT-011 | Undo/redo trim | Exact source/timeline values restore | Automated | Covered domain; UI partial |
| EDT-012 | Split at valid interior playhead | Two clips preserve total source range and properties | Automated/UI | Covered domain; UI partial |
| EDT-013 | Split at clip start/end/outside | No invalid zero-length clip; clear no-op/error | Automated | Partial |
| EDT-014 | Duplicate clip | New ID; all intended properties copied; deterministic placement | Automated/UI | Partial |
| EDT-015 | Delete clip | Correct item deleted and undo restores same ID | Automated/UI | Covered domain |
| EDT-016 | Ripple delete enabled | Following items shift by exact removed duration | Automated/UI | Partial |
| EDT-017 | Ripple delete disabled | Gap remains; other items unchanged | Automated/UI | Partial |
| EDT-018 | Operation with overlapping items | Defined overlap behavior remains consistent | Automated/manual | Not covered |

## G. Cut, copy, paste and clipboard

| ID | Test | Expected result | Type | Current coverage |
|---|---|---|---|---|
| CLP-001 | Copy selected clip | Project unchanged; internal clipboard contains independent snapshot | Automated | Partial |
| CLP-002 | Cut selected clip | Snapshot copied, then selected clip deleted as undoable operation | Automated/UI | Partial |
| CLP-003 | Paste at playhead | New ID and correct target track/start | Automated/UI | Partial |
| CLP-004 | Paste to incompatible track | Rejected without partial mutation | Automated | Partial |
| CLP-005 | Paste after source clip deleted | Clipboard still pastes complete copied data | Automated | Not covered |
| CLP-006 | Repeated paste | Every paste receives unique IDs | Automated | Not covered |
| CLP-007 | Undo/redo cut and paste | Exact sequence state restores | Automated | Partial |
| CLP-008 | Paste with no clipboard | Safe no-op; no crash | Automated/UI | Not covered |

## H. Snapping, ripple, tracks, markers

| ID | Test | Expected result | Type | Current coverage |
|---|---|---|---|---|
| SNP-001 | Snap enabled near clip edge | Move/trim snaps within defined threshold | Automated/UI | Partial |
| SNP-002 | Snap disabled | No snapping occurs | Automated/UI | Partial |
| SNP-003 | Snap to playhead | Correct target and threshold | Automated/UI | Verify implementation |
| SNP-004 | Snap to marker | Correct marker target | Automated/UI | Not covered |
| SNP-005 | Zoom changes snapping | Threshold remains pixel-consistent or documented | Automated/UI | Not covered |
| TRK-001 | Add video/audio/overlay/text track | Correct kind/order/name | Automated/UI | Partial model |
| TRK-002 | Rename track | Persisted and undoable if supported | Automated/UI | Partial/model |
| TRK-003 | Mute/solo/lock/hide track | UI and export obey flags | Automated/media/manual | Partial |
| TRK-004 | Delete non-empty track | Confirmation/policy; undo restores items | Automated/UI | Partial model |
| TRK-005 | Reorder tracks | Layer order and export change consistently | Automated/media/UI | Not covered |
| MRK-001 | Add/move/delete marker | Correct time/name/color and undo behavior | Automated/UI | Partial model |
| MRK-002 | Marker persists | Save/reopen retains marker | Automated | Partial |

## I. Inspector and properties

For every editable property, test valid value, minimum, maximum, invalid text, undo, redo, save/reopen, preview behavior, and export behavior.

| ID | Property/function | Expected result | Current coverage |
|---|---|---|---|
| INS-001 | Position X/Y | Applied to selected item only; persisted/exported | Partial |
| INS-002 | Uniform scale | Positive clamped value; X/Y updated consistently | Partial |
| INS-003 | Rotation | Supports negative and >360 values according to policy | Partial |
| INS-004 | Opacity | Clamped 0–1; exported alpha correct | Partial |
| INS-005 | Constant speed | Clamped 0.1–100; timeline/export duration rules consistent | Partial |
| INS-006 | Reverse | Exported source reverses; cancellation works | Partial |
| INS-007 | Brightness/contrast/saturation/B&W | Filter order and values persist | Partial |
| INS-008 | Stabilization toggle/analysis | Background job status and result persist | Partial |
| INS-009 | Add effect | Correct default parameters and order | Partial |
| INS-010 | Disable/remove/reorder effect | Stack updates and export follows order | Partial/model |
| INS-011 | No selection | Inspector disabled; Apply does nothing safely | Manual | Partial |
| INS-012 | Switch selection with unsaved textbox edits | Defined apply/discard behavior; no property leakage | Manual | Not covered |
| INS-013 | Invalid numeric input | No crash; value rejected or fallback clearly indicated | Automated/UI | Partial fallback only |

## J. Undo/redo regression matrix

Every destructive or property-changing operation must be tested in these sequences:

1. Execute → Undo.
2. Execute → Undo → Redo.
3. Execute A → Execute B → Undo B → Undo A.
4. Execute A → Undo A → Execute C; redo history clears.
5. Save/reopen after operations.
6. Perform 100+ commands to verify depth behavior.
7. Undo after selection changes.
8. Undo after failed/no-op command; stack must remain correct.

Operations requiring this matrix:

- Add/delete/move/trim/split/duplicate/ripple delete.
- Cut/paste.
- Track and marker commands.
- Transform, opacity, speed, reverse, color, stabilization.
- Effect add/remove/reorder/parameter edits.
- Text, transitions, masks, chroma key, keyframes when implemented.

## K. Keyboard and context menus

| ID | Test | Expected result | Type |
|---|---|---|---|
| INP-001 | Right-click selected clip | Clip menu opens at pointer with valid actions | UI/manual |
| INP-002 | Right-click empty timeline | Empty-area menu or no menu according to spec; no wrong clip action | UI/manual |
| INP-003 | Right-click track header | Track menu applies to clicked track | UI/manual |
| INP-004 | Ctrl+C/X/V | Same commands as menus | UI/manual |
| INP-005 | Delete | Deletes selected item, not text inside focused textbox | UI/manual |
| INP-006 | Space | Playback only when timeline/preview context allows; does not type into fields | UI/manual |
| INP-007 | Ctrl+Z/Y | Undo/redo unless focused control owns local text undo per documented policy | UI/manual |
| INP-008 | Context menu item enablement | Invalid actions disabled rather than silently doing nothing | UI/manual |
| INP-009 | Keyboard focus after dialog/menu | Timeline shortcuts resume without stuck focus | UI/manual |

## L. Features not yet fully implemented

These remain **(BLOCKED)** for full QA and must not be marked passed based only on domain types:

- True composed timeline preview.
- Multi-selection/marquee.
- Full waveform and thumbnail timeline rendering.
- Complete text authoring/styling/animation.
- Keyframe editor and runtime evaluation.
- Complete transitions.
- Complete masks and feathering.
- Complete audio mixer, fades, gain, mute/solo export.
- Speed curves and freeze frame.
- Advanced color/HSL/curves.

Agents must create tests alongside implementation and change these entries only after observable behavior exists.
