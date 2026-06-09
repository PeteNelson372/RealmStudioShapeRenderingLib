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
    public sealed class MapMeasure : MapComponent2D
    {
        public MapMeasure()
        {            
            MeasureLinePaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1,
                Color = MeasureLineColor
            };

            MeasureAreaPaint = new SKPaint
            {
                Style = SKPaintStyle.StrokeAndFill,
                StrokeWidth = 1,
                Color = MeasureLineColor
            };

            MeasureValuePaint = new()
            {
                Color = SKColors.White,
                IsAntialias = true
            };

            MeasureValueOutlinePaint = new()
            {
                Color = SKColors.Black,
                IsAntialias = true,
                ImageFilter = SKImageFilter.CreateDilate(1, 1)
            };
        }

        [XmlElement]
        public float MapPixelWidth { get; set; } = 0f;

        [XmlElement]
        public float MapPixelHeight { get; set; } = 0f;

        [XmlElement]
        public string MapAreaUnits { get; set; } = "pixels";

        [XmlIgnore]
        public SKColor MeasureLineColor { get; set; } = new SKColor(138, 26, 0, 191);

        [XmlElement("MeasureLineColor")]
        public string MeasureLineColorXml
        {
            get => XmlColorConverter.Serialize(MeasureLineColor);
            set => MeasureLineColor = XmlColorConverter.Deserialize(value);
        }

        [XmlElement]
        public bool UseMapUnits { get; set; }

        [XmlElement]
        public bool MeasureArea { get; set; }

        [XmlIgnore]
        public List<SKPoint> MeasurePoints { get; set; } = [];

        [XmlElement("MeasurePoints")]
        public string PointsList
        {
            get => string.Join(";", MeasurePoints.Select(p => $"{p.X},{p.Y}"));

            set
            {
                MeasurePoints.Clear();

                if (string.IsNullOrWhiteSpace(value))
                {
                    return;
                }

                foreach (string pair in value.Split(';'))
                {
                    string[] parts = pair.Split(',');

                    MeasurePoints.Add(new SKPoint(float.Parse(parts[0]), float.Parse(parts[1])));
                }
            }
        }

        [XmlIgnore]
        public SKPaint MeasureLinePaint { get; set; }

        [XmlIgnore]
        public SKPaint MeasureAreaPaint { get; set; }

        [XmlIgnore]
        public SKPaint MeasureValuePaint { get; set; }

        [XmlIgnore]
        public SKPaint MeasureValueOutlinePaint { get; set; }

        [XmlElement]
        public float TotalMeasureLength { get; set; }

        [XmlElement]
        public bool RenderValue { get; set; }

        public override void Render(SKCanvas canvas, FontManager? fontManager = null)
        {
            MeasureLinePaint.Color = MeasureLineColor;
            MeasureAreaPaint.Color = MeasureLineColor;

            if (MeasurePoints.Count >= 2)
            {
                if (MeasureArea && MeasurePoints.Count > 2)
                {
                    SKPath path = new();

                    path.MoveTo(MeasurePoints.First());

                    for (int i = 1; i < MeasurePoints.Count; i++)
                    {
                        path.LineTo(MeasurePoints[i]);
                    }

                    path.Close();

                    canvas.DrawPath(path, MeasureAreaPaint);
                }
                else
                {
                    for (int i = 0; i < MeasurePoints.Count - 1; i++)
                    {
                        canvas.DrawLine(MeasurePoints[i], MeasurePoints[i + 1], MeasureLinePaint);
                    }
                }

                if (RenderValue)
                {
                    // render measure value and units
                    SKPoint measureValuePoint = new(MeasurePoints.Last().X + 30, MeasurePoints.Last().Y + 20);
                    RenderDistanceLabel(canvas, measureValuePoint, TotalMeasureLength);

                    if (MeasureArea)
                    {
                        float area = Utilities.CalculatePolygonArea(MeasurePoints);

                        SKPoint measureAreaPoint = new(MeasurePoints.Last().X + 30, MeasurePoints.Last().Y + 40);

                        RenderAreaLabel(canvas, measureAreaPoint, area);
                    }
                }
            }
        }

        public void RenderDistanceLabel(SKCanvas? canvas, SKPoint labelPoint, float distance)
        {
            SKFont measureValueSkFont = new(SKTypeface.FromFamilyName("Segoe UI"), 12);

            if (UseMapUnits && !string.IsNullOrEmpty(MapAreaUnits))
            {
                string lblText = string.Format("{0} {1}", (int)(distance * MapPixelWidth), MapAreaUnits);

                canvas?.DrawText(lblText, labelPoint, SKTextAlign.Center, measureValueSkFont, MeasureValueOutlinePaint);
                canvas?.DrawText(lblText, labelPoint, SKTextAlign.Center, measureValueSkFont, MeasureValuePaint);
            }
            else
            {
                string lblText = string.Format("{0} {1}", (int)distance, "pixels");

                canvas?.DrawText(lblText, labelPoint, SKTextAlign.Center, measureValueSkFont, MeasureValueOutlinePaint);
                canvas?.DrawText(lblText, labelPoint, SKTextAlign.Center, measureValueSkFont, MeasureValuePaint);
            }
        }

        public void RenderAreaLabel(SKCanvas? canvas, SKPoint labelPoint, float measuredArea)
        {
            SKFont measureValueSkFont = new(SKTypeface.FromFamilyName("Segoe UI"), 12);

            if (UseMapUnits && !string.IsNullOrEmpty(MapAreaUnits))
            {
                string labelString = string.Format("{0} {1}", (int)(measuredArea * (MapPixelWidth * MapPixelWidth)), MapAreaUnits + "\xB2");

                canvas?.DrawText(labelString, labelPoint, SKTextAlign.Center, measureValueSkFont, MeasureValueOutlinePaint);
                canvas?.DrawText(labelString, labelPoint, SKTextAlign.Center, measureValueSkFont, MeasureValuePaint);
            }
            else
            {
                string labelString = string.Format("{0} {1}", (int)measuredArea, "pixels\xB2");

                canvas?.DrawText(labelString, labelPoint, SKTextAlign.Center, measureValueSkFont, MeasureValueOutlinePaint);
                canvas?.DrawText(labelString, labelPoint, SKTextAlign.Center, measureValueSkFont, MeasureValuePaint);
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
