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
using RealmStudioX.WPF.EditorUtilities;
using SkiaSharp;
using System.Xml.Serialization;

namespace RealmStudioShapeRenderingLib
{
    public class MapRegion : MapComponent2D
    {
        [XmlElement]
        public string RegionName { get; set; } = string.Empty;

        [XmlElement]
        public string RegionDescription { get; set; } = string.Empty;

        [XmlIgnore]
        public SKColor RegionBorderColor { get; set; } = SKColor.Parse("#0056B3");

        [XmlElement("RegionBorderColor")]
        public string RegionBorderColorXml
        {
            get => XmlColorConverter.Serialize(RegionBorderColor);
            set => RegionBorderColor = XmlColorConverter.Deserialize(value);
        }

        [XmlElement]
        public int RegionBorderWidth { get; set; } = 10;

        [XmlElement]
        public int RegionInnerOpacity { get; set; } = 64;

        [XmlElement]
        public int RegionBorderSmoothing { get; set; } = 20;

        [XmlElement]
        public PathType RegionBorderType { get; set; } = PathType.SolidLinePath;

        [XmlIgnore]
        public SKPaint RegionBorderPaint { get; set; } = new();

        [XmlIgnore]
        public SKPaint RegionInnerPaint { get; set; } = new();

        [XmlIgnore]
        public SKPath BoundaryPath { get; set; } = new();

        [XmlArray]
        [XmlArrayItem("RegionPoint", Type = typeof(MapRegionPoint))]
        public List<MapRegionPoint> MapRegionPoints { get; set; } = [];

        [XmlIgnore]
        public SKPoint SnappedStartPoint { get; set; } = SKPoint.Empty;

        [XmlIgnore]
        public bool IsEditing { get; set; } = false;

        public MapRegion() { }

        public override void FinalizeShapeGeometry(RealmStudioMap map)
        {
            using SKPath path = Utilities.BuildClosedPath([.. MapRegionPoints.Select(p => p.RegionPoint)]);

            BoundaryPath.Dispose();

            BoundaryPath = new(path);
            Bounds = BoundaryPath.Bounds;
        }

        public void AddRegionPoint(SKPoint point)
        {
            MapRegionPoints.Add(new MapRegionPoint(point));

            using SKPath path = Utilities.BuildClosedPath([.. MapRegionPoints.Select(p => p.RegionPoint)]);

            BoundaryPath.Dispose();

            BoundaryPath = new(path);
            Bounds = BoundaryPath.Bounds;
        }

        public void InsertRegionPoint(int index, SKPoint point)
        {
            MapRegionPoints.Insert(index, new MapRegionPoint(point));

            using SKPath path = Utilities.BuildClosedPath([.. MapRegionPoints.Select(p => p.RegionPoint)]);

            BoundaryPath.Dispose();

            BoundaryPath = new(path);
            Bounds = BoundaryPath.Bounds;
        }

        public int RegionPointCount => MapRegionPoints.Count;

        public void RemoveRegionPointAt(int index)
        {
            if (index >= 0 && index < MapRegionPoints.Count)
            {
                MapRegionPoints.RemoveAt(index);

                using SKPath path = Utilities.BuildClosedPath([.. MapRegionPoints.Select(p => p.RegionPoint)]);

                BoundaryPath.Dispose();

                BoundaryPath = new(path);
                Bounds = BoundaryPath.Bounds;
            }
        }

        public void RemoveRegionPoint(MapRegionPoint mrp)
        {
            if (MapRegionPoints.Contains(mrp))
            {
                MapRegionPoints.Remove(mrp);
                using SKPath path = Utilities.BuildClosedPath([.. MapRegionPoints.Select(p => p.RegionPoint)]);

                BoundaryPath.Dispose();

                BoundaryPath = new(path);
                Bounds = BoundaryPath.Bounds;
            }
        }

        public void Move(float dx, float dy)
        {
            for (int i = 0; i < MapRegionPoints.Count; i++)
            {
                MapRegionPoint point = MapRegionPoints[i];
                point.RegionPoint = new SKPoint(point.RegionPoint.X + dx, point.RegionPoint.Y + dy);
            }

            using SKPath path = Utilities.BuildClosedPath([.. MapRegionPoints.Select(p => p.RegionPoint)]);
           
            BoundaryPath.Dispose();
            
            BoundaryPath = new(path);
            Bounds = BoundaryPath.Bounds;
        }

        public void MoveRegionPoint(MapRegionPoint mrp, SKPoint newPoint)
        {
            if (mrp.IsSelected)
            {                
                mrp.RegionPoint = newPoint;
                using SKPath path = Utilities.BuildClosedPath([.. MapRegionPoints.Select(p => p.RegionPoint)]);
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

            RegionRenderer.Render(canvas, [.. MapRegionPoints.Select(p => p.RegionPoint)], regionBorderStyle);
            
            if (IsSelected)
            {
                if (BoundaryPath != null)
                {
                    // draw an outline around the region to show that it is selected
                    canvas.DrawRect(Bounds, PaintObjects.RegionSelectPaint);

                    // draw dots on region vertices

                    float minDistance = 20;
                    float totalDistance = 0;

                    for (int i = 0; i < MapRegionPoints.Count; i++)
                    {
                        MapRegionPoint point = MapRegionPoints[i];

                        bool renderPoint = false;

                        if (i < MapRegionPoints.Count - 1)
                        {
                            float distance = SKPoint.Distance(point.RegionPoint, MapRegionPoints[i + 1].RegionPoint);
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
                        ..MapRegionPoints.Select(
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

                MapRegionPoints.Clear();
                MapRegionPoints.AddRange(regionState.MapRegionPoints.Select(p => new MapRegionPoint(new SKPoint(p.RegionPoint.X, p.RegionPoint.Y))));
                
                using SKPath path = Utilities.BuildClosedPath([.. MapRegionPoints.Select(p => p.RegionPoint)]);
                
                BoundaryPath.Dispose();
                BoundaryPath = new(path);
                Bounds = BoundaryPath.Bounds;
            }
        }
    }
}
