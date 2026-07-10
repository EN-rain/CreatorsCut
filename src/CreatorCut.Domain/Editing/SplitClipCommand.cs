namespace CreatorCut.Domain.Editing;

public sealed class SplitClipCommand : IEditCommand
{
    public string Description => $"Split clip {ItemId} at {SplitTime.Seconds:F2}s";

    public required TrackId TrackId { get; init; }
    public required TimelineItemId ItemId { get; init; }
    public required MediaTime SplitTime { get; init; }

    private MediaTime _originalDuration;
    private readonly List<TimelineItem> _addedItems = [];

    public EditResult Execute(Sequence sequence)
    {
        var track = sequence.Tracks.FirstOrDefault(t => t.Id == TrackId);
        if (track == null)
            return EditResult.Fail("Track not found");

        var index = track.Items.FindIndex(i => i.Id == ItemId);
        if (index < 0)
            return EditResult.Fail("Item not found");

        var item = track.Items[index];
        if (SplitTime <= item.TimelineStart || SplitTime >= item.TimelineStart.Add(item.Duration))
            return EditResult.Fail("Split time is outside item bounds");

        _originalDuration = item.Duration;
        var offset = SplitTime.Subtract(item.TimelineStart);
        var remaining = item.Duration.Subtract(offset);

        var right = new TimelineItem
        {
            Kind = item.Kind,
            MediaAssetId = item.MediaAssetId,
            TimelineStart = SplitTime,
            Duration = remaining,
            SourceStart = item.SourceStart.Add(offset),
            SourceDuration = item.SourceDuration.Subtract(offset),
            Speed = item.Speed,
            Volume = item.Volume,
            Transform = new Transform2D
            {
                PositionX = item.Transform.PositionX,
                PositionY = item.Transform.PositionY,
                ScaleX = item.Transform.ScaleX,
                ScaleY = item.Transform.ScaleY,
                RotationDegrees = item.Transform.RotationDegrees,
            },
        };

        item.Duration = offset;
        track.Items.Insert(index + 1, right);
        _addedItems.Add(right);

        return EditResult.Ok();
    }

    public EditResult Undo(Sequence sequence)
    {
        var track = sequence.Tracks.FirstOrDefault(t => t.Id == TrackId);
        if (track == null) return EditResult.Fail("Track not found");

        foreach (var added in _addedItems)
            track.Items.Remove(added);
        _addedItems.Clear();

        var orig = track.Items.FirstOrDefault(i => i.Id == ItemId);
        if (orig != null)
            orig.Duration = _originalDuration;

        return EditResult.Ok();
    }
}
