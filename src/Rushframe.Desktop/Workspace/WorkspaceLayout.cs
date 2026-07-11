using Rushframe.Desktop.Panels;

namespace Rushframe.Desktop.Workspace;

public sealed class WorkspaceLayout
{
    public const int SchemaVersion = 1;

    public int Version { get; init; } = SchemaVersion;

    public required Dictionary<string, PanelState> Panels { get; init; }

    public static WorkspaceLayout Default() => new()
    {
        Panels = PanelRegistry.All.ToDictionary(
            p => p.Id.Key,
            _ => new PanelState { IsOpen = true }),
    };

    public bool IsPanelOpen(PanelId id) =>
        Panels.TryGetValue(id.Key, out var state) && state.IsOpen;

    public WorkspaceLayout WithPanelToggled(PanelId id, bool open)
    {
        var updated = new Dictionary<string, PanelState>(Panels)
        {
            [id.Key] = new PanelState { IsOpen = open },
        };
        return new WorkspaceLayout { Panels = updated };
    }
}

public sealed class PanelState
{
    public bool IsOpen { get; init; } = true;
    public double Width { get; init; } = 250;
    public double Height { get; init; } = 200;
}
