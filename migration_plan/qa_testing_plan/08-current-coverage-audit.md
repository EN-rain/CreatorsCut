# Current QA Coverage Audit

Audit date: 2026-07-10

## Active solution identity

The active .NET solution and projects are named `Rushframe`:

```text
Rushframe.slnx
src/Rushframe.*
tests/Rushframe.*
```

Agents must use the active solution names when building or testing.

## Latest automated result

Command:

```powershell
dotnet build Rushframe.slnx -c Release --no-restore
dotnet test Rushframe.slnx -c Release --no-restore -v:minimal
```

Result:

- Domain tests: 94 passed.
- Desktop tests: 17 passed.
- Legacy-import tests: 7 passed.
- Media integration tests: 5 passed.
- Total: 123 passed.
- Failed: 0.

## What is covered well

### Domain/editor model — strong

Automated tests cover substantial parts of:

- Architecture dependency direction.
- Media time.
- Project model.
- Add, delete, move, trim, split, duplicate, ripple and related edit behavior.
- Undo/redo.
- Serialization.
- Track/marker/effect/transition/keyframe and other domain behavior contained in `EditingTests.cs`.
- Basic cache behavior.
- Project repository save/load.
- Autosave newest-recovery path.

Status: **(AUTOMATED / STRONG)**

### Legacy import — moderate

Seven tests cover core legacy importer paths and fixtures.

Status: **(AUTOMATED / PARTIAL)**

### Workspace data classes — moderate

Fifteen desktop tests cover panel registry, workspace layout classes, and related shell-level behavior.

Status: **(AUTOMATED / PARTIAL)**

## Important coverage gaps

### Media Intelligence timeline integration — moderate

Automated tests cover:

- Importing the pipeline's `media-analysis.json` schema.
- Rejecting malformed scene/transcript time ranges.
- Deduplicating scene tags and preserving warnings.
- Mapping source-time scenes and transcript segments through clip source trim and speed.
- Creating scene markers and `AI Captions` text clips.
- Replacing prior generated content for the same media asset.
- Undo restoration.
- `.rushframe` serialization of analysis metadata and generated links.

A runtime smoke test confirms the desktop application still starts. Direct UI automation for the Run/Add-to-Timeline buttons and marker clicks remains missing.

Status: **(AUTOMATED / PARTIAL)**

### Real WPF editor interaction

No adequate automated coverage was found for:

- MainWindow command wiring.
- Timeline mouse input.
- Middle-mouse pan.
- Wheel zoom.
- Right-click menus.
- Keyboard focus conflicts.
- Move/trim event-to-command routing.
- Media panel add-to-timeline.
- Inspector apply behavior.
- Source preview.
- Panel interactions in a real window.

Status: **(NOT COVERED / MANUAL REQUIRED)**

### FFmpeg media services

`tests/Rushframe.Media.Tests` now covers:

- Probe.
- Proxy generation.
- Thumbnail generation.
- Waveform generation.
- Timeline export.
- Export with audio stream presence.
- Audio extraction.
- Cancellation.
- FFmpeg process cancellation path.

Status: **(AUTOMATED / PARTIAL)**

### Export correctness

Automated media tests now create synthetic video/audio fixtures, export a timeline, and assert that the result contains video and audio streams.

Remaining gaps:

- No golden frame/image comparison yet.
- No exact loudness/tone validation yet.
- Complex effects/composition parity still needs manual or golden tests.

Status: **(AUTOMATED / PARTIAL)**

### Persistence and recovery

Coverage is incomplete for:

- Atomic save failure.
- Corrupt project handling.
- Autosave recovery beyond newest-autosave load.
- Force-kill recovery.
- Read-only destination.
- Long/Unicode paths.
- Offline media relink.

Status: **(PARTIAL / HIGH RISK)**

### Performance and soak

No repeatable automated results were found for:

- 1,000-item timeline interaction.
- Memory growth.
- Repeated project open/close.
- Long edit session.
- Repeated cancellation.
- Orphan FFmpeg processes.

Status: **(NOT COVERED)**

## Current QA maturity by area

| Area | Status |
|---|---|
| Domain commands | **(AUTOMATED)** |
| Undo/redo | **(AUTOMATED)** |
| Serialization | **(AUTOMATED / PARTIAL)** |
| Legacy import | **(AUTOMATED / PARTIAL)** |
| Workspace model | **(AUTOMATED / PARTIAL)** |
| Media Intelligence timeline mapping | **(AUTOMATED / PARTIAL)** |
| Real WPF interactions | **(MANUAL / PARTIAL)** |
| Media services | **(AUTOMATED / PARTIAL)** |
| Preview behavior | **(MANUAL / PARTIAL)** |
| Export video correctness | **(AUTOMATED / PARTIAL)** |
| Export audio correctness | **(AUTOMATED / PARTIAL)** |
| Autosave/crash recovery | **(PARTIAL)** |
| Cache reliability | **(PARTIAL)** |
| CPU performance | **(NOT COVERED)** |
| Soak/stability | **(NOT COVERED)** |
| Clean-machine launch/install | **(NOT COVERED)** |

## Immediate QA implementation order

1. Add golden render frame/tone validation for representative timeline exports.
2. Add WPF UI tests for context menu, middle mouse, shortcuts, move, trim, Inspector, and panel restore.
3. Add corrupt-project, force-kill recovery, read-only, long-path, and Unicode-path tests.
4. Add 1,000-item timeline benchmark and repeated cancellation/process-leak test.
5. Run the manual private-use runbook with the owner's real sample media.

## Correct conclusion

The current 123 tests now cover domain behavior, workspace data, legacy import, media services, derivative generation, audio extraction, timeline export, export audio presence, Media Intelligence import/timeline mapping, and key recovery paths. Rushframe still requires manual WPF interaction checks and real-project smoke testing before important private editing work.
