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
        private readonly List<MapComponent2D> _shapes = new(500);

        [XmlArray("Shapes")]
        [XmlArrayItem("Shape2D", Type = typeof(Shape2D))]
        [XmlArrayItem("PaintedShape", Type = typeof(PaintedShape))]
        [XmlArrayItem("Landform", Type = typeof(Landform))]
        [XmlArrayItem("WaterBody", Type = typeof(WaterBody))]
        [XmlArrayItem("MapPath", Type = typeof(MapPath))]
        [XmlArrayItem("MapSymbol", Type = typeof(MapSymbol))]
        [XmlArrayItem("River", Type = typeof(River))]
        [XmlArrayItem("Lake", Type = typeof(Lake))]
        [XmlArrayItem("PaintedWaterBody", Type = typeof(PaintedWaterBody))]
        [XmlArrayItem("MapLabel", Type = typeof(MapLabel))]
        [XmlArrayItem("PlacedMapBox", Type = typeof(PlacedMapBox))]
        [XmlArrayItem("PlacedMapFrame", Type = typeof(PlacedMapFrame))]
        [XmlArrayItem("MapGrid", Type = typeof(MapGrid))]
        [XmlArrayItem("Windrose", Type = typeof(MapWindrose))]
        [XmlArrayItem("MapScale", Type = typeof(MapScale))]
        [XmlArrayItem("MapRegion", Type = typeof(MapRegion))]
        [XmlArrayItem("MapVignette", Type = typeof(MapVignette))]

        //[XmlArrayItem("MapHeightMap", Type = typeof(MapHeightMap))]

        [XmlArrayItem("DrawnArrow", Type = typeof(DrawnArrow))]
        [XmlArrayItem("DrawingErase", Type = typeof(DrawingErase))]
        [XmlArrayItem("DrawnDiamond", Type = typeof(DrawnDiamond))]
        [XmlArrayItem("DrawnEllipse", Type = typeof(DrawnEllipse))]
        [XmlArrayItem("DrawnFivePointStar", Type = typeof(DrawnFivePointStar))]
        [XmlArrayItem("DrawnLine", Type = typeof(DrawnLine))]
        [XmlArrayItem("DrawnPolygon", Type = typeof(DrawnPolygon))]
        [XmlArrayItem("DrawnRectangle", Type = typeof(DrawnRectangle))]
        [XmlArrayItem("DrawnRegularPolygon", Type = typeof(DrawnRegularPolygon))]
        [XmlArrayItem("DrawnSixPointStar", Type = typeof(DrawnSixPointStar))]
        [XmlArrayItem("DrawnStamp", Type = typeof(DrawnStamp))]
        [XmlArrayItem("DrawnTriangle", Type = typeof(DrawnTriangle))]
        [XmlArrayItem("PaintedLine", Type = typeof(PaintedLine))]
        [XmlArrayItem("DrawnPixelEdits", Type = typeof(DrawnPixelEdits))]
        public List<MapComponent2D> Shapes
        {
            get => _shapes;
            set
            {
                _shapes.Clear();

                if (value != null)
                {
                    _shapes.AddRange(value);
                }
            }
        }

        // -------------------------------------------------
        // Metadata
        // -------------------------------------------------

        [XmlAttribute]
        public string MapLayerId { get; set; } = Guid.NewGuid().ToString();

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

            public bool Contains(MapComponent2D shape)
            {
                return TileShapes.Contains(shape);
            }

            public void Add(MapComponent2D shape)
            {
                TileShapes.Add(shape);
            }

            public void Remove(MapComponent2D shape)
            {
                TileShapes.Remove(shape);
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
            int x = (int)MathF.Floor(p.X / CellSize);
            int y = (int)MathF.Floor(p.Y / CellSize);
            return (x, y);
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

        public IEnumerable<MapComponent2D> QuerySymbolsInRadius(SKPoint center, float radius)
        {
            float r2 = radius * radius;

            var min = new SKPoint(center.X - radius, center.Y - radius);
            var max = new SKPoint(center.X + radius, center.Y + radius);

            var minCell = GetCell(min);
            var maxCell = GetCell(max);

            HashSet<MapSymbol> results = new();

            for (int x = minCell.x; x <= maxCell.x; x++)
            {
                for (int y = minCell.y; y <= maxCell.y; y++)
                {
                    if (!_grid.TryGetValue((x, y), out var list))
                    {
                        continue;
                    }

                    foreach (var shape in list)
                    {
                        if (shape is not MapSymbol ms)
                            continue;

                        var pos = ms.Location;

                        float dx = pos.X - center.X;
                        float dy = pos.Y - center.Y;

                        if ((dx * dx + dy * dy) <= r2)
                        {
                            results.Add(ms);
                        }
                    }
                }
            }

            return results;
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
            if (shape is MapSymbol || shape is IDrawnMapComponent)
            {
                Enqueue(shape);
            }
            else
            {
                _shapes.Add(shape);
            }
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

            // Add symbols to tiles (multi-tile aware)
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


            AddToSpatialGrid(shape);
        }

        public void Remove(MapComponent2D shape)
        {
            _shapes.Remove(shape);

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

        public void UpdateShapeTiles(MapComponent2D shape, SKRect oldBounds, SKRect newBounds)
        {
            // Build old tile set
            HashSet<(int x, int y)> oldTiles = [];

            int oldMinX = (int)MathF.Floor(oldBounds.Left / TileSize);
            int oldMaxX = (int)MathF.Floor(oldBounds.Right / TileSize);

            int oldMinY = (int)MathF.Floor(oldBounds.Top / TileSize);
            int oldMaxY = (int)MathF.Floor(oldBounds.Bottom / TileSize);

            for (int x = oldMinX; x <= oldMaxX; x++)
            {
                for (int y = oldMinY; y <= oldMaxY; y++)
                {
                    oldTiles.Add((x, y));
                }
            }

            // Build new tile set
            HashSet<(int x, int y)> newTiles = [];

            int newMinX = (int)MathF.Floor(newBounds.Left / TileSize);
            int newMaxX = (int)MathF.Floor(newBounds.Right / TileSize);

            int newMinY = (int)MathF.Floor(newBounds.Top / TileSize);
            int newMaxY = (int)MathF.Floor(newBounds.Bottom / TileSize);

            for (int x = newMinX; x <= newMaxX; x++)
            {
                for (int y = newMinY; y <= newMaxY; y++)
                {
                    newTiles.Add((x, y));
                }
            }

            // Remove from tiles no longer occupied
            foreach (var key in oldTiles.Except(newTiles))
            {
                if (_tiles.TryGetValue(key, out var tile))
                {
                    tile.Remove(shape);
                    tile.IsModified = true;
                }
            }

            // Add to newly occupied tiles
            foreach (var key in newTiles.Except(oldTiles))
            {
                var tile = GetOrCreateTile(key.x, key.y);

                tile.Add(shape);
                tile.IsModified = true;
            }

            // Mark tiles occupied before and after as dirty
            foreach (var key in oldTiles.Union(newTiles))
            {
                if (_tiles.TryGetValue(key, out var tile))
                {
                    tile.IsModified = true;
                }
            }
        }



        // -------------------------------------------------
        // MapSymbol Z Ordering
        // -------------------------------------------------

        public void MoveMapComponentZOrder(MapComponent2D selected, ZOrderMoveType moveType)
        {
            if (selected == null)
                return;

            int adaptiveStep = GetAdaptiveStepSize();

            switch (moveType)
            {
                case ZOrderMoveType.ForwardOne:
                    MoveShape(selected, +1);
                    break;

                case ZOrderMoveType.BackwardOne:
                    MoveShape(selected, -1);
                    break;

                case ZOrderMoveType.ForwardStep:
                    MoveShape(selected, +adaptiveStep);
                    break;

                case ZOrderMoveType.BackwardStep:
                    MoveShape(selected, -adaptiveStep);
                    break;

                case ZOrderMoveType.ToTop:
                    MoveToTop(selected);
                    break;

                case ZOrderMoveType.ToBottom:
                    MoveToBottom(selected);
                    break;

                case ZOrderMoveType.AboveAllOverlaps:
                    MoveAboveAllOverlaps(selected);
                    break;

                case ZOrderMoveType.BelowAllOverlaps:
                    MoveBelowAllOverlaps(selected);
                    break;
            }
        }

        public void MoveShape(MapComponent2D shape, int delta)
        {
            if (shape == null || delta == 0)
            {
                return;
            }

            int index = _shapes.IndexOf(shape);
            
            if (index < 0)
            {
                return;
            }

            int newIndex = Math.Clamp(index + delta, 0, _shapes.Count - 1);

            if (newIndex == index)
            {
                return;
            }

            // IMPORTANT: adjust index if removing before inserting forward
            if (newIndex > index)
            {
                newIndex--;
            }

            MoveShapeToIndex(shape, newIndex);
        }

        public void MoveToTop(MapComponent2D shape)
        {
            if (shape == null)
            {
                return;
            }

            int index = _shapes.IndexOf(shape);
            if (index < 0 || index == _shapes.Count - 1)
            {
                return; // already at top or not found
            }

            _shapes.RemoveAt(index);
            _shapes.Add(shape);

            InvalidateAllTiles();
        }

        public void MoveToBottom(MapComponent2D shape)
        {
            if (shape == null)
            {
                return;
            }

            int index = _shapes.IndexOf(shape);
            if (index <= 0)
            {
                return; // already at bottom or not found
            }

            _shapes.RemoveAt(index);
            _shapes.Insert(0, shape);

            InvalidateAllTiles();
        }

        private void MoveAboveAllOverlaps(MapComponent2D selected)
        {
            var shapes = _shapes;
            int currentIndex = shapes.IndexOf(selected);
            if (currentIndex < 0) return;

            var overlaps = shapes
                .Where(s => s != selected &&
                            s.Bounds.IntersectsWith(selected.Bounds))
                .ToList();

            // Find the highest (max index) overlapping symbol ABOVE current
            int? maxIndex = overlaps
                .Select(s => shapes.IndexOf(s))
                .Where(i => i > currentIndex)
                .DefaultIfEmpty(-1)
                .Max();

            if (maxIndex.HasValue && maxIndex.Value > currentIndex)
            {
                MoveShapeToIndex(selected, maxIndex.Value);
            }
        }

        private void MoveBelowAllOverlaps(MapComponent2D selected)
        {
            var shapes = _shapes;
            int currentIndex = shapes.IndexOf(selected);
            if (currentIndex < 0) return;

            var overlaps = shapes
                .Where(s => s != selected &&
                            s.Bounds.IntersectsWith(selected.Bounds))
                .ToList();

            // Find the lowest (min index) overlapping symbol BELOW current
            int? minIndex = overlaps
                .Select(s => shapes.IndexOf(s))
                .Where(i => i < currentIndex)
                .DefaultIfEmpty(-1)
                .Min();

            if (minIndex.HasValue && minIndex.Value >= 0 && minIndex.Value < currentIndex)
            {
                MoveShapeToIndex(selected, minIndex.Value);
            }
        }

        private int GetAdaptiveStepSize()
        {
            int count = _shapes.Count;

            return Math.Clamp(count / 50, 5, 500);
        }

        private void MoveAboveNextOverlap(MapSymbol selected)
        {
            var shapes = _shapes;
            int currentIndex = shapes.IndexOf(selected);
            if (currentIndex < 0) return;

            var overlaps = _shapes
                .Where(s => s != selected &&
                s.Bounds.IntersectsWith(selected.Bounds))
                .ToList();

            var (shape, index) = overlaps
                .Select(s => (shape: s, index: shapes.IndexOf(s)))
                .Where(x => x.index > currentIndex)
                .OrderBy(x => x.index)
                .FirstOrDefault();

            if (shape != null)
            {
                MoveShapeToIndex(selected, index);
            }
        }

        private void MoveBelowNextOverlap(MapSymbol selected)
        {
            var shapes = _shapes;
            int currentIndex = shapes.IndexOf(selected);
            if (currentIndex < 0) return;

            var overlaps = _shapes
                .Where(s => s != selected &&
                s.Bounds.IntersectsWith(selected.Bounds))
                .ToList();

            var (shape, index) = overlaps
                .Select(s => (shape: s, index: shapes.IndexOf(s)))
                .Where(x => x.index < currentIndex)
                .OrderByDescending(x => x.index)
                .FirstOrDefault();

            if (shape != null)
            {
                MoveShapeToIndex(selected, index);
            }
        }

        public void MoveShapeToIndex(MapComponent2D shape, int newIndex)
        {
            int index = _shapes.IndexOf(shape);
            if (index < 0) return;

            newIndex = Math.Clamp(newIndex, 0, _shapes.Count - 1);

            if (index == newIndex) return;

            _shapes.RemoveAt(index);
            _shapes.Insert(newIndex, shape);

            InvalidateAllTiles();
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

        private void EnsureTileCache(LayerTile tile)
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

            foreach (var shape in _shapes)
            {
                if (!tile.TileShapes.Contains(shape))
                    continue;

                if (!shape.Bounds.IntersectsWith(tile.Bounds))
                    continue;

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




