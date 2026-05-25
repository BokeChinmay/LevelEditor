using LevelEditor.Models;

namespace LevelEditor.Commands;

public class PlaceTileCommand : IEditorCommand {
    private readonly TileLayer _layer;
    private readonly int _x, _y;
    private readonly TilePlacement? _newTile;
    private readonly TilePlacement? _previousTile;

    public string Description => _newTile == null ? $"Erase tile at ({_x},{_y})" :  $"Place {_newTile.TileId} at ({_x},{_y})";

    public PlaceTileCommand(TileLayer layer, int x, int y, TilePlacement? newTile) {
        _layer = layer;
        _x = x;
        _y = y;
        _newTile = newTile;
        _previousTile = layer.GetTile(x, y);
    }

    public void Execute() => _layer.SetTile(_x, _y, _newTile);
    public void Undo() => _layer.SetTile(_x, _y, _previousTile);
}

public class PlaceEntityCommand : IEditorCommand {
    private readonly List<EntityPlacement> _entities;
    private readonly EntityPlacement _entity;

    public string Description => $"Place {_entity.EntityType} at ({_entity.GridX},{_entity.GridY})";

    public PlaceEntityCommand(List<EntityPlacement> entities, EntityPlacement entity)
    {
        _entities = entities;
        _entity = entity;
    }

    public void Execute() => _entities.Add(_entity);
    public void Undo() => _entities.Remove(_entity);
}

public class RemoveEntityCommand : IEditorCommand {
    private readonly List<EntityPlacement> _entities;
    private readonly EntityPlacement _entity;

    public string Description => $"Remove {_entity.EntityType}";

    public RemoveEntityCommand(List<EntityPlacement> entities, EntityPlacement entity)
    {
        _entities = entities;
        _entity = entity;
    }

    public void Execute() => _entities.Remove(_entity);
    public void Undo() => _entities.Add(_entity);
}

public class BatchCommand : IEditorCommand {
    private readonly List<IEditorCommand> _commands;
    public string Description { get; }

    public BatchCommand(List<IEditorCommand> commands, string description) {
        _commands = commands;
        Description = description;
    }

    public void Execute() { foreach (var c in _commands) c.Execute(); }
    public void Undo() { foreach (var c in Enumerable.Reverse(_commands)) c.Undo(); }
}