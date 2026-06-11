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
using RealmStudioX.WPF.EditorUtilities;
using SkiaSharp;
using System.Xml.Serialization;

namespace RealmStudioShapeRenderingLib
{
    public class MapGrid : MapComponent2D
    {
        [XmlElement]
        public bool GridEnabled { get; set; }

        [XmlElement]
        public MapGridType GridType { get; set; } = MapGridType.Square;

        [XmlIgnore]
        public SKColor GridColor { get; set; } = new SKColor(0, 0, 0, 126);

        [XmlElement("GridColor")]
        public string GridColorXml
        {
            get => XmlColorConverter.Serialize(GridColor);
            set => GridColor = XmlColorConverter.Deserialize(value);
        }

        [XmlElement]
        public int GridLayerIndex { get; set; } = MapBuilder.DEFAULTGRIDLAYER;

        [XmlElement]
        public int GridSize { get; set; } = 64;

        [XmlElement]
        public int GridLineWidth { get; set; } = 2;

        [XmlElement]
        public bool ShowGridSize { get; set; } = true;

        [XmlElement]
        public float MapAreaWidth { get; set; } = 0f;

        [XmlElement]
        public float MapAreaHeight { get; set; } = 0f;

        [XmlElement]
        public string MapAreaUnits { get; set; } = "pixels";

        public MapGrid() { }

        public override void Render(SKCanvas canvas, FontManager? fontManager = null)
        {
            if (GridEnabled)
            {
                switch (GridType)
                {
                    case MapGridType.Square:
                        RenderSquareGrid(canvas);
                        break;

                    case MapGridType.PointedHex:
                        RenderPointedHexGrid(canvas);
                        break;

                    case MapGridType.FlatHex:
                        RenderFlatHexGrid(canvas);
                        break;
                }
            }
        }

        private void RenderSquareGrid(SKCanvas canvas)
        {
            int numHorizontalLines = (int)(Bounds.Height / GridSize);
            int numVerticalLines = (int)(Bounds.Width / GridSize);

            int xOffset = GridSize;
            int yOffset = GridSize;

            using (new SKAutoCanvasRestore(canvas))
            {
                using SKPaint gridPaint = new()
                {
                    Style = SKPaintStyle.Stroke,
                    Color = GridColor,
                    StrokeWidth = GridLineWidth,
                    StrokeJoin = SKStrokeJoin.Bevel
                };

                canvas.ClipRect(new SKRect(0, 0, Bounds.Width, Bounds.Height));

                for (int i = 0; i < numHorizontalLines; i++)
                {
                    SKPoint startPoint = new(0, yOffset);
                    SKPoint endPoint = new(Bounds.Width, yOffset);
                    canvas.DrawLine(startPoint, endPoint, gridPaint);

                    yOffset += GridSize;
                }

                for (int j = 0; j < numVerticalLines; j++)
                {
                    SKPoint startPoint = new(xOffset, 0);
                    SKPoint endPoint = new(xOffset, Bounds.Height);
                    canvas.DrawLine(startPoint, endPoint, gridPaint);

                    xOffset += GridSize;
                }

                if (ShowGridSize)
                {
                    float horizontalGridDistance = MapAreaWidth / numVerticalLines;
                    float verticalGridDistance = MapAreaHeight / numHorizontalLines;

                    string mapUnits = MapAreaUnits;

                    if (string.IsNullOrEmpty(mapUnits) || mapUnits.Length == 0)
                    {
                        mapUnits = "pixels";
                    }

                    string gridScaleString = string.Format("One square is {0:N} by {1:N} {2}", horizontalGridDistance, verticalGridDistance, mapUnits);

                    using SKTypeface gridFontTypeFace = SKTypeface.FromFamilyName(familyName: "Tahoma", weight: SKFontStyleWeight.Normal,
                        width: SKFontStyleWidth.Normal, slant: SKFontStyleSlant.Upright);

                    using SKFont gridLabelFont = new(gridFontTypeFace, 10);

                    using SKPaint glowPaint = new()
                    {
                        Color = SKColors.Yellow,
                        MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Outer, 10),
                        IsAntialias = true,
                    };

                    canvas.DrawText(gridScaleString, 20, Bounds.Height - 40, gridLabelFont, glowPaint);

                    using SKPaint paint = new()
                    {
                        Color = SKColors.Black,
                        IsAntialias = true,
                    };

                    canvas.DrawText(gridScaleString, 20, Bounds.Height - 40, gridLabelFont, paint);
                }

            }
        }

        private void RenderPointedHexGrid(SKCanvas canvas)
        {
            float hexwidth = GridSize * (float)Math.Sqrt(3.0);
            float hexheight = GridSize * 2.0F;
            float horizontalSpacing = hexwidth;
            float verticalSpacing = hexheight * 0.75F;

            // tile the grid onto the map
            SKPoint center = new(hexwidth, hexheight);

            int numHorizontalHexagons = (int)(Bounds.Width / horizontalSpacing) + 2;
            int numVerticalHexagons = (int)(Bounds.Height / verticalSpacing) + 2;

            using (new SKAutoCanvasRestore(canvas))
            {
                using SKPaint gridPaint = new()
                {
                    Style = SKPaintStyle.Stroke,
                    Color = GridColor,
                    StrokeWidth = GridLineWidth,
                    StrokeJoin = SKStrokeJoin.Bevel
                };

                canvas.ClipRect(new SKRect(0, 0, Bounds.Width, Bounds.Height));

                for (int i = 0; i < numVerticalHexagons; i++)
                {
                    for (int j = 0; j < numHorizontalHexagons; j++)
                    {
                        SKPoint[] hexPoints = new SKPoint[6];

                        center.X = j * horizontalSpacing;
                        center.Y = i * verticalSpacing;

                        for (int k = 0; k < 6; k++)
                        {
                            float angle_deg = (60.0F * k) + 30.0F;
                            float angle_rad = (float)(Math.PI / 180.0F * angle_deg);

                            if (int.IsOddInteger(i))
                            {
                                hexPoints[k] = new SKPoint((float)(center.X + (horizontalSpacing / 2.0F) + GridSize * Math.Cos(angle_rad)),
                                    (float)(center.Y + GridSize * Math.Sin(angle_rad)));
                            }
                            else
                            {
                                hexPoints[k] = new SKPoint((float)(center.X + GridSize * Math.Cos(angle_rad)),
                                    (float)(center.Y + GridSize * Math.Sin(angle_rad)));
                            }

                        }

                        canvas.DrawPoints(SKPointMode.Lines, hexPoints, gridPaint);
                    }
                }

                if (ShowGridSize)
                {
                    float horizontalGridDistance = MapAreaWidth * hexwidth;
                    float verticalGridDistance = MapAreaHeight * hexheight;

                    string mapUnits = MapAreaUnits;

                    if (string.IsNullOrEmpty(mapUnits) || mapUnits.Length == 0)
                    {
                        mapUnits = "pixels";
                    }

                    string gridScaleString = string.Format("One hexagon is {0:N} by {1:N} {2}", horizontalGridDistance, verticalGridDistance, mapUnits);

                    using SKTypeface gridFontTypeFace = SKTypeface.FromFamilyName(familyName: "Tahoma", weight: SKFontStyleWeight.Normal,
                        width: SKFontStyleWidth.Normal, slant: SKFontStyleSlant.Upright);

                    using SKFont gridLabelFont = new(gridFontTypeFace, 10);

                    using SKPaint glowPaint = new()
                    {
                        Color = SKColors.Yellow,
                        MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Outer, 10),
                        IsAntialias = true,
                    };

                    canvas.DrawText(gridScaleString, 20, Bounds.Height - 40, gridLabelFont, glowPaint);

                    using SKPaint paint = new()
                    {
                        Color = SKColors.Black,
                        IsAntialias = true,
                    };

                    canvas.DrawText(gridScaleString, 20, Bounds.Height - 40, gridLabelFont, paint);
                }
            }
        }

        private void RenderFlatHexGrid(SKCanvas canvas)
        {
            float hexwidth = GridSize * 2.0F;
            float hexheight = GridSize * (float)Math.Sqrt(3.0);
            float horizontalSpacing = hexwidth * 0.75F;
            float verticalSpacing = hexheight;

            using (new SKAutoCanvasRestore(canvas))
            {
                using SKPaint gridPaint = new()
                {
                    Style = SKPaintStyle.Stroke,
                    Color = GridColor,
                    StrokeWidth = GridLineWidth,
                    StrokeJoin = SKStrokeJoin.Bevel
                };

                canvas.ClipRect(new SKRect(0, 0, Bounds.Width, Bounds.Height));

                // tile the grid onto the map
                SKPoint center = new(hexwidth, hexheight);

                int numHorizontalHexagons = (int)(Bounds.Width / horizontalSpacing) + 2;
                int numVerticalHexagons = (int)(Bounds.Height / verticalSpacing) + 2;

                for (int i = 0; i < numVerticalHexagons; i++)
                {
                    for (int j = 0; j < numHorizontalHexagons; j++)
                    {
                        SKPoint[] hexPoints = new SKPoint[6];

                        center.X = j * horizontalSpacing;
                        center.Y = i * verticalSpacing;

                        for (int k = 0; k < 6; k++)
                        {
                            float angle_deg = 60.0F * k;
                            float angle_rad = (float)(Math.PI / 180.0F * angle_deg);

                            if (int.IsOddInteger(j))
                            {
                                hexPoints[k] = new SKPoint((float)(center.X + GridSize * Math.Cos(angle_rad)),
                                    (float)(center.Y + (verticalSpacing / 2.0F) + GridSize * Math.Sin(angle_rad)));
                            }
                            else
                            {
                                hexPoints[k] = new SKPoint((float)(center.X + GridSize * Math.Cos(angle_rad)),
                                    (float)(center.Y + GridSize * Math.Sin(angle_rad)));
                            }

                        }

                        canvas.DrawPoints(SKPointMode.Lines, hexPoints, gridPaint);
                    }
                }

                if (ShowGridSize)
                {
                    float horizontalGridDistance = MapAreaWidth * hexwidth;
                    float verticalGridDistance = MapAreaHeight * hexheight;

                    string mapUnits = MapAreaUnits;

                    if (string.IsNullOrEmpty(mapUnits) || mapUnits.Length == 0)
                    {
                        mapUnits = "pixels";
                    }

                    string gridScaleString = string.Format("One hexagon is {0:N} by {1:N} {2}", horizontalGridDistance, verticalGridDistance, mapUnits);

                    using SKTypeface gridFontTypeFace = SKTypeface.FromFamilyName(familyName: "Tahoma", weight: SKFontStyleWeight.Normal,
                        width: SKFontStyleWidth.Normal, slant: SKFontStyleSlant.Upright);

                    using SKFont gridLabelFont = new(gridFontTypeFace, 10);

                    using SKPaint glowPaint = new()
                    {
                        Color = SKColors.Yellow,
                        MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Outer, 10),
                        IsAntialias = true,
                    };

                    canvas.DrawText(gridScaleString, 20, Bounds.Height - 40, gridLabelFont, glowPaint);

                    using SKPaint paint = new()
                    {
                        Color = SKColors.Black,
                        IsAntialias = true,
                    };

                    canvas.DrawText(gridScaleString, 20, Bounds.Height - 40, gridLabelFont, paint);
                }

            }
        }

        public override bool HitTest(SKPoint worldPos)
        {
            return false;
        }

        public override IShapeState CaptureState()
        {
            throw new NotImplementedException();
        }

        public override void RestoreState(IShapeState state)
        {
            throw new NotImplementedException();
        }
    }
}
