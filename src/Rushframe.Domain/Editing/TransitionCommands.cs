namespace Rushframe.Domain.Editing;

public sealed class ApplyTransitionCommand : IEditCommand
{
    public string Description => "Apply transition";

    public required TimelineItemId LeftItemId { get; init; }
    public required TimelineItemId RightItemId { get; init; }
    public TransitionKind Kind { get; init; } = TransitionKind.CrossDissolve;

    private Transition? _applied;

    public EditResult Execute(Sequence sequence)
    {
        foreach (var track in sequence.Tracks)
        {
            var left = track.Items.FirstOrDefault(i => i.Id == LeftItemId);
            var right = track.Items.FirstOrDefault(i => i.Id == RightItemId);
            if (left == null || right == null) continue;

            var overlap = left.TimelineEnd.Seconds - right.TimelineStart.Seconds;
            if (overlap > 0) return EditResult.Fail("Clips already overlap; cannot apply transition");

            _applied = new Transition
            {
                LeftItemId = LeftItemId,
                RightItemId = RightItemId,
                Kind = Kind,
                Duration = MediaTime.FromSeconds(Math.Min(1.0, right.Duration.Seconds / 2)),
            };

            return EditResult.Ok();
        }

        return EditResult.Fail("Items not found on same track");
    }

    public EditResult Undo(Sequence sequence)
    {
        _applied = null;
        return EditResult.Ok();
    }
}
