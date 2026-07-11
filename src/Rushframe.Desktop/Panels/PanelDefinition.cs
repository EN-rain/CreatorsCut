namespace Rushframe.Desktop.Panels;

public sealed class PanelDefinition
{
    public PanelId Id { get; init; }
    public string Title { get; init; } = "";
    public bool IsVisibleByDefault { get; init; } = true;
    public bool CanClose { get; init; } = true;
    public bool CanFloat { get; init; }
}
