/**************************************************************************************************************************
* Copyright 2024, Peter R. Nelson
*
* This file is part of the RealmStudio application. The RealmStudio application is intended
* for creating fantasy maps for gaming and world building.
*
* RealmStudio is free software: you can redistribute it and/or modify it under the terms
* of the GNU General Public License as published by the Free Software Foundation,
* either version 3 of the License, or (at your option) any later version.
*
* This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
* without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
* See the GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License along with this program.
* The text of the GNU General Public License (GPL) is found in the LICENSE.txt file.
* If the LICENSE.txt file is not present or the text of the GNU GPL is not present in the LICENSE.txt file,
* see https://www.gnu.org/licenses/.
*
* For questions about the RealmStudio application or about licensing, please email
* support@brookmonte.com
*
***************************************************************************************************************************/
#nullable enable

using SkiaSharp;
using System.Xml.Serialization;


namespace RealmStudioShapeRenderingLib
{
    [XmlType("MapLayer")]
    public class MapLayer
    {
        [XmlArray("Shapes")]
        [XmlArrayItem("Shape")]
        private readonly List<MapComponent2D> _shapes = new(500);

        [XmlIgnore]
        public IReadOnlyList<MapComponent2D> Shapes => _shapes;

        // -------------------------------------------------
        // Metadata
        // -------------------------------------------------

        [XmlAttribute]
        public Guid MapLayerGuid { get; set; } = Guid.NewGuid();

        [XmlAttribute]
        public string MapLayerName { get; set; } = "";

        [XmlAttribute]
        public float MapLayerOrder { get; set; }

        [XmlIgnore]
        public bool ShowLayer { get; set; } = true;

        [XmlAttribute]
        public bool Drawable { get; set; } = false;

        [XmlIgnore]
        public SKRect LayerRect { get; set; } = SKRect.Empty;


        [XmlIgnore]
        private readonly Queue<MapComponent2D> _placementQueue = new();

        [XmlIgnore]
        public int MaxPlacementsPerFrame { get; set; } = 300;

        // -------------------------------------------------
        // Tile Cache
        // -------------------------------------------------

        private class LayerTile
        {
            public SKRect Bounds;

            public SKSurface? Surface;
            public SKImage? Image;

            public bool IsModified = true;

            public readonly HashSet<MapComponent2D> TileShapes = [];

            public bool Contains(MapComponent2D symbol)
            {
                return TileShapes.Contains(symbol);
            }

            public void Add(MapComponent2D symbol)
            {
                TileShapes.Add(symbol);
            }

            public void Remove(MapComponent2D symbol)
            {
                TileShapes.Remove(symbol);
            }
        }

        [XmlIgnore]
        private readonly Dictionary<(int x, int y), LayerTile> _tiles = [];

        private const int TileSize = 1024;

        private static (int x, int y) GetTileCoord(SKPoint p)
        {
            return ((int)(p.X / TileSize), (int)(p.Y / TileSize));
        }

        private LayerTile GetOrCreateTile(int x, int y)
        {
            var key = (x, y);

            if (!_tiles.TryGetValue(key, out var tile))
            {
                tile = new LayerTile
                {
                    Bounds = new SKRect(
                        x * TileSize,
                        y * TileSize,
                        (x + 1) * TileSize,
                        (y + 1) * TileSize)
                };

                _tiles[key] = tile;
            }

            return tile;
        }

        // -------------------------------------------------
        // Spatial Grid
        // -------------------------------------------------

        private readonly Dictionary<(int x, int y), List<MapComponent2D>> _grid = new();

        private const float CellSize = 128f;

        private static (int x, int y) GetCell(SKPoint p)
        {
            return ((int)(p.X / CellSize), (int)(p.Y / CellSize));
        }

        private void AddToSpatialGrid(MapComponent2D shape)
        {
            var bounds = shape.Bounds;

            var topLeft = new SKPoint(bounds.Left, bounds.Top);
            var bottomRight = new SKPoint(bounds.Right, bounds.Bottom);

            var min = GetCell(topLeft);
            var max = GetCell(bottomRight);

            for (int x = min.x; x <= max.x; x++)
                for (int y = min.y; y <= max.y; y++)
                {
                    var key = (x, y);

                    if (!_grid.TryGetValue(key, out var list))
                    {
                        list = [];
                        _grid[key] = list;
                    }

                    list.Add(shape);
                }
        }

        private void RemoveFromSpatialGrid(MapComponent2D shape)
        {
            var bounds = shape.Bounds;

            var topLeft = new SKPoint(bounds.Left, bounds.Top);
            var bottomRight = new SKPoint(bounds.Right, bounds.Bottom);

            var min = GetCell(topLeft);
            var max = GetCell(bottomRight);

            for (int x = min.x; x <= max.x; x++)
                for (int y = min.y; y <= max.y; y++)
                {
                    if (_grid.TryGetValue((x, y), out var list))
                    {
                        list.Remove(shape);
                    }
                }
        }

        public IEnumerable<MapComponent2D> QueryNearby(SKPoint point)
        {
            var (cx, cy) = GetCell(point);

            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    var key = (cx + dx, cy + dy);

                    if (_grid.TryGetValue(key, out var list))
                    {
                        foreach (var shape in list)
                        {
                            yield return shape;
                        }
                    }
                }
        }

        // -------------------------------------------------
        // Shape Management
        // -------------------------------------------------

        public void Enqueue(MapComponent2D shape)
        {
            if (shape == null)
                return;

            _placementQueue.Enqueue(shape);
        }

        public void Add(MapComponent2D shape)
        {
            Enqueue(shape);
        }

        public void ProcessPlacementQueue()
        {
            int count = 0;

            while (_placementQueue.Count > 0 && count < MaxPlacementsPerFrame)
            {
                var shape = _placementQueue.Dequeue();

                AddInternal(shape);

                count++;
            }
        }

        private void AddInternal(MapComponent2D shape)
        {
            if (shape == null)
            {
                return;
            }

            _shapes.Add(shape);

            bool isVectorSymbol = shape is MapSymbol ms && ms.SymbolDefinition.SymbolFormat == SymbolFileFormat.Vector;
            // vector symbols are rendered directly from the MapLayer.Shapes list, not from the tiles

            //if (!isVectorSymbol)
            {
                // Add bitmap symbols to tiles (multi-tile aware)
                var bounds = shape.Bounds;

                var topLeft = new SKPoint(bounds.Left, bounds.Top);
                var bottomRight = new SKPoint(bounds.Right, bounds.Bottom);

                var minTile = GetTileCoord(topLeft);
                var maxTile = GetTileCoord(bottomRight);

                for (int x = minTile.x; x <= maxTile.x; x++)
                    for (int y = minTile.y; y <= maxTile.y; y++)
                    {
                        var tile = GetOrCreateTile(x, y);

                        tile.Add(shape);
                        tile.IsModified = true;
                    }
            }

            AddToSpatialGrid(shape);
        }

        public void Remove(MapComponent2D shape)
        {
            _shapes.Remove(shape);

            bool isVectorSymbol = shape is MapSymbol ms && ms.SymbolDefinition.SymbolFormat == SymbolFileFormat.Vector;

            if (!isVectorSymbol)
            {
                var bounds = shape.Bounds;

                var topLeft = new SKPoint(bounds.Left, bounds.Top);
                var bottomRight = new SKPoint(bounds.Right, bounds.Bottom);

                var minTile = GetTileCoord(topLeft);
                var maxTile = GetTileCoord(bottomRight);

                for (int x = minTile.x; x <= maxTile.x; x++)
                    for (int y = minTile.y; y <= maxTile.y; y++)
                    {
                        if (_tiles.TryGetValue((x, y), out var tile))
                        {
                            tile.Remove(shape);
                            tile.IsModified = true;
                        }
                    }
            }

            RemoveFromSpatialGrid(shape);
        }

        public void Clear()
        {
            _shapes.Clear();
            _tiles.Clear();
            _grid.Clear();
        }

        public void InvalidateSymbol(MapSymbol symbol)
        {
            // simplest version: nuke entire cache
            InvalidateAllTiles();
        }

        public void InvalidateAllTiles()
        {
            foreach (var tile in _tiles.Values)
            {
                tile.IsModified = true;
            }
        }

        public void UpdateSymbolTiles(MapSymbol symbol, SKRect oldBounds, SKRect newBounds)
        {
            //if (symbol.SymbolDefinition.SymbolFormat == SymbolFileFormat.Vector)
            //{
            //    // vector symbols are rendered directly from the MapLayer.Shapes list, not from the tiles
            //    return;
            //}

            var affected = SKRect.Union(oldBounds, newBounds);

            int minX = (int)MathF.Floor(affected.Left / TileSize);
            int maxX = (int)MathF.Floor(affected.Right / TileSize);

            int minY = (int)MathF.Floor(affected.Top / TileSize);
            int maxY = (int)MathF.Floor(affected.Bottom / TileSize);

            // Remove from tiles no longer intersecting
            foreach (var tile in _tiles.Values)
            {
                if (tile.Contains(symbol) && !tile.Bounds.IntersectsWith(newBounds))
                {
                    tile.Remove(symbol);
                    tile.IsModified = true;
                }
            }

            // Add to new tiles
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    if (_tiles.TryGetValue((x, y), out var tile))
                    {
                        if (!tile.Contains(symbol))
                        {
                            tile.Add(symbol);
                        }

                        tile.IsModified = true;
                    }
                }
            }
        }

        // -------------------------------------------------
        // Rendering
        // -------------------------------------------------

        public void Draw(SKCanvas canvas, SKRect viewport)
        {
            if (!ShowLayer || !Drawable)
                return;

            var topLeft = new SKPoint(viewport.Left, viewport.Top);
            var bottomRight = new SKPoint(viewport.Right, viewport.Bottom);

            var minTile = GetTileCoord(topLeft);
            var maxTile = GetTileCoord(bottomRight);

            for (int x = minTile.x; x <= maxTile.x; x++)
                for (int y = minTile.y; y <= maxTile.y; y++)
                {
                    if (_tiles.TryGetValue((x, y), out var tile))
                    {
                        EnsureTileCache(tile);

                        if (tile.Image != null)
                        {
                            canvas.DrawImage(tile.Image, tile.Bounds.Location);
                        }
                    }
                }
        }

        private static void EnsureTileCache(LayerTile tile)
        {
            if (!tile.IsModified && tile.Image != null)
            {
                return;
            }

            tile.Surface?.Dispose();
            tile.Image?.Dispose();

            var info = new SKImageInfo(TileSize, TileSize);
            tile.Surface = SKSurface.Create(info);

            var canvas = tile.Surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            canvas.Save();
            canvas.Translate(-tile.Bounds.Left, -tile.Bounds.Top);

            foreach (var shape in tile.TileShapes)
            {
                if (!shape.Bounds.IntersectsWith(tile.Bounds))
                {
                    continue;
                }

                shape.Render(canvas);
            }

            canvas.Restore();

            tile.Image = tile.Surface.Snapshot();
            tile.IsModified = false;
        }

        // -------------------------------------------------
        // Hit Testing
        // -------------------------------------------------

        public MapComponent2D? HitTest(SKPoint worldPos)
        {
            foreach (var shape in QueryNearby(worldPos).Reverse())
            {
                if (shape is Shape2D s2d)
                {
                    if (s2d.Bounds.Contains(worldPos) &&
                        s2d.HitPath.Contains(worldPos.X, worldPos.Y))
                    {
                        return shape;
                    }
                }
            }

            return null;
        }
    }
}


