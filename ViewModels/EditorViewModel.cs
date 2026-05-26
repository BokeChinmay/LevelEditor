using System.Windows;
using System.IO;
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
    private const int MaxRecentFiles = 8;
    private readonly string _recentFilesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LevelEditor", "recent.json");

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
    [ObservableProperty] private ObservableCollection<EntityProperty> _entityProperties = new();
    [ObservableProperty] private ObservableCollection<string> _recentFiles = new();

    public ObservableCollection<TileDefinition> TileDefinitions { get; } = new();
    public ObservableCollection<EntityDefinition> EntityDefinitions { get; } = new();
    public List<string> TileCategories => TileDefinitions
        .Select(t => t.Category).Distinct().ToList();

    public bool CanUndo => _history.CanUndo;
    public bool CanRedo => _history.CanRedo;

    public string ZoomText => $"Zoom: {Zoom:P0}";

    public string SelectedTileName => IsEntityMode 
        ? (SelectedEntity?.Name ?? "None")
        : (SelectedTile?.Name ?? "None");
    
    public string SelectedTileColor => IsEntityMode 
        ? (SelectedEntity?.Color ?? "#808080")
        : (SelectedTile?.Color ?? "#808080");

    public Visibility TilePaletteVisibility => 
        IsEntityMode ? Visibility.Collapsed : Visibility.Visible;

    public TileDefinition? HighlightedTile => SelectedTile;
    public TileLayer? HighlightedLayer => ActiveLayer;
    
    public Visibility EntityPaletteVisibility => 
        IsEntityMode ? Visibility.Visible : Visibility.Collapsed;
    
    public Visibility EntityPropertiesVisibility => 
        SelectedEntityPlacement != null ? Visibility.Visible : Visibility.Collapsed;

    public List<KeyValuePair<string, string>> SelectedEntityProperties => 
        SelectedEntityPlacement?.Properties.Select(kvp => kvp).ToList() ?? new();

    public event Action? LevelChanged;

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

        StatusText = "Ready | Left click: paint | Shift+click: fill | Right drag: pan | Scroll: zoom | 1-3: layers | E: eraser";

        LoadRecentFiles();
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
        NotifyLevelChanged();
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
        NotifyLevelChanged();
    }

    public void FloodFill(int startX, int startY)
    {
        if (ActiveLayer == null || SelectedTile == null) return;
        if (SelectedTile.Id == "eraser") return;
        if (startX < 0 || startX >= Level.Width ||
            startY < 0 || startY >= Level.Height) return;

        var targetTileId = ActiveLayer.GetTile(startX, startY)?.TileId ?? "";
        var fillTileId = SelectedTile.Id;

        if (targetTileId == fillTileId) return;

        var commands = new List<LevelEditor.Commands.IEditorCommand>();
        var visited = new HashSet<string>();
        var queue = new Queue<(int x, int y)>();
        queue.Enqueue((startX, startY));

        while (queue.Count > 0) {
            var (x, y) = queue.Dequeue();
            var key = $"{x},{y}";

            if (visited.Contains(key)) continue;
            if (x < 0 || x >= Level.Width || y < 0 || y >= Level.Height) continue;

            var currentId = ActiveLayer.GetTile(x, y)?.TileId ?? "";
            if (currentId != targetTileId) continue;

            visited.Add(key);
            var placement = new TilePlacement { TileId = fillTileId };
            commands.Add(new LevelEditor.Commands.PlaceTileCommand(
                ActiveLayer, x, y, placement));

            queue.Enqueue((x + 1, y));
            queue.Enqueue((x - 1, y));
            queue.Enqueue((x, y + 1));
            queue.Enqueue((x, y - 1));
        }

        if (commands.Count == 0) return;

        var batch = new LevelEditor.Commands.BatchCommand(
            commands, $"Flood fill with {fillTileId}");
        _history.Execute(batch);
        NotifyLevelChanged();
        StatusText = $"Filled {commands.Count} tiles with {SelectedTile.Name}";
    }

    public void ExecuteCommand(LevelEditor.Commands.IEditorCommand command) 
    {
        _history.Execute(command);
        NotifyLevelChanged();
    }

    [RelayCommand]
    private void Undo()
    {
        _history.Undo();
        StatusText = $"Undid: {_history.NextUndoDescription ?? "nothing"}";
        NotifyLevelChanged();
    }

    [RelayCommand]
    private void Redo()
    {
        _history.Redo();
        StatusText = $"Redid action";
        NotifyLevelChanged();
    }

    [RelayCommand]
    private void NewLevel()
    {
        if (IsDirty) {
            var result = System.Windows.MessageBox.Show(
                "You have unsaved changes. Create new level anyway?",
                "Unsaved Changes",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result != System.Windows.MessageBoxResult.Yes) return;
        }

        Level = LevelDocument.CreateDefault();
        ActiveLayer = Level.Layers.First();
        _history.Clear();
        _currentFilePath = null;
        IsDirty = false;
        UpdateWindowTitle();
        StatusText = "New level created";
        NotifyLevelChanged();
    }

    [RelayCommand]
    private void SaveLevel()
    {
        if (_currentFilePath == null) { SaveLevelAs(); return; }
        _serializer.Save(Level, _currentFilePath);
        IsDirty = false;
        UpdateWindowTitle();
        StatusText = $"Saved to {_currentFilePath}";
        AddToRecentFiles(_currentFilePath);
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
        AddToRecentFiles(_currentFilePath);
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

    [RelayCommand]
    private void OpenRecentFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            System.Windows.MessageBox.Show(
                $"File not found:\n{filePath}",
                "File Not Found",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            RecentFiles.Remove(filePath);
            SaveRecentFiles();
            return;
        }

        _currentFilePath = filePath;
        Level = _serializer.Load(filePath);
        ActiveLayer = Level.Layers.FirstOrDefault();
        _history.Clear();
        IsDirty = false;
        AddToRecentFiles(filePath);
        UpdateWindowTitle();
        StatusText = $"Opened {filePath}";
        NotifyLevelChanged();
    }

    [RelayCommand]
    private void ResizeLevel()
    {
        // Clamp to reasonable bounds
        Level.Width = Math.Clamp(Level.Width, 5, 200);
        Level.Height = Math.Clamp(Level.Height, 5, 200);

        // Remove any tiles outside the new bounds
        foreach (var layer in Level.Layers)
        {
            var keysToRemove = layer.Tiles.Keys
                .Where(k =>
                {
                    var parts = k.Split(',');
                    if (parts.Length != 2) return true;
                    return !int.TryParse(parts[0], out var x) ||
                        !int.TryParse(parts[1], out var y) ||
                        x >= Level.Width || y >= Level.Height;
                }).ToList();

            foreach (var key in keysToRemove)
                layer.Tiles.Remove(key);
        }

        // Remove entities outside bounds
        Level.Entities.RemoveAll(e =>
            e.GridX >= Level.Width || e.GridY >= Level.Height);

        IsDirty = true;
        NotifyLevelChanged();
        StatusText = $"Level resized to {Level.Width}x{Level.Height}";
    }

    private void UpdateWindowTitle()
    {
        WindowTitle = $"Level Editor - {Level.Name}{(IsDirty ? " *" : "")}";
    }

    private void NotifyLevelChanged() => LevelChanged?.Invoke();

    private void LoadRecentFiles()
    {
        try
        {
            if (!File.Exists(_recentFilesPath)) return;
            var json = File.ReadAllText(_recentFilesPath);
            var files = System.Text.Json.JsonSerializer
                .Deserialize<List<string>>(json) ?? new();

            RecentFiles.Clear();
            foreach (var f in files.Where(File.Exists).Take(MaxRecentFiles))
                RecentFiles.Add(f);
        }
        catch { }
    }

    private void SaveRecentFiles()
    {
        try
        {
            var dir = Path.GetDirectoryName(_recentFilesPath)!;
            Directory.CreateDirectory(dir);
            var json = System.Text.Json.JsonSerializer.Serialize(
                RecentFiles.ToList());
            File.WriteAllText(_recentFilesPath, json);
        }
        catch { }
    }

    private void AddToRecentFiles(string filePath)
    {
        RecentFiles.Remove(filePath);
        RecentFiles.Insert(0, filePath);
        while (RecentFiles.Count > MaxRecentFiles)
            RecentFiles.RemoveAt(RecentFiles.Count - 1);
        SaveRecentFiles();
    }

    partial void OnIsEntityModeChanged(bool value) {
        OnPropertyChanged(nameof(TilePaletteVisibility));
        OnPropertyChanged(nameof(EntityPaletteVisibility));
        OnPropertyChanged(nameof(SelectedTileName));
        OnPropertyChanged(nameof(SelectedTileColor));
    }

    partial void OnZoomChanged(double value) {
        OnPropertyChanged(nameof(ZoomText));
    }

    partial void OnSelectedTileChanged(TileDefinition? value) {
        foreach (var tile in TileDefinitions)
            tile.IsSelected = tile == value;
        OnPropertyChanged(nameof(SelectedTileName));
        OnPropertyChanged(nameof(SelectedTileColor));
    }

    partial void OnSelectedEntityChanged(EntityDefinition? value) {
        foreach (var entity in EntityDefinitions) 
            entity.IsSelected = entity == value;
        OnPropertyChanged(nameof(SelectedTileName));
        OnPropertyChanged(nameof(SelectedTileColor));
    }

    partial void OnSelectedEntityPlacementChanged(EntityPlacement? value) {
        OnPropertyChanged(nameof(EntityPropertiesVisibility));

        EntityProperties.Clear();
        if(value == null) return;

        foreach (var kvp in value.Properties) {
            var prop = new EntityProperty(kvp.Key, kvp.Value);
            prop.PropertyChanged += (s, e) => {
                if (s is EntityProperty ep) 
                    value.Properties[ep.Key] = ep.Value;
            };
            EntityProperties.Add(prop);
        }
    }
}