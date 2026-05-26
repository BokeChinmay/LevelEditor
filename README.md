# Level Editor

A standalone 2D tile-based level editor built in C# and WPF. Design game levels 
visually, place entities with configurable properties, and export to JSON for 
use in Unity or any custom game engine.

## Features

- **Tile painting** - click or drag to paint tiles across multiple layers
- **Flood fill** - Shift+click to fill a contiguous region instantly
- **Entity placement** - place enemies, pickups, triggers, and spawn points
  with configurable per-entity properties
- **Layer system** - three independent layers (Ground, Collision, Decoration)
  with per-layer visibility toggle
- **Undo / Redo** - full command history via the Command design pattern;
  every action including flood fills is undoable in one step
- **Save / Load** - native `.level` format preserves all layer and entity data
- **Unity export** - exports a flat JSON format consumable by Unity's
  `JsonUtility` or Newtonsoft.Json
- **Recent files** - persisted across sessions via AppData
- **Unsaved changes protection** - prompts on close or new level

## Tech Stack

| Layer | Technology |
|---|---|
| Language | C# 13 / .NET 9 |
| UI Framework | WPF (Windows Presentation Foundation) |
| Architecture | MVVM — CommunityToolkit.Mvvm |
| Serialization | System.Text.Json |
| Pattern | Command pattern (undo/redo) |

## Architecture

```
LevelEditor/
├── Commands/           # IEditorCommand, CommandHistory, TileCommands
├── Models/             # LevelDocument, TileLayer, EntityPlacement, TilePalette
├── Services/           # LevelSerializer (save/load/export)
├── ViewModels/         # EditorViewModel (MVVM core)
├── Views/              # AboutDialog
└── MainWindow.xaml     # Canvas renderer, input handling
```

## How the Command Pattern Works

Every action in the editor is an `IEditorCommand` with `Execute()` and `Undo()` 
methods. Painting a tile creates a `PlaceTileCommand` that stores both the new 
and previous tile state. Flood fills create a `BatchCommand` wrapping dozens of 
`PlaceTileCommand`s — the entire fill undoes in a single `Ctrl+Z`.

```csharp
public interface IEditorCommand
{
    string Description { get; }
    void Execute();
    void Undo();
}
```

## Keyboard Shortcuts

| Shortcut | Action |
|---|---|
| `Ctrl+N` | New level |
| `Ctrl+O` | Open level |
| `Ctrl+S` | Save level |
| `Ctrl+E` | Export for Unity |
| `Ctrl+Z` | Undo |
| `Ctrl+Y` | Redo |
| `1` / `2` / `3` | Switch active layer |
| `E` | Select eraser |
| `Shift+Click` | Flood fill |
| Right drag | Pan canvas |
| Scroll wheel | Zoom in/out |

## Unity Integration

Export your level via **File → Export for Unity**. The output JSON looks like:

```json
{
  "name": "level_01",
  "width": 20,
  "height": 15,
  "tileSize": 32,
  "layers": [
    {
      "name": "Ground",
      "zIndex": 0,
      "tiles": [
        { "key": "0,14", "tileId": "grass", "flipX": false, "flipY": false }
      ]
    }
  ],
  "entities": [
    { "id": "abc123", "type": "player_spawn", "x": 2, "y": 12, "properties": {} }
  ]
}
```

In Unity, deserialize with `JsonUtility` or Newtonsoft and instantiate prefabs 
by matching `tileId` and `type` to your asset library.

## Running

**Prerequisites:** Windows, .NET 9 SDK

```bash
git clone https://github.com/BokeChinmay/LevelEditor.git
cd LevelEditor/LevelEditor
dotnet run
```

## Demo

[▶ Watch the demo](https://youtu.be/qby0bty2J88)