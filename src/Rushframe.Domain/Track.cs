namespace Rushframe.Domain;

public enum TrackKind { Video, Audio, Music, Voice, Text, Overlay }

public sealed class Track
{
    public TrackId Id { get; init; } = TrackId.New();
    public TrackKind Kind { get; init; }
    public string Name { get; set; } = "";
    public int Order { get; set; }
    public bool Muted { get; set; }
    public bool Solo { get; set; }
    public bool Locked { get; set; }
    public bool Hidden { get; set; }
    public List<TimelineItem> Items { get; init; } = [];
}
