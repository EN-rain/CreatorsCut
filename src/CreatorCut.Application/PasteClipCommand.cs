using CreatorCut.Domain;
using CreatorCut.Domain.Editing;

namespace CreatorCut.Application;

public sealed class PasteClipCommand : IEditCommand
{
    public string Description => "Paste clip";

    public required TrackId TrackId { get; init; }
    public required MediaTime TimelineStart { get; init; }
    public required CopyClipCommand CopyCommand { get; init; }

    private TimelineItemId _pastedItemId;

    public EditResult Execute(Sequence sequence)
    {
        if (CopyCommand.Clipboard == null)
            return EditResult.Fail("Nothing to paste");

        var track = sequence.Tracks.FirstOrDefault(t => t.Id == TrackId);
        if (track == null)
            return EditResult.Fail(new TrackNotFoundError(TrackId));

        var paste = new TimelineItem
        {
            Kind = CopyCommand.Clipboard.Kind,
            MediaAssetId = CopyCommand.Clipboard.MediaAssetId,
            TimelineStart = TimelineStart,
            Duration = CopyCommand.Clipboard.Duration,
            SourceStart = CopyCommand.Clipboard.SourceStart,
            SourceDuration = CopyCommand.Clipboard.SourceDuration,
            Speed = CopyCommand.Clipboard.Speed,
            Volume = CopyCommand.Clipboard.Volume,
            Opacity = CopyCommand.Clipboard.Opacity,
            Transform = new Transform2D
            {
                PositionX = CopyCommand.Clipboard.Transform.PositionX,
                PositionY = CopyCommand.Clipboard.Transform.PositionY,
                ScaleX = CopyCommand.Clipboard.Transform.ScaleX,
                ScaleY = CopyCommand.Clipboard.Transform.ScaleY,
                RotationDegrees = CopyCommand.Clipboard.Transform.RotationDegrees,
            },
        };

        _pastedItemId = paste.Id;
        track.Items.Add(paste);
        return EditResult.Ok();
    }

    public EditResult Undo(Sequence sequence)
    {
        foreach (var track in sequence.Tracks)
        {
            var removed = track.Items.RemoveAll(i => i.Id == _pastedItemId);
            if (removed > 0) return EditResult.Ok();
        }
        return EditResult.Fail("Pasted item not found");
    }
}
