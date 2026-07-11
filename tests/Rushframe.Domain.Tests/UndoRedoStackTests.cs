namespace Rushframe.Domain.Tests;

public sealed class UndoRedoStackTests
{
    [Fact]
    public void execute_then_undo_restores_original()
    {
        var (stack, seq, itemId) = MakeWithOneSplit();

        var undoResult = stack.Undo(seq);
        Assert.True(undoResult.Success);
        Assert.Single(seq.Tracks[0].Items);
        Assert.Equal(10, seq.Tracks[0].Items[0].Duration.Seconds, 3);
    }

    [Fact]
    public void undo_then_redo_works()
    {
        var (stack, seq, _) = MakeWithOneSplit();

        stack.Undo(seq);
        var redoResult = stack.Redo(seq);

        Assert.True(redoResult.Success);
        Assert.Equal(2, seq.Tracks[0].Items.Count);
    }

    [Fact]
    public void new_command_clears_redo_stack()
    {
        var (stack, seq, itemId) = MakeWithOneSplit();

        stack.Undo(seq);
        Assert.True(stack.CanRedo);

        // execute new command
        var trim = new Editing.TrimClipCommand
        {
            TrackId = seq.Tracks[0].Id,
            ItemId = itemId,
            NewDuration = MediaTime.FromSeconds(5),
        };
        stack.Execute(seq, trim);

        Assert.False(stack.CanRedo);
    }

    [Fact]
    public void undo_empty_returns_error()
    {
        var stack = new Editing.UndoRedoStack();
        var seq = new Sequence();

        var result = stack.Undo(seq);
        Assert.False(result.Success);
    }

    private static (Editing.UndoRedoStack stack, Sequence seq, TimelineItemId itemId) MakeWithOneSplit()
    {
        var stack = new Editing.UndoRedoStack();
        var seq = new Sequence();
        var track = new Track { Kind = TrackKind.Video };
        var item = new TimelineItem { Duration = MediaTime.FromSeconds(10) };
        track.Items.Add(item);
        seq.Tracks.Add(track);

        var split = new Editing.SplitClipCommand
        {
            TrackId = track.Id,
            ItemId = item.Id,
            SplitTime = MediaTime.FromSeconds(5),
        };
        stack.Execute(seq, split);

        return (stack, seq, item.Id);
    }
}
