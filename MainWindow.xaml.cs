using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using LevelEditor.Models;
using LevelEditor.ViewModels;

namespace LevelEditor;

public partial class MainWindow : Window
{
    private EditorViewModel _vm => (EditorViewModel)DataContext;
    private bool _isPainting = false;
    private bool _isPanning = false;
    private Point _lastPanPoint;
    private int _lastPaintedX = -1;
    private int _lastPaintedY = -1;

    // Visual caches for performance
    private readonly Dictionary<string, Rectangle> _tileRects = new();
    private readonly Dictionary<string, Ellipse> _entityEllipses = new();

    public MainWindow()
    {
        InitializeComponent();
        _vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(EditorViewModel.Level))
                RedrawCanvas();
        };

        _vm.LevelChanged += () => RedrawCanvas();

        Loaded += (s, e) => RedrawCanvas();
    }

    //Canvas interaction

    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        EditorCanvas.CaptureMouse();
        _isPainting = true;
        _lastPaintedX = -1;
        _lastPaintedY = -1;
        HandleCanvasClick(e.GetPosition(EditorCanvas));
    }

    private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        EditorCanvas.ReleaseMouseCapture();
        _isPainting = false;
    }

    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isPanning)
        {
            var current = e.GetPosition(this);
            _vm.OffsetX += current.X - _lastPanPoint.X;
            _vm.OffsetY += current.Y - _lastPanPoint.Y;
            _lastPanPoint = current;
            return;
        }

        if (_isPainting && e.LeftButton == MouseButtonState.Pressed)
            HandleCanvasClick(e.GetPosition(EditorCanvas));

        // Update status with current grid position
        var pos = e.GetPosition(EditorCanvas);
        var (gx, gy) = ScreenToGrid(pos);
        if (gx >= 0 && gx < _vm.Level.Width && gy >= 0 && gy < _vm.Level.Height)
            _vm.StatusText = $"Grid: ({gx}, {gy})  |  " +
                $"Layer: {_vm.ActiveLayer?.Name}  |  " +
                $"Zoom: {_vm.Zoom:P0}";
    }

    private void Canvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isPanning = true;
        _lastPanPoint = e.GetPosition(this);
        EditorCanvas.CaptureMouse();
        EditorCanvas.Cursor = Cursors.Hand;
    }

    protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseRightButtonUp(e);
        _isPanning = false;
        EditorCanvas.ReleaseMouseCapture();
        EditorCanvas.Cursor = Cursors.Cross;
    }

    private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        var delta = e.Delta > 0 ? 0.1 : -0.1;
        _vm.Zoom = Math.Clamp(_vm.Zoom + delta, 0.2, 4.0);
    }

    private void HandleCanvasClick(Point canvasPos)
    {
        var (gridX, gridY) = ScreenToGrid(canvasPos);

        // Avoid re-painting the same cell during drag
        if (gridX == _lastPaintedX && gridY == _lastPaintedY) return;
        _lastPaintedX = gridX;
        _lastPaintedY = gridY;

        if (_vm.IsEntityMode)
        {
            _vm.PlaceEntity(gridX, gridY);
            RedrawEntities();
        }
        else
        {
            _vm.PaintTile(gridX, gridY);
            RedrawTileAt(gridX, gridY);
        }
    }

    private (int x, int y) ScreenToGrid(Point canvasPos)
    {
        var tileSize = _vm.Level.TileSize;
        return ((int)(canvasPos.X / tileSize), (int)(canvasPos.Y / tileSize));
    }

    //Canvas rendering

    public void RedrawCanvas()
    {
        EditorCanvas.Children.Clear();
        _tileRects.Clear();
        _entityEllipses.Clear();

        DrawGrid();
        DrawAllTiles();
        DrawEntities();
    }

    private void DrawGrid()
    {
        var tileSize = _vm.Level.TileSize;
        var width = _vm.Level.Width;
        var height = _vm.Level.Height;
        var gridColor = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));

        // Vertical lines
        for (int x = 0; x <= width; x++)
        {
            var line = new Line
            {
                X1 = x * tileSize, Y1 = 0,
                X2 = x * tileSize, Y2 = height * tileSize,
                Stroke = gridColor, StrokeThickness = x == 0 ? 2 : 0.5
            };
            EditorCanvas.Children.Add(line);
        }

        // Horizontal lines
        for (int y = 0; y <= height; y++)
        {
            var line = new Line
            {
                X1 = 0, Y1 = y * tileSize,
                X2 = width * tileSize, Y2 = y * tileSize,
                Stroke = gridColor, StrokeThickness = y == 0 ? 2 : 0.5
            };
            EditorCanvas.Children.Add(line);
        }

        // Level bounds highlight
        var border = new Rectangle
        {
            Width = width * tileSize,
            Height = height * tileSize,
            Stroke = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
            StrokeThickness = 2,
            Fill = Brushes.Transparent
        };
        Canvas.SetLeft(border, 0);
        Canvas.SetTop(border, 0);
        EditorCanvas.Children.Add(border);
    }

    private void DrawAllTiles()
    {
        var tileSize = _vm.Level.TileSize;

        foreach (var layer in _vm.Level.Layers.OrderBy(l => l.ZIndex))
        {
            if (!layer.IsVisible) continue;

            foreach (var kvp in layer.Tiles)
            {
                var parts = kvp.Key.Split(',');
                if (parts.Length != 2) continue;
                if (!int.TryParse(parts[0], out var x) ||
                    !int.TryParse(parts[1], out var y)) continue;

                var tileDef = _vm.TileDefinitions
                    .FirstOrDefault(t => t.Id == kvp.Value.TileId);
                if (tileDef == null) continue;

                var rect = CreateTileRect(tileDef, x, y, tileSize);
                EditorCanvas.Children.Add(rect);
                _tileRects[$"{layer.Name}:{kvp.Key}"] = rect;
            }
        }
    }

    private void RedrawTileAt(int gridX, int gridY)
    {
        var tileSize = _vm.Level.TileSize;
        var key = $"{gridX},{gridY}";

        // Remove existing visuals for this cell across all layers
        foreach (var layer in _vm.Level.Layers)
        {
            var cacheKey = $"{layer.Name}:{key}";
            if (_tileRects.TryGetValue(cacheKey, out var existing))
            {
                EditorCanvas.Children.Remove(existing);
                _tileRects.Remove(cacheKey);
            }
        }

        // Redraw all layers at this cell
        foreach (var layer in _vm.Level.Layers.OrderBy(l => l.ZIndex))
        {
            if (!layer.IsVisible) continue;
            var tile = layer.GetTile(gridX, gridY);
            if (tile == null) continue;

            var tileDef = _vm.TileDefinitions
                .FirstOrDefault(t => t.Id == tile.TileId);
            if (tileDef == null) continue;

            var rect = CreateTileRect(tileDef, gridX, gridY, tileSize);
            EditorCanvas.Children.Add(rect);
            _tileRects[$"{layer.Name}:{key}"] = rect;
        }
    }

    private Rectangle CreateTileRect(TileDefinition tileDef, int x, int y, int tileSize)
    {
        var color = (Color)ColorConverter.ConvertFromString(tileDef.Color);
        var rect = new Rectangle
        {
            Width = tileSize - 1,
            Height = tileSize - 1,
            Fill = new SolidColorBrush(color),
            Opacity = tileDef.Category == "Decoration" ? 0.8 : 1.0
        };

        // Visual indicator for solid tiles
        if (tileDef.IsSolid)
        {
            rect.Stroke = new SolidColorBrush(
                Color.FromArgb(120, 255, 255, 255));
            rect.StrokeThickness = 1;
        }

        if (tileDef.IsOneWay)
        {
            rect.StrokeDashArray = new DoubleCollection { 4, 2 };
            rect.Stroke = Brushes.Yellow;
            rect.StrokeThickness = 1.5;
        }

        Canvas.SetLeft(rect, x * tileSize + 1);
        Canvas.SetTop(rect, y * tileSize + 1);
        return rect;
    }

    private void DrawEntities()
    {
        var tileSize = _vm.Level.TileSize;

        foreach (var entity in _vm.Level.Entities)
        {
            var def = _vm.EntityDefinitions
                .FirstOrDefault(e => e.Id == entity.EntityType);
            if (def == null) continue;

            var ellipse = CreateEntityEllipse(def, entity, tileSize);
            EditorCanvas.Children.Add(ellipse);
            _entityEllipses[entity.Id] = ellipse;
        }
    }

    private void RedrawEntities()
    {
        var tileSize = _vm.Level.TileSize;

        // Remove old entity visuals
        foreach (var ellipse in _entityEllipses.Values)
            EditorCanvas.Children.Remove(ellipse);
        _entityEllipses.Clear();

        DrawEntities();
    }

    private Ellipse CreateEntityEllipse(EntityDefinition def, EntityPlacement entity, int tileSize)
    {
        var color = (Color)ColorConverter.ConvertFromString(def.Color);
        var ellipse = new Ellipse
        {
            Width = tileSize - 4,
            Height = tileSize - 4,
            Fill = new SolidColorBrush(Color.FromArgb(200, color.R, color.G, color.B)),
            Stroke = new SolidColorBrush(color),
            StrokeThickness = 2,
            ToolTip = $"{def.Name}\n({entity.GridX}, {entity.GridY})",
            Cursor = Cursors.Hand
        };

        ellipse.MouseLeftButtonDown += (s, e) =>
        {
            _vm.SelectedEntityPlacement = entity;
            e.Handled = true;
        };

        Canvas.SetLeft(ellipse, entity.GridX * tileSize + 2);
        Canvas.SetTop(ellipse, entity.GridY * tileSize + 2);
        return ellipse;
    }

    //Palette button handlers

    private void TileButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is TileDefinition tile)
        {
            _vm.SelectedTile = tile;
            _vm.IsEntityMode = false;
        }
    }

    private void EntityButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is EntityDefinition entity)
        {
            _vm.SelectedEntity = entity;
            _vm.IsEntityMode = true;
        }
    }

    private void LayerButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is TileLayer layer)
            _vm.ActiveLayer = layer;
    }
}