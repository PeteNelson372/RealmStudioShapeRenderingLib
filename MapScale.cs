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
    public class MapScale : MapComponent2D, IDisposable
    {
        [XmlElement]
        public int ScaleWidth { get; set; } = 256;

        [XmlElement]
        public int ScaleHeight { get; set; } = 16;

        [XmlElement]
        public SKPoint Location { get; set; } = SKPoint.Empty;

        [XmlElement]
        public int ScaleSegmentCount { get; set; } = 5;  // how many segments in the scale

        [XmlElement]
        public int ScaleLineWidth { get; set; } = 3;  // width of the outline around the scale

        [XmlIgnore]
        public SKColor ScaleColor1 { get; set; } = SKColors.Black;  // odd numbered segment color

        [XmlElement("ScaleColor1")]
        public string ScaleColor1Xml
        {
            get => XmlColorConverter.Serialize(ScaleColor1);
            set => ScaleColor1 = XmlColorConverter.Deserialize(value);
        }

        [XmlIgnore]
        public SKColor ScaleColor2 { get; set; } = SKColors.White;  // even numbered segment color

        [XmlElement("ScaleColor2")]
        public string ScaleColor2Xml
        {
            get => XmlColorConverter.Serialize(ScaleColor2);
            set => ScaleColor2 = XmlColorConverter.Deserialize(value);
        }

        [XmlIgnore]
        public SKColor ScaleColor3 { get; set; } = SKColors.Black;  // line color of scale outline

        [XmlElement("ScaleColor3")]
        public string ScaleColor3Xml
        {
            get => XmlColorConverter.Serialize(ScaleColor3);
            set => ScaleColor3 = XmlColorConverter.Deserialize(value);
        }

        [XmlElement]
        public float ScaleDistance { get; set; } = 100.0F;  // distance of each segment

        [XmlElement]
        public string ScaleDistanceUnit { get; set; } = string.Empty;  // feet, meters, miles, kilometers, etc.
        
        [XmlElement]
        public ScaleNumbersDisplayLocation ScaleNumbersDisplayType { get; set; } = ScaleNumbersDisplayLocation.All;  // where to display the segment labels

        [XmlElement]
        public FontStyleModel ScaleFont { get; set; } = new FontStyleModel(); // scale segment label font, color, outline

        [XmlIgnore]
        public SKColor ScaleFontColor { get; set; } = SKColors.White;

        [XmlElement("ScaleFontColor")]
        public string ScaleFontColorXml
        {
            get => XmlColorConverter.Serialize(ScaleFontColor);
            set => ScaleFontColor = XmlColorConverter.Deserialize(value);
        }

        [XmlElement]
        public int ScaleOutlineWidth { get; set; } = 1;

        [XmlIgnore]
        public SKColor ScaleOutlineColor { get; set; } = SKColors.Black;

        [XmlElement("ScaleOutlineColor")]
        public string ScaleOutlineColorXml
        {
            get => XmlColorConverter.Serialize(ScaleOutlineColor);
            set => ScaleOutlineColor = XmlColorConverter.Deserialize(value);
        }

        private SKPaint SegmentOutlinePaint = new();
        private SKPaint EvenSegmentPaint = new();
        private SKPaint OddSegmentPaint = new();
        private SKPaint ScaleLabelPaint = new();
        private SKPaint OutlinePaint = new();

        private bool disposedValue;

        public MapScale(){ }

        public void ConstructPaintObjects()
        {
            SegmentOutlinePaint.Dispose();
            SegmentOutlinePaint = new()
            {
                Style = SKPaintStyle.StrokeAndFill,
                StrokeWidth = ScaleLineWidth,
                Color = ScaleColor3
            };

            EvenSegmentPaint.Dispose();
            EvenSegmentPaint = new()
            {
                Style = SKPaintStyle.StrokeAndFill,
                StrokeWidth = ScaleHeight - ScaleLineWidth,
                Color = ScaleColor1
            };

            OddSegmentPaint.Dispose();
            OddSegmentPaint = new()
            {
                Style = SKPaintStyle.StrokeAndFill,
                StrokeWidth = ScaleHeight - ScaleLineWidth,
                Color = ScaleColor2
            };

            ScaleLabelPaint.Dispose();
            ScaleLabelPaint = new()
            {
                Color = ScaleFontColor,
                IsAntialias = true
            };

            OutlinePaint.Dispose();
            OutlinePaint = new()
            {
                Color = ScaleOutlineColor,
                IsAntialias = true,
                ImageFilter = SKImageFilter.CreateDilate(ScaleOutlineWidth, ScaleOutlineWidth),
            };
        }

        public override bool HitTest(SKPoint worldPos)
        {
            return Bounds.Contains(worldPos);
        }

        public override IShapeState CaptureState()
        {
            throw new NotImplementedException();
        }

        public override void RestoreState(IShapeState state)
        {
            throw new NotImplementedException();
        }

        public override void Render(SKCanvas canvas, FontManager? fontManager = null, SKPath? clipPath = null)
        {
            ConstructPaintObjects();

            SKFontStyle fs = SKFontStyle.Normal;

            if (ScaleFont.Bold && ScaleFont.Italic)
            {
                fs = SKFontStyle.BoldItalic;
            }
            else if (ScaleFont.Bold)
            {
                fs = SKFontStyle.Bold;
            }
            else if (ScaleFont.Italic)
            {
                fs = SKFontStyle.Italic;
            }

            SKTypeface fontTypeface = SKTypeface.FromFamilyName(ScaleFont.Family, fs);

            SKFont skScaleFont = new(fontTypeface)
            {
                Size = ScaleFont.Size,
            };

            float segmentWidth = ScaleWidth / ScaleSegmentCount;

            // draw the scale outline
            SKRect outlineRect = Bounds;

            canvas.DrawRect(outlineRect, SegmentOutlinePaint);

            // draw the segments and labels
            int segmentX = (int)(Bounds.Left + (ScaleLineWidth / 2));
            int segmentY = (int)(Bounds.Top + (ScaleHeight / 2));

            for (int i = 0; i < ScaleSegmentCount; i++)
            {
                SKPoint sp = new(segmentX, segmentY);
                SKPoint ep = new(segmentX + segmentWidth, segmentY);

                if (int.IsEvenInteger(i))
                {
                    canvas.DrawLine(sp, ep, EvenSegmentPaint);
                }
                else
                {
                    canvas.DrawLine(sp, ep, OddSegmentPaint);
                }

                segmentX = (int)(segmentX + segmentWidth);
            }

            // draw the distance labels
            int labelX = (int)(Bounds.Left + (ScaleLineWidth / 2));
            int labelY = (int)Bounds.Top;

            float distance = 0;

            for (int i = 0; i <= ScaleSegmentCount; i++)
            {
                string distanceText = string.Format("{0}", (int)distance);

                skScaleFont.MeasureText(distanceText, out SKRect bounds, ScaleLabelPaint);

                SKPoint labelPoint = new(labelX, labelY - bounds.Height);

                if (ScaleOutlineWidth == 0)
                {
                    ScaleLabelPaint.Color = ScaleOutlineColor;
                }

                switch (ScaleNumbersDisplayType)
                {
                    case ScaleNumbersDisplayLocation.Ends:
                        {
                            if (i == 0 || i == ScaleSegmentCount)
                            {
                                SKPoint drawPoint = new(labelPoint.X - (bounds.Width / 2f), labelPoint.Y);
                                canvas.DrawText(distanceText, drawPoint, SKTextAlign.Center, skScaleFont, OutlinePaint);
                                canvas.DrawText(distanceText, drawPoint, SKTextAlign.Center, skScaleFont, ScaleLabelPaint);
                            }
                        }
                        break;
                    case ScaleNumbersDisplayLocation.EveryOther:
                        {
                            if (int.IsEvenInteger(i))
                            {
                                SKPoint drawPoint = new(labelPoint.X - (bounds.Width / 2f), labelPoint.Y);
                                canvas.DrawText(distanceText, drawPoint, SKTextAlign.Center, skScaleFont, OutlinePaint);
                                canvas.DrawText(distanceText, drawPoint, SKTextAlign.Center, skScaleFont, ScaleLabelPaint);
                            }
                        }
                        break;
                    case ScaleNumbersDisplayLocation.All:
                        {
                            SKPoint drawPoint = new(labelPoint.X - (bounds.Width / 2f), labelPoint.Y);
                            canvas.DrawText(distanceText, drawPoint, SKTextAlign.Center, skScaleFont, OutlinePaint);
                            canvas.DrawText(distanceText, drawPoint, SKTextAlign.Center, skScaleFont, ScaleLabelPaint);
                        }
                        break;
                }

                distance += ScaleDistance;
                labelX = (int)(labelX + segmentWidth);
            }

            if (!string.IsNullOrEmpty(ScaleDistanceUnit))
            {
                // draw the scale unit label
                skScaleFont.MeasureText(ScaleDistanceUnit, out SKRect bounds, ScaleLabelPaint);

                int unitLabelX = (int)(Bounds.Left + (Bounds.Width / 2) - (bounds.Width / 2.0F));
                int unitLabelY = (int)(Bounds.Top + Bounds.Height);

                unitLabelY = (int)(unitLabelY + bounds.Height * 2);

                SKPoint unitLabelPoint = new(unitLabelX, unitLabelY);
                canvas.DrawText(ScaleDistanceUnit, unitLabelPoint, SKTextAlign.Center, skScaleFont, OutlinePaint);
                canvas.DrawText(ScaleDistanceUnit, unitLabelPoint, SKTextAlign.Center, skScaleFont, ScaleLabelPaint);
            }

        }

        public SKRect GetLocalBounds()
        {
            return new SKRect(0, 0, ScaleWidth, ScaleHeight);
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    SegmentOutlinePaint.Dispose();
                    EvenSegmentPaint.Dispose();
                    OddSegmentPaint.Dispose();
                    ScaleLabelPaint.Dispose();
                    OutlinePaint.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }


    }
}
