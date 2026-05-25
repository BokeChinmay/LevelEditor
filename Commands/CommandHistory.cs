namespace LevelEditor.Commands;

public class CommandHistory {
    private readonly Stack<IEditorCommand> _undoStack = new();
    private readonly Stack<IEditorCommand> _redoStack = new();

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;
    public string? NextUndoDescription => CanUndo ? _undoStack.Peek().Description : null;
    public string? NextRedoDescription => CanRedo ? _redoStack.Peek().Description : null;

    public event Action? HistoryChanged;

    public void Execute(IEditorCommand command) {
        command.Execute();
        _undoStack.Push(command);
        _redoStack.Clear(); // New action clears redo history
        HistoryChanged?.Invoke();
    }

    public void Undo() {
        if (!CanUndo) return;
        var command = _undoStack.Pop();
        command.Undo();
        _redoStack.Push(command);
        HistoryChanged?.Invoke();
    }

    public void Redo() {
        if(!CanRedo) return;
        var command = _redoStack.Pop();
        command.Execute();
        _undoStack.Push(command);
        HistoryChanged?.Invoke();
    }

    public void Clear() {
        _undoStack.Clear();
        _redoStack.Clear();
        HistoryChanged?.Invoke();
    }
}