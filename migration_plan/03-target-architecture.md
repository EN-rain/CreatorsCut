# Target Architecture

> **Status:** ✅ DONE — Solution skeleton created at `Rushframe.slnx` with 8 projects matching this layout. Architecture tests verify dependency direction.

## Solution layout

Create a new solution without deleting the legacy application:

```text
Rushframe.sln
src/
  Rushframe.Desktop/            # WPF executable, views, resources, composition root
  Rushframe.Application/        # Use cases, commands, services, job coordination
  Rushframe.Domain/             # Pure timeline/project model and invariants
  Rushframe.Infrastructure/     # SQLite, filesystem, settings, logging, autosave
  Rushframe.Media.Abstractions/ # Media interfaces and cross-boundary DTOs
  Rushframe.Media.Native/       # C# P/Invoke or C++/CLI adapter
  Rushframe.LegacyImport/       # Read-only importer for existing Python project data
native/
  Rushframe.MediaBridge/        # Optional native bridge DLL
  third_party/                   # Build scripts/manifests, not untracked binary dumping
tests/
  Rushframe.Domain.Tests/
  Rushframe.Application.Tests/
  Rushframe.Infrastructure.Tests/
  Rushframe.Media.Tests/
  Rushframe.Desktop.Tests/
  Rushframe.LegacyImport.Tests/
```

Use `Directory.Build.props`, central package management, nullable reference types, warnings as errors for new code, and x64 as the first supported target.

## Dependency rules

```text
Desktop ───────→ Application ───────→ Domain
   │                   │
   └────────────→ Infrastructure
                       │
Application ─────→ Media.Abstractions ←──── Media.Native
Application ─────→ LegacyImport
```

Forbidden dependencies:

- Domain → WPF
- Domain → SQLite
- Domain → FFmpeg/MLT
- Application → concrete WPF controls
- Desktop view code-behind → native media calls
- Media bridge → SQLite project schema

## Layer responsibilities

### Rushframe.Domain

Owns immutable identifiers and validated editing concepts:

- Project and sequence identifiers.
- Timeline, tracks, clips, transitions, effects, keyframes, markers.
- Rational time representation.
- Editing invariants.
- Pure edit operations where possible.

It must be deterministic and testable without files or native libraries.

### Rushframe.Application

Owns user intentions and orchestration:

- `SplitClipCommand`
- `TrimClipCommand`
- `MoveClipCommand`
- `AddMediaCommand`
- `AddTransitionCommand`
- `SetPropertyCommand`
- `UndoCommand` and `RedoCommand`
- Save, autosave, import, preview, proxy, and export jobs

Each command must return a typed result and an undo record or be explicitly non-undoable.

### Rushframe.Desktop

Owns presentation and input:

- Main window and docked panels.
- Timeline control and interaction state.
- Inspector views.
- Menus, toolbars, context menus, and shortcuts.
- Drag/drop and Windows file dialogs.
- Error dialogs and progress UI.

WPF should use commands and view models. Avoid business logic in code-behind.

### Rushframe.Infrastructure

Owns external persistence and OS services:

- SQLite repositories.
- JSON document storage.
- Filesystem abstraction.
- Settings.
- Logging.
- Crash recovery and autosave.
- Cache directories and cleanup.

### Rushframe.Media.Abstractions

Defines stable contracts:

```csharp
public interface IMediaProbeService;
public interface IProxyService;
public interface IThumbnailService;
public interface IWaveformService;
public interface IPreviewSession;
public interface IRenderService;
public interface IStabilizationService;
```

DTOs crossing this boundary must be explicit and versioned. Do not pass domain objects directly into native code.

### Rushframe.Media.Native

Maps managed requests to:

- MLT if the integration spike succeeds.
- FFmpeg/libav or FFmpeg subprocess fallback.
- WASAPI/NAudio playback where appropriate.

The rest of the application must not know which backend is active.

### Rushframe.LegacyImport

Reads existing Rushframe project files and maps them into the new domain model. It never writes into the legacy project directory.

## Process model

Initial release should use one desktop process plus isolated worker processes for crash-prone or long-running jobs:

```text
Rushframe.exe
├── UI thread
├── Application/background job threads
├── Native preview session
└── Rushframe.RenderWorker.exe processes
    └── FFmpeg/native rendering
```

Reasons:

- An encoder crash should not terminate the editor.
- Export cancellation is easier.
- Logs and progress are isolated.
- Memory is reclaimed when a worker exits.

## Time representation

Do not use `double` seconds as the canonical timeline time.

Use a rational or tick-based type:

```csharp
public readonly record struct MediaTime(long Numerator, long Denominator);
```

Or use integer timeline ticks with an explicit sequence timebase. Conversion to frames/samples must be deliberate.

## Undo/redo architecture

Use command-based history:

```text
User input
→ Editor command
→ Validate
→ Apply to immutable/current document
→ Record inverse or before-state delta
→ Notify UI and preview
→ Schedule autosave
```

Do not snapshot entire projects for every small edit. Use compact change sets, with periodic checkpoints for crash recovery.

## Preview architecture

Preview is a disposable projection of the domain timeline:

```text
Timeline revision
→ Preview graph builder
→ proxy-aware composition graph
→ decoded frame/audio buffers
→ WPF preview surface/audio device
```

Preview errors must not corrupt the project. A stale preview revision must be discarded.

## CPU-friendly defaults

- Generate 540p or 720p proxies.
- Default preview quality: 1/2 or 1/4 resolution.
- Decode only near the playhead.
- Maintain bounded frame, thumbnail, and waveform caches.
- Pause expensive background work during active playback if needed.
- Effects must declare preview cost and support bypass/draft modes.
- Export always references original media unless the user explicitly chooses proxy export.
