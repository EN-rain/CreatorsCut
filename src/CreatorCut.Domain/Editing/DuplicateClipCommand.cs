namespace CreatorCut.Domain.Editing;

public sealed class DuplicateClipCommand : IEditCommand
{
    public string Description => $"Duplicate clip {ItemId}";

    public required TimelineItemId ItemId { get; init; }

    private TimelineItem? _duplicate;

    public EditResult Execute(Sequence sequence)
    {
        foreach (var track in sequence.Tracks)
        {
            var idx = track.Items.FindIndex(i => i.Id == ItemId);
            if (idx < 0) continue;

            var original = track.Items[idx];
            _duplicate = new TimelineItem
            {
                Kind = original.Kind,
                MediaAssetId = original.MediaAssetId,
                TimelineStart = original.TimelineStart.Add(original.Duration),
                Duration = original.Duration,
                SourceStart = original.SourceStart,
                SourceDuration = original.SourceDuration,
                Speed = original.Speed,
                Volume = original.Volume,
                Opacity = original.Opacity,
                Transform = new Transform2D
                {
                    PositionX = original.Transform.PositionX,
                    PositionY = original.Transform.PositionY,
                    ScaleX = original.Transform.ScaleX,
                    ScaleY = original.Transform.ScaleY,
                    RotationDegrees = original.Transform.RotationDegrees,
                },
            };

            track.Items.Insert(idx + 1, _duplicate);
            return EditResult.Ok();
        }

        return EditResult.Fail("Item not found");
    }

    public EditResult Undo(Sequence sequence)
    {
        if (_duplicate == null) return EditResult.Fail("Nothing to undo");

        foreach (var track in sequence.Tracks)
        {
            if (track.Items.Remove(_duplicate))
            {
                _duplicate = null;
                return EditResult.Ok();
            }
        }

        return EditResult.Fail("Duplicate not found");
    }
}
