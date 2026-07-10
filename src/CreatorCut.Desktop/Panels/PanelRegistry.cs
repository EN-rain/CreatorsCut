using System.Collections.Immutable;

namespace CreatorCut.Desktop.Panels;

public static class PanelRegistry
{
    private static readonly ImmutableArray<PanelDefinition> _panels =
    [
        new() { Id = PanelId.Media, Title = "Media", CanFloat = true },
        new() { Id = PanelId.Preview, Title = "Preview", CanFloat = true },
        new() { Id = PanelId.Inspector, Title = "Inspector", CanFloat = true },
        new() { Id = PanelId.Timeline, Title = "Timeline", CanClose = false, CanFloat = true },
        new() { Id = PanelId.Tasks, Title = "Tasks / Campaign", CanFloat = true },
        new() { Id = PanelId.RenderQueue, Title = "Render Queue", CanFloat = true },
    ];

    public static ImmutableArray<PanelDefinition> All => _panels;

    public static PanelDefinition? Find(PanelId id) =>
        _panels.FirstOrDefault(p => p.Id == id);
}
