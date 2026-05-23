/**************************************************************************************************************************
* Copyright 2026, Peter R. Nelson
*
* This file is part of the RealmStudioX application. The RealmStudioX application is intended
* for creating fantasy maps for gaming and world building.
*
* RealmStudioX is free software: you can redistribute it and/or modify it under the terms
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
* For questions about the RealmStudioX application or about licensing, please email
* support@brookmonte.com
*
***************************************************************************************************************************/
using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public class MapRegion : MapComponent2D
    {
        public string RegionName { get; set; } = string.Empty;
        public string RegionDescription { get; set; } = string.Empty;

        public SKColor RegionBorderColor { get; set; } = SKColor.Parse("#0056B3");
        public int RegionBorderWidth { get; set; } = 10;
        public int RegionInnerOpacity { get; set; } = 64;
        public int RegionBorderSmoothing { get; set; } = 20;
        public PathType RegionBorderType { get; set; } = PathType.SolidLinePath;

        public SKPaint RegionBorderPaint { get; set; } = new();

        public SKPaint RegionInnerPaint { get; set; } = new();

        public SKPath BoundaryPath { get; set; } = new();

        private readonly List<MapRegionPoint> _mapRegionPoints = [];
        public SKPoint SnappedStartPoint { get; set; } = SKPoint.Empty;

        public bool IsEditing { get; set; } = false;

        public MapRegion() { }

        public void AddRegionPoint(SKPoint point)
        {
            _mapRegionPoints.Add(new MapRegionPoint(point));

            using SKPath path = Utilities.BuildClosedPath([.. _mapRegionPoints.Select(p => p.RegionPoint)]);

            BoundaryPath.Dispose();

            BoundaryPath = new(path);
            Bounds = BoundaryPath.Bounds;
        }

        public void InsertRegionPoint(int index, SKPoint point)
        {
            _mapRegionPoints.Insert(index, new MapRegionPoint(point));

            using SKPath path = Utilities.BuildClosedPath([.. _mapRegionPoints.Select(p => p.RegionPoint)]);

            BoundaryPath.Dispose();

            BoundaryPath = new(path);
            Bounds = BoundaryPath.Bounds;
        }

        public IReadOnlyList<MapRegionPoint> RegionPoints => _mapRegionPoints;

        public int RegionPointCount => _mapRegionPoints.Count;

        public void RemoveRegionPointAt(int index)
        {
            if (index >= 0 && index < _mapRegionPoints.Count)
            {
                _mapRegionPoints.RemoveAt(index);

                using SKPath path = Utilities.BuildClosedPath([.. _mapRegionPoints.Select(p => p.RegionPoint)]);

                BoundaryPath.Dispose();

                BoundaryPath = new(path);
                Bounds = BoundaryPath.Bounds;
            }
        }

        public void RemoveRegionPoint(MapRegionPoint mrp)
        {
            if (_mapRegionPoints.Contains(mrp))
            {
                _mapRegionPoints.Remove(mrp);
                using SKPath path = Utilities.BuildClosedPath([.. _mapRegionPoints.Select(p => p.RegionPoint)]);

                BoundaryPath.Dispose();

                BoundaryPath = new(path);
                Bounds = BoundaryPath.Bounds;
            }
        }

        public void Move(float dx, float dy)
        {
            for (int i = 0; i < _mapRegionPoints.Count; i++)
            {
                MapRegionPoint point = _mapRegionPoints[i];
                point.RegionPoint = new SKPoint(point.RegionPoint.X + dx, point.RegionPoint.Y + dy);
            }

            using SKPath path = Utilities.BuildClosedPath([.. _mapRegionPoints.Select(p => p.RegionPoint)]);
           
            BoundaryPath.Dispose();
            
            BoundaryPath = new(path);
            Bounds = BoundaryPath.Bounds;
        }

        public void MoveRegionPoint(MapRegionPoint mrp, SKPoint newPoint)
        {
            if (mrp.IsSelected)
            {                
                mrp.RegionPoint = newPoint;
                using SKPath path = Utilities.BuildClosedPath([.. _mapRegionPoints.Select(p => p.RegionPoint)]);
                BoundaryPath.Dispose();
                BoundaryPath = new(path);
                Bounds = BoundaryPath.Bounds;
            }
        }

        public override void Render(SKCanvas canvas, FontManager? fontManager = null)
        {
            RegionRenderStyle regionBorderStyle = new()
            {
                MapPathType = RegionBorderType,
                BorderColor = RegionBorderColor,
                Opacity = RegionInnerOpacity,
                Color = RegionBorderColor,
                BorderWidth = RegionBorderWidth,
                Smoothing = RegionBorderSmoothing
            };

            RegionRenderer.Render(canvas, [.. _mapRegionPoints.Select(p => p.RegionPoint)], regionBorderStyle);
            
            if (IsSelected)
            {
                if (BoundaryPath != null)
                {
                    // draw an outline around the region to show that it is selected
                    canvas.DrawRect(Bounds, PaintObjects.RegionSelectPaint);

                    // draw dots on region vertices

                    float minDistance = 20;
                    float totalDistance = 0;

                    for (int i = 0; i < _mapRegionPoints.Count; i++)
                    {
                        MapRegionPoint point = _mapRegionPoints[i];

                        bool renderPoint = false;

                        if (i < _mapRegionPoints.Count - 1)
                        {
                            float distance = SKPoint.Distance(point.RegionPoint, _mapRegionPoints[i + 1].RegionPoint);
                            totalDistance += distance;

                            if (totalDistance > minDistance)
                            {
                                renderPoint = true;
                                totalDistance = 0;
                            }
                        }
                        else
                        {
                            renderPoint = true;
                        }

                        if (renderPoint)
                        {
                            point.Render(canvas);
                        }
                    }
                }
            }
        }

        public void ConstructRegionPaint()
        {
            RegionBorderPaint = new SKPaint()
            {
                StrokeWidth = RegionBorderWidth,
                Color = RegionBorderColor,
                Style = SKPaintStyle.Stroke,
                StrokeCap = SKStrokeCap.Round,
                StrokeJoin = SKStrokeJoin.Round,
                IsAntialias = true,
                PathEffect = SKPathEffect.CreateCorner(RegionBorderSmoothing)
            };

            SKColor innerColor = new(RegionBorderColor.Red, RegionBorderColor.Green, RegionBorderColor.Blue, (byte)RegionInnerOpacity);

            RegionInnerPaint = new SKPaint()
            {
                Color = innerColor,
                Style = SKPaintStyle.Fill,
                PathEffect = SKPathEffect.CreateCorner(RegionBorderSmoothing)
            };
        }


        public override bool HitTest(SKPoint worldPos)
        {
            return BoundaryPath.Contains(worldPos.X, worldPos.Y);
        }

        public override IShapeState CaptureState()
        {
            MapRegionState state = new()
            {
                RegionName = RegionName,
                RegionDescription = RegionDescription,
                RegionBorderColor = RegionBorderColor,
                RegionBorderWidth = RegionBorderWidth,
                RegionInnerOpacity = RegionInnerOpacity,
                RegionBorderSmoothing = RegionBorderSmoothing,
                RegionBorderType = RegionBorderType,
                MapRegionPoints =
                    [
                        .._mapRegionPoints.Select(
                            p => new MapRegionPoint(
                                new SKPoint(
                                    p.RegionPoint.X,
                                    p.RegionPoint.Y)))
                    ]
            };

            return state;
        }

        public override void RestoreState(IShapeState state)
        {
            if (state is MapRegionState regionState)
            {
                RegionName = regionState.RegionName;
                RegionDescription = regionState.RegionDescription;
                RegionBorderColor = regionState.RegionBorderColor;
                RegionBorderWidth = regionState.RegionBorderWidth;
                RegionInnerOpacity = regionState.RegionInnerOpacity;
                RegionBorderSmoothing = regionState.RegionBorderSmoothing;
                RegionBorderType = regionState.RegionBorderType;

                _mapRegionPoints.Clear();
                _mapRegionPoints.AddRange(regionState.MapRegionPoints.Select(p => new MapRegionPoint(new SKPoint(p.RegionPoint.X, p.RegionPoint.Y))));
                
                using SKPath path = Utilities.BuildClosedPath([.. _mapRegionPoints.Select(p => p.RegionPoint)]);
                
                BoundaryPath.Dispose();
                BoundaryPath = new(path);
                Bounds = BoundaryPath.Bounds;
            }
        }
    }
}
