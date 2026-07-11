# WPF Docking Strategy Decision

## Decision: Use the built-in WPF `DockPanel` / `Grid` splitters for Phase 2, defer third-party libraries

### Options considered

| Library | License | NuGet | Decision |
|---|---|---|---|
| **AvalonDock** (Extended WPF Toolkit) | Ms-PL / OSS | Yes | ❌ Heavy dependency, overkill for initial 5-panel layout |
| **DockPanelSuite** for WPF | LGPL | No | ❌ No stable WPF port |
| **Built-in WPF `GridSplitter` + `DockPanel`** | Built-in | Built-in | ✅ **Selected for Phase 2** |
| **Avalonia** | MIT | Yes | ❌ Different framework, not WPF |

## Rationale

1. **Phase 2 requires only 5 panels** (Media, Preview, Inspector, Timeline, Tasks/Campaign). A `Grid` with `GridSplitter` handles this well.
2. **No floating/tear-off panels** are required for the initial release. The interaction spec says "close and restore through View > Panels" — no drag-to-undock.
3. **AvalonDock adds ~1MB+** to the installer and has its own focus/input quirks that conflict with the interaction spec.
4. **Workspace layout persistence** (save/restore panel sizes) can be done with simple `GridLength` serialization to JSON.

## Strategy

```csharp
// MainWindow.xaml layout
// <Grid>
//   <Grid.RowDefinitions>
//     <RowDefinition Height="2*" />  <!-- Timeline -->
//     <RowDefinition Height="Auto" /> <!-- Splitter -->
//     <RowDefinition Height="5*" />  <!-- Preview + Inspector -->
//   </Grid.RowDefinitions>
//   <Grid.ColumnDefinitions>...</ColumnDefinitions>
//   <GridSplitter />s between panels
// </Grid>
```

Panel close → collapse to `GridLength(0)`.  
Panel restore → set back to saved size.  
Layout saved to `%LocalAppData%\Rushframe\layout.json`.

## Future upgrade path

If floating panels are needed post-v1, upgrade to `AvalonDock` by swapping the layout container while keeping the same view models. The `IPanelViewModel` interface is designed for this:

```csharp
public interface IPanelViewModel
{
    string Title { get; }
    bool IsVisible { get; set; }
    double SavedSize { get; set; }
}
```
