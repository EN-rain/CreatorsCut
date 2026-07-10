# Installer Format Decision

## Decision: **WiX Toolset v5** for the first stable release

### Candidates compared

| Format | Pros | Cons | Decision |
|---|---|---|---|
| **WiX Toolset v5** | MSI standard, enterprise-friendly, repair/modify, native DLL deployment, `ComponentGroup` ref counting | Steeper authoring syntax, requires WiX extension for .NET | ✅ **Selected** |
| **MSIX** | Sandboxed, clean uninstall, Store-ready | Writable locations restrictions, side-by-side worker processes need `modificationPackage`, native DLL trust issues | ❌ Rejected for v1 |
| **Squirrel.Windows** | Simple, auto-update | No MSI, limited enterprise features | ❌ Rejected |
| **dotnet publish single-file** | Zero installer work | No Start Menu, no uninstall, no file associations | ❌ Dev-only |

### Why not MSIX

1. FFmpeg worker processes (`.exe` sidecars) are blocked by MSIX package identity unless signed and declared.
2. Writable cache/proxy directories under `%LocalAppData%` need `modificationPackage` or broad capability declarations.
3. MSIX package signature required for sideloading — adds cost and friction for the first release.
4. If MSIX becomes a requirement later (e.g., Microsoft Store), WiX bundles can be converted.

## Installer contents (WiX v5)

```xml
<ComponentGroup Id="ProductComponents">
  <Component File="CreatorCut.exe" />
  <Component File="CreatorCut.RenderWorker.exe" />
  <Component File="ffmpeg.exe" />        <!-- bundled -->
  <Component File="*.dll" />              <!-- .NET runtime or self-contained -->
  <Component File="presets/*" />          <!-- default assets -->
</ComponentGroup>
```

## Directory layout

| Scope | Path |
|---|---|
| Install | `Program Files\CreatorCut\` |
| User settings | `%LocalAppData%\CreatorCut\` |
| User projects (default) | `Documents\CreatorCut Projects\` |
| Cache/proxies | `%LocalAppData%\CreatorCut\Cache\` |

## Update mechanism

First release: **manual installer download** (replace existing install).  
Automatic updates: deferred to post-v1 feature (requires signed packages, rollback, schema compatibility checks).
