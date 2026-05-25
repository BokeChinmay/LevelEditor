namespace LevelEditor.Models;

public static class TilePalette
{
    public static List<TileDefinition> DefaultTiles => new()
    {
        // Ground tiles
        new() { Id = "grass", Name = "Grass", Category = "Ground", Color = "#4CAF50", IsSolid = true  },
        new() { Id = "dirt",  Name = "Dirt",  Category = "Ground", Color = "#8D6E63", IsSolid = true  },
        new() { Id = "stone", Name = "Stone", Category = "Ground", Color = "#9E9E9E", IsSolid = true  },
        new() { Id = "sand",  Name = "Sand",  Category = "Ground", Color = "#F9A825", IsSolid = true  },
        new() { Id = "snow",  Name = "Snow",  Category = "Ground", Color = "#E3F2FD", IsSolid = true  },

        // Platform tiles
        new() { Id = "platform",     Name = "Platform",     Category = "Platform", Color = "#795548", IsSolid = true, IsOneWay = true },
        new() { Id = "ice_platform", Name = "Ice Platform", Category = "Platform", Color = "#B3E5FC", IsSolid = true, IsOneWay = true },

        // Hazard tiles
        new() { Id = "spike", Name = "Spike", Category = "Hazard", Color = "#F44336", IsSolid = false },
        new() { Id = "lava",  Name = "Lava",  Category = "Hazard", Color = "#FF5722", IsSolid = false },

        // Decoration tiles
        new() { Id = "bush",   Name = "Bush",   Category = "Decoration", Color = "#388E3C", IsSolid = false },
        new() { Id = "flower", Name = "Flower", Category = "Decoration", Color = "#E91E63", IsSolid = false },
        new() { Id = "torch",  Name = "Torch",  Category = "Decoration", Color = "#FFC107", IsSolid = false },

        // Special tiles
        new() { Id = "water",  Name = "Water",  Category = "Special", Color = "#2196F3", IsSolid = false },
        new() { Id = "ladder", Name = "Ladder", Category = "Special", Color = "#FF9800", IsSolid = false },
        new() { Id = "eraser", Name = "Eraser", Category = "Eraser",  Color = "#FFFFFF", IsSolid = false },
    };

    public static List<EntityDefinition> DefaultEntities => new()
    {
        new() { Id = "player_spawn", Name = "Player Spawn", Category = "Core",    Color = "#00BCD4", Properties = new() },

        new() { Id = "enemy_basic",  Name = "Basic Enemy",  Category = "Enemy",   Color = "#F44336",Properties = new() {
                new() { Name = "patrol_distance", Type = "int",    DefaultValue = "3" },
                new() { Name = "speed",           Type = "float",  DefaultValue = "2.0" },
                new() { Name = "facing",          Type = "string", DefaultValue = "right" }
            }},

        new() { Id = "enemy_flying", Name = "Flying Enemy", Category = "Enemy",   Color = "#E91E63", Properties = new() {
                new() { Name = "patrol_distance", Type = "int",    DefaultValue = "5" },
                new() { Name = "speed",           Type = "float",  DefaultValue = "3.0" }
            }},

        new() { Id = "coin",         Name = "Coin",         Category = "Pickup",  Color = "#FFC107", Properties = new() {
                new() { Name = "value",           Type = "int",    DefaultValue = "1" }
            }},

        new() { Id = "health_pack",  Name = "Health Pack",  Category = "Pickup",  Color = "#4CAF50", Properties = new() {
                new() { Name = "heal_amount",     Type = "int",    DefaultValue = "25" }
            }},

        new() { Id = "checkpoint",   Name = "Checkpoint",   Category = "Special", Color = "#9C27B0", Properties = new() {
                new() { Name = "checkpoint_id",   Type = "int",    DefaultValue = "0" }
            }},

        new() { Id = "level_exit",   Name = "Level Exit",   Category = "Special", Color = "#FF9800", Properties = new() {
                new() { Name = "target_level",    Type = "string", DefaultValue = "level_02" }
            }},

        new() { Id = "trigger_zone", Name = "Trigger Zone", Category = "Trigger", Color = "#607D8B", Properties = new()
            {
                new() { Name = "trigger_id",      Type = "string", DefaultValue = "" },
                new() { Name = "trigger_once",    Type = "bool",   DefaultValue = "true" }
            }},
    };
}