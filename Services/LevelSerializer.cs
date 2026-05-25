using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using LevelEditor.Models;

namespace LevelEditor.Services;

public class LevelSerializer {
    private static readonly JsonSerializerOptions Options = new() {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public void Save(LevelDocument level, string filePath) {
        level.Metadata.ModifiedAt = DateTime.UtcNow;
        var json = JsonSerializer.Serialize(level, Options);
        File.WriteAllText(filePath, json);
    }

    public LevelDocument Load(string filePath) {
        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<LevelDocument>(json, Options)
            ?? throw new InvalidDataException("Failed to deserialize level file.");
    }

    public void ExportForUnity(LevelDocument level, string filePath) {
        var export = new
        {
            name = level.Name,
            width = level.Width,
            height = level.Height,
            tileSize = level.TileSize,
            layers = level.Layers.Select(l => new
            {
                name = l.Name,
                zIndex = l.ZIndex,
                tiles = l.Tiles.Select(kvp => new
                {
                    key = kvp.Key,
                    tileId = kvp.Value.TileId,
                    flipX = kvp.Value.FlipX,
                    flipY = kvp.Value.FlipY
                }).ToList()
            }).ToList(),
            entities = level.Entities.Select(e => new
            {
                id = e.Id,
                type = e.EntityType,
                x = e.GridX,
                y = e.GridY,
                properties = e.Properties
            }).ToList()
        };

        var json = JsonSerializer.Serialize(export, Options);
        File.WriteAllText(filePath, json);
    }
}