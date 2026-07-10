# Editor Interaction Specification

> **Status:** ⬜ NOT STARTED — This is Phase 2/3 work. No WPF UI exists yet.

This document defines user input behavior. Agents must not invent conflicting controls.

## Global principles

- Commands work from menus, context menus, toolbar buttons, and shortcuts through the same command implementation.
- Input affects the surface under the pointer, not the whole application unexpectedly.
- Destructive actions are undoable.
- Context menus show only valid actions or clearly disable invalid ones.
- All drag operations show a preview and commit only on pointer release.
- Escape cancels the active transient tool or drag.

## Mouse behavior by surface

| Input | Timeline | Preview | Other panels |
|---|---|---|---|
| Left click | Seek/select | Play/pause only when configured; otherwise select canvas | Standard control action |
| Left drag | Move/trim/select depending on target/tool | Manipulate selected visual layer | Standard drag |
| Middle drag | Pan timeline horizontally/vertically | Pan zoomed preview | No window minimize behavior initiated by app |
| Wheel | Vertical track scroll | Zoom only when pointer is over preview and configured; otherwise no action | Scroll panel |
| Shift + wheel | Horizontal timeline scroll | Horizontal pan when zoomed | Horizontal scroll if supported |
| Ctrl + wheel | Timeline zoom around pointer time | Preview zoom around pointer point | Standard zoom only where defined |
| Right click | Context menu for clip/track/empty space | Preview/layer context menu | Panel-specific context menu |
| Double click | Open item editor or fit timeline region | Fit preview or enter direct transform mode | Panel-defined action |

The application must handle mouse messages inside its own surfaces. It must not simulate OS-level middle-click behavior.

## Timeline context menus

### Clip context menu

Show actions based on item type:

```text
Cut
Copy
Paste After
Duplicate
Split at Playhead
Trim Start to Playhead
Trim End to Playhead
Delete
Ripple Delete
Enable/Disable
Link/Unlink
Detach Audio                 # video with audio only
Replace Media
Speed…
Reverse
Freeze Frame at Playhead
Add Transition               # valid edge/neighbor only
Add Effect…
Properties
```

Audio clips additionally show:

```text
Normalize
Mute Clip
Audio Gain…
Fade In
Fade Out
```

Text/sticker/image clips show relevant transform/style actions and omit audio-only actions.

### Empty track context menu

```text
Paste
Add Video/Image
Add Audio
Add Text
Add Sticker
Add Track Above
Add Track Below
Track Properties
```

### Track header context menu

```text
Rename Track
Mute
Solo
Lock
Hide
Move Track Up
Move Track Down
Duplicate Track
Delete Track
```

Delete Track requires confirmation when non-empty and must be undoable.

## Preview context menu

When a visual layer is selected:

```text
Fit Preview
100%
25% / 50% / 200%
Reset Pan
Reset Transform
Center Layer
Fit Layer to Canvas
Fill Canvas
Bring Forward
Send Backward
Copy Transform
Paste Transform
Add Keyframe
Properties
```

When no layer is selected, omit layer actions.

## Keyboard shortcuts

Initial defaults:

| Shortcut | Command |
|---|---|
| Space | Play/pause |
| S or Ctrl+B | Split selected clips at playhead |
| Delete | Delete selection |
| Shift+Delete | Ripple delete |
| Ctrl+Z | Undo |
| Ctrl+Shift+Z or Ctrl+Y | Redo |
| Ctrl+C | Copy |
| Ctrl+X | Cut |
| Ctrl+V | Paste |
| Ctrl+D | Duplicate |
| Home | Go to sequence start |
| End | Go to sequence end |
| Left/Right | Previous/next frame when stopped |
| Shift+Left/Right | Larger seek step |
| I / O | Set source or range in/out where applicable |
| M | Add marker |
| F | Fit preview |
| Ctrl+0 | Reset workspace layout |
| + / - | Timeline zoom in/out when timeline focused |

Shortcuts must respect text-entry focus. Pressing Delete while typing in a text box must delete text, not a timeline clip.

## Timeline tools

Initial tools:

- Selection tool.
- Razor/split tool.
- Hand/pan tool.
- Optional ripple trim mode.

Tool state is visible. Temporary overrides may use modifier keys, but agents must document them.

## Dragging clips

- Dragging the body moves a clip.
- Dragging left/right edges trims.
- Dragging across tracks validates item compatibility.
- Snapping targets include playhead, markers, clip edges, and sequence boundaries.
- Holding Alt duplicates during drag after the behavior is implemented and tested.
- Holding Shift may disable snapping.
- A tooltip shows timeline start, duration, and trim values.
- Invalid drop targets show a prohibited cursor and do not mutate the project.

## Hover text bug prevention

Timeline labels must not change layout, font weight, blur/filter, or transform on hover.

Allowed hover changes:

- Border color.
- Background tint.
- Cursor.
- Tooltip.
- Trim-handle visibility.

Text must remain clipped with ellipsis. Full name and timing appear in a tooltip after a short delay.

## Panel close and restore

Every closable panel must be restorable through:

```text
View > Panels > [Panel Name]
```

Additionally, the default workspace may expose a visible closed-panel strip. The menu path is mandatory even if the strip is removed.

Closing Preview or Timeline must not leave playback or input capture active.

## Focus and capture safety

- Pointer capture begins only during an active drag.
- Pointer capture is always released on pointer up, cancel, lost capture, window deactivation, and exception cleanup.
- Window deactivation cancels unfinished drag edits.
- No edit commits twice.
- Context menu opening cancels transient hover-only state but does not change selection unless intended.

## Accessibility

- Commands have text labels and tooltips.
- Context menus are keyboard navigable.
- Focus indicators are visible.
- Timeline items expose accessible names including type, label, start, and duration.
- Color is not the only indicator of selection or invalid state.
