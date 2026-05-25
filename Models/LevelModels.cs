using System.ComponentModel;

namespace LevelEditor.Models;

public class LevelDocument {
    public string Name { get; set; } = "Untitled Level";
    public int Width { get; set; } = 20;
    public int Height { get; set; } = 15;
    public int TileSize { get; set; } = 32;
    public List<TileLayer> Layers { get; set; } = new();
    public List<EntityPlacement> Entities { get; set; } = new();
    public LevelMetadata Metadata { get; set; } = new();

    public static LevelDocument CreateDefault() {
        var doc = new LevelDocument();
        doc.Layers.Add(new TileLayer { Name = "Ground", ZIndex = 0 });
        doc.Layers.Add(new TileLayer { Name = "Collision", ZIndex = 1 });
        doc.Layers.Add(new TileLayer { Name = "Decoration", ZIndex = 2 });
        return doc;
    }
}

public class TileLayer {
    public string Name { get; set; } = "";
    public int ZIndex { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool IsLocked { get; set; } = false;
    public Dictionary<string, TilePlacement> Tiles { get; set; } = new();

    public TilePlacement? GetTile(int x, int y) => Tiles.TryGetValue($"{x},{y}", out var t) ? t : null;

    public void SetTile(int x, int y, TilePlacement? tile) {
        var key = $"{x},{y}";
        if (tile == null) Tiles.Remove(key);
        else Tiles[key] = tile;
    }
}

public class TilePlacement {
    public string TileId { get; set; } = "";
    public int VariantIndex { get; set; } = 0;
    public bool FlipX { get; set; } = false;
    public bool FlipY { get; set; } = false;
}

public class EntityPlacement {
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string EntityType { get; set; } = "";
    public int GridX { get; set; } 
    public int GridY { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
}

public class LevelMetadata {
    public string Author { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    public string GameVersion { get; set; } = "1.0";
}

public class TileDefinition : INotifyPropertyChanged{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Color { get; set; } = "#808080";
    public bool IsSolid { get; set; } = false;
    public bool IsOneWay { get; set; } = false;

    private bool _isSelected = false;
    public bool IsSelected {
        get => _isSelected;
        set {
            if (_isSelected == value) return;
            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public class EntityDefinition {
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Color { get; set; } = "#FF0000";
    public List<PropertyDefinition> Properties { get; set; } = new();
}

public class PropertyDefinition {
    public string Name { get; set; } = "";
    public string Type { get; set; } = "string";
    public string DefaultValue { get; set; } = "";
}