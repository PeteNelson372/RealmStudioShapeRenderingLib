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
    public class MapWindrose : MapComponent2D
    {
        [XmlElement]
        public SKPoint Location { get; set; }

        [XmlIgnore]
        public SKColor WindroseColor { get; set; } = SKColor.Parse("#7F3D3728");

        [XmlElement("WindroseColor")]
        public string WindroseColorXml
        {
            get => XmlColorConverter.Serialize(WindroseColor);
            set => WindroseColor = XmlColorConverter.Deserialize(value);
        }

        [XmlElement]
        public int DirectionCount { get; set; } = 16;

        [XmlElement]
        public int LineWidth { get; set; } = 2;

        [XmlElement]
        public int InnerRadius { get; set; }

        [XmlElement]
        public int OuterRadius { get; set; } = 1000;

        [XmlElement]
        public int InnerCircles { get; set; }

        [XmlElement]
        public bool FadeOut { get; set; }

        [XmlIgnore]
        public SKPaint? _windrosePaint;

        public override void Render(SKCanvas canvas, FontManager? fontManager = null, SKPath? clipPath = null)
        {
            _windrosePaint = new()
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = LineWidth,
                Color = WindroseColor,
                IsAntialias = true,
            };

            using (new SKAutoCanvasRestore(canvas))
            {
                canvas.ClipRect(Bounds);

                // draw circles, if any
                if (InnerRadius > 0)
                {
                    DrawWindroseLines(canvas, true);

                    switch (InnerCircles)
                    {
                        case 1:
                            canvas.DrawCircle(Location.X, Location.Y, InnerRadius, _windrosePaint);
                            break;
                        case 2:
                            canvas.DrawCircle(Location.X, Location.Y, InnerRadius / 2, _windrosePaint);
                            canvas.DrawCircle(Location.X, Location.Y, InnerRadius, _windrosePaint);
                            break;
                    }
                }
                else
                {
                    DrawWindroseLines(canvas, false);
                }
            }
        }

        private void DrawWindroseLines(SKCanvas canvas, bool fromCircle)
        {
            float circleStep = 360.0F / DirectionCount;

            for (int i = 0; i < DirectionCount; i++)
            {
                SKPoint sp;

                if (fromCircle)
                {
                    // get the starting point on the perimeter of the inner circle
                    sp = Utilities.PointOnCircle(InnerRadius, i * circleStep, Location);
                }
                else
                {
                    // get the starting point at the cursor point
                    sp = Location;
                }

                // get the ending point at the outer radius
                SKPoint ep = Utilities.PointOnCircle(OuterRadius, i * circleStep, Location);

                if (FadeOut)
                {
                    DrawFadedLine(canvas, sp, ep);
                }
                else
                {
                    canvas.DrawLine(sp, ep, _windrosePaint);
                }
            }
        }

        private void DrawFadedLine(SKCanvas canvas, SKPoint sp, SKPoint ep)
        {
            if (_windrosePaint != null)
            {
                SKPoint[] linePoints = Utilities.GetPoints(254, sp, ep);
                int segmentOpacity = WindroseColor.Alpha;

                SKPaint fadedLinePaint = _windrosePaint.Clone();

                for (int j = 0; j < linePoints.Length - 1; j++)
                {
                    // draw line between points in linePoints, decreasing opacity
                    fadedLinePaint.Color = new SKColor(WindroseColor.Red, WindroseColor.Green, WindroseColor.Blue, (byte)segmentOpacity); ;

                    canvas.DrawLine(linePoints[j], linePoints[j + 1], fadedLinePaint);

                    segmentOpacity--;
                    segmentOpacity = Math.Max(segmentOpacity, 0);
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
