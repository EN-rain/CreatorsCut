namespace CreatorCut.Domain.Editing;

public sealed class UndoRedoStack
{
    private readonly LinkedList<IEditCommand> _undoStack = [];
    private readonly LinkedList<IEditCommand> _redoStack = [];
    private const int MaxUndoDepth = 100;

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public EditResult Execute(Sequence sequence, IEditCommand command)
    {
        var result = command.Execute(sequence);
        if (result.Success)
        {
            _undoStack.AddLast(command);
            if (_undoStack.Count > MaxUndoDepth)
                _undoStack.RemoveFirst();
            _redoStack.Clear();
        }
        return result;
    }

    public EditResult Undo(Sequence sequence)
    {
        if (_undoStack.Count == 0)
            return EditResult.Fail("Nothing to undo");

        var command = _undoStack.Last!.Value;
        _undoStack.RemoveLast();

        var result = command.Undo(sequence);
        if (result.Success)
            _redoStack.AddLast(command);

        return result;
    }

    public EditResult Redo(Sequence sequence)
    {
        if (_redoStack.Count == 0)
            return EditResult.Fail("Nothing to redo");

        var command = _redoStack.Last!.Value;
        _redoStack.RemoveLast();

        var result = command.Execute(sequence);
        if (result.Success)
            _undoStack.AddLast(command);

        return result;
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }
}
