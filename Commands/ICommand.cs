namespace LevelEditor.Commands;

public interface IEditorCommand
{
    string Description { get; }
    void Execute();
    void Undo();
}