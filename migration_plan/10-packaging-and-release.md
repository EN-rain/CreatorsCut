# Packaging and Release Plan

> **Status:** ⬜ NOT STARTED — Packaging spike deferred to Phase 0 remaining tasks.

## Release target

- Windows 10/11 x64.
- CPU-only operation supported.
- Optional hardware acceleration may be detected later but is never required.
- Application launches from Start Menu/Desktop without terminal commands.

## Packaging candidates

### WiX Toolset

Preferred when the application needs conventional MSI installation, native DLL deployment, repair, and predictable enterprise behavior.

### MSIX

Use only if all native dependencies, writable locations, sidecar workers, and update behavior work correctly under MSIX restrictions.

The packaging spike in Phase 0 decides between them. Do not select based only on appearance.

## Runtime contents

The installer may include:

```text
CreatorCut.exe
CreatorCut.RenderWorker.exe
CreatorCut.MediaBridge.dll
.NET desktop runtime or self-contained runtime
MLT runtime/plugins if accepted by the media spike
FFmpeg binaries/libraries and required notices
Application assets
Default presets/templates
SQLite native/runtime components
Licenses and third-party notices
```

Do not download critical runtime dependencies silently on first launch unless a later product decision explicitly allows it.

## Directory policy

```text
Install files:
  Program Files\CreatorCut\

User settings/logs:
  %LocalAppData%\CreatorCut\

User project default:
  Documents\CreatorCut Projects\

Shared cache/proxies:
  %LocalAppData%\CreatorCut\Cache\
```

Never write project data into the installation directory.

## Project portability

Support two project modes:

1. Referenced media: project points to original files.
2. Managed media: selected files are copied into the project package.

A future “Collect Project” command may copy all referenced media into a portable folder. Do not copy large media automatically without user consent.

## Dependency licensing

Before release, create `THIRD_PARTY_NOTICES.txt` and record:

- Dependency name and exact version.
- License.
- Source URL.
- Whether dynamically or statically linked.
- Whether source-offer or attribution obligations apply.
- Codec/patent considerations where relevant.

Do not assume all FFmpeg builds have the same licensing configuration. The exact shipped build determines obligations.

## Updates

First stable release may use manual installer updates. Automatic updating is a separate feature and must include:

- Signed packages.
- Rollback/failure handling.
- Project schema compatibility.
- Native dependency consistency.

## Code signing

Release builds should be Authenticode signed before public distribution. Unsigned internal builds must be clearly identified.

## Crash handling

- Write local crash logs and session recovery data.
- Never upload media, project files, or logs automatically.
- Offer the user a way to open the log folder.
- Native worker crashes should produce a structured job failure and keep the editor open.

## Release channels

Recommended:

- `dev`: developer builds, diagnostics enabled.
- `preview`: user-test builds, migration backups mandatory.
- `stable`: signed builds passing all quality gates.

## Release checklist

- Clean Windows VM installation test.
- Launch without developer tools installed.
- Create/import/save/reopen project.
- Generate proxy.
- Preview and export sample timeline.
- Close and restore every panel.
- Verify right-click and middle-mouse behavior.
- Cancel render.
- Uninstall without deleting user projects.
- Validate third-party notices.
- Validate CPU-only operation.
