using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LevelEditor.Commands;
using LevelEditor.Models;
using LevelEditor.Services;

namespace LevelEditor.ViewModels;

public partial class EditorViewModel : ObservableObject
{
    private readonly LevelSerializer _serializer = new();
    private readonly CommandHistory _history = new();
    private string? _currentFilePath;

    [ObservableProperty] private LevelDocument _level = LevelDocument.CreateDefault();
    [ObservableProperty] private TileLayer? _activeLayer;
    [ObservableProperty] private TileDefinition? _selectedTile;
    [ObservableProperty] private EntityDefinition? _selectedEntity;
    [ObservableProperty] private EntityPlacement? _selectedEntityPlacement;
    [ObservableProperty] private bool _isEntityMode = false;
    [ObservableProperty] private double _zoom = 1.0;
    [ObservableProperty] private double _offsetX = 0;
    [ObservableProperty] private double _offsetY = 0;
    [ObservableProperty] private string _statusText = "Ready";
    [ObservableProperty] private bool _isDirty = false;
    [ObservableProperty] private string _windowTitle = "Level Editor — Untitled";

    public ObservableCollection<TileDefinition> TileDefinitions { get; } = new();
    public ObservableCollection<EntityDefinition> EntityDefinitions { get; } = new();
    public List<string> TileCategories => TileDefinitions
        .Select(t => t.Category).Distinct().ToList();

    public bool CanUndo => _history.CanUndo;
    public bool CanRedo => _history.CanRedo;

    public EditorViewModel()
    {
        foreach (var tile in TilePalette.DefaultTiles)
            TileDefinitions.Add(tile);

        foreach (var entity in TilePalette.DefaultEntities)
            EntityDefinitions.Add(entity);

        ActiveLayer = Level.Layers.First();

        _history.HistoryChanged += () =>
        {
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
            IsDirty = true;
            UpdateWindowTitle();
        };

        SelectedTile = TileDefinitions.First();
    }

    public void PaintTile(int gridX, int gridY)
    {
        if (ActiveLayer == null || ActiveLayer.IsLocked) return;
        if (gridX < 0 || gridX >= Level.Width || gridY < 0 || gridY >= Level.Height) return;

        if (SelectedTile?.Id == "eraser")
        {
            var cmd = new PlaceTileCommand(ActiveLayer, gridX, gridY, null);
            _history.Execute(cmd);
        }
        else if (SelectedTile != null)
        {
            var placement = new TilePlacement { TileId = SelectedTile.Id };
            var cmd = new PlaceTileCommand(ActiveLayer, gridX, gridY, placement);
            _history.Execute(cmd);
        }

        StatusText = $"Painted {SelectedTile?.Name} at ({gridX}, {gridY})";
    }

    public void PlaceEntity(int gridX, int gridY)
    {
        if (SelectedEntity == null) return;
        if (gridX < 0 || gridX >= Level.Width || gridY < 0 || gridY >= Level.Height) return;

        var entity = new EntityPlacement
        {
            EntityType = SelectedEntity.Id,
            GridX = gridX,
            GridY = gridY,
            Properties = SelectedEntity.Properties
                .ToDictionary(p => p.Name, p => p.DefaultValue)
        };

        var cmd = new PlaceEntityCommand(Level.Entities, entity);
        _history.Execute(cmd);
        StatusText = $"Placed {SelectedEntity.Name} at ({gridX}, {gridY})";
    }

    [RelayCommand]
    private void Undo()
    {
        _history.Undo();
        StatusText = $"Undid: {_history.NextUndoDescription ?? "nothing"}";
    }

    [RelayCommand]
    private void Redo()
    {
        _history.Redo();
        StatusText = $"Redid action";
    }

    [RelayCommand]
    private void NewLevel()
    {
        Level = LevelDocument.CreateDefault();
        ActiveLayer = Level.Layers.First();
        _history.Clear();
        _currentFilePath = null;
        IsDirty = false;
        UpdateWindowTitle();
        StatusText = "New level created";
    }

    [RelayCommand]
    private void SaveLevel()
    {
        if (_currentFilePath == null) { SaveLevelAs(); return; }
        _serializer.Save(Level, _currentFilePath);
        IsDirty = false;
        UpdateWindowTitle();
        StatusText = $"Saved to {_currentFilePath}";
    }

    [RelayCommand]
    private void SaveLevelAs()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Level Files (*.level)|*.level|All Files (*.*)|*.*",
            DefaultExt = ".level",
            FileName = Level.Name
        };

        if (dialog.ShowDialog() != true) return;
        _currentFilePath = dialog.FileName;
        _serializer.Save(Level, _currentFilePath);
        IsDirty = false;
        UpdateWindowTitle();
        StatusText = $"Saved to {_currentFilePath}";
    }

    [RelayCommand]
    private void OpenLevel()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Level Files (*.level)|*.level|All Files (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true) return;
        _currentFilePath = dialog.FileName;
        Level = _serializer.Load(_currentFilePath);
        ActiveLayer = Level.Layers.FirstOrDefault();
        _history.Clear();
        IsDirty = false;
        UpdateWindowTitle();
        StatusText = $"Opened {_currentFilePath}";
    }

    [RelayCommand]
    private void ExportForUnity()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "JSON Files (*.json)|*.json",
            DefaultExt = ".json",
            FileName = Level.Name + "_export"
        };

        if (dialog.ShowDialog() != true) return;
        _serializer.ExportForUnity(Level, dialog.FileName);
        StatusText = $"Exported for Unity to {dialog.FileName}";
    }

    private void UpdateWindowTitle()
    {
        WindowTitle = $"Level Editor - {Level.Name}{(IsDirty ? " *" : "")}";
    }
}