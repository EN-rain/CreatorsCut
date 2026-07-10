using CreatorCut.Domain;
using CreatorCut.Domain.Editing;

namespace CreatorCut.Application;

public sealed class CopyClipCommand : IEditCommand
{
    public string Description => $"Copy clip {ItemId}";

    public required TimelineItemId ItemId { get; init; }

    public TimelineItem? Clipboard { get; private set; }

    public EditResult Execute(Sequence sequence)
    {
        foreach (var track in sequence.Tracks)
        {
            var idx = track.Items.FindIndex(i => i.Id == ItemId);
            if (idx < 0) continue;

            var original = track.Items[idx];
            Clipboard = new TimelineItem
            {
                Kind = original.Kind,
                MediaAssetId = original.MediaAssetId,
                TimelineStart = original.TimelineStart,
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

            return EditResult.Ok();
        }

        return EditResult.Fail(new ItemNotFoundError(ItemId));
    }

    public EditResult Undo(Sequence sequence)
    {
        Clipboard = null;
        return EditResult.Ok();
    }
}
