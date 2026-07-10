namespace CreatorCut.Domain.Editing;

public sealed class TrimClipCommand : IEditCommand
{
    public string Description => $"Trim clip {ItemId}";

    public required TrackId TrackId { get; init; }
    public required TimelineItemId ItemId { get; init; }
    public MediaTime? NewStart { get; init; }
    public MediaTime? NewDuration { get; init; }
    public RippleState Ripple { get; init; } = new();

    private MediaTime _oldStart;
    private MediaTime _oldDuration;
    private readonly List<(TimelineItem Item, MediaTime OldStart)> _rippledItems = [];

    public EditResult Execute(Sequence sequence)
    {
        var track = sequence.Tracks.FirstOrDefault(t => t.Id == TrackId);
        if (track == null) return EditResult.Fail("Track not found");

        var item = track.Items.FirstOrDefault(i => i.Id == ItemId);
        if (item == null) return EditResult.Fail("Item not found");

        _oldStart = item.TimelineStart;
        _oldDuration = item.Duration;

        if (NewStart.HasValue)
        {
            if (NewStart.Value.Seconds < 0)
                return EditResult.Fail("Trim start cannot be negative");
            item.TimelineStart = NewStart.Value;
        }

        if (NewDuration.HasValue)
        {
            if (NewDuration.Value.Seconds <= 0)
                return EditResult.Fail("Duration must be greater than zero");

            if (Ripple.Enabled)
            {
                var gapEnd = _oldStart.Add(_oldDuration);
                var delta = _oldDuration.Subtract(NewDuration.Value);
                foreach (var rippled in track.Items.Where(i => i.TimelineStart >= gapEnd).OrderBy(i => i.TimelineStart.Seconds))
                {
                    _rippledItems.Add((rippled, rippled.TimelineStart));
                    rippled.TimelineStart = rippled.TimelineStart.Subtract(delta);
                }
            }

            item.Duration = NewDuration.Value;
        }

        return EditResult.Ok();
    }

    public EditResult Undo(Sequence sequence)
    {
        var track = sequence.Tracks.FirstOrDefault(t => t.Id == TrackId);
        if (track == null) return EditResult.Fail("Track not found");

        foreach (var (rippled, oldStart) in _rippledItems)
            rippled.TimelineStart = oldStart;
        _rippledItems.Clear();

        var item = track.Items.FirstOrDefault(i => i.Id == ItemId);
        if (item == null) return EditResult.Fail("Item not found");

        item.TimelineStart = _oldStart;
        item.Duration = _oldDuration;
        return EditResult.Ok();
    }
}
