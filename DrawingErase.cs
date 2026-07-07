/**************************************************************************************************************************
* Copyright 2025, Peter R. Nelson
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
using SkiaSharp;
using System.Xml.Serialization;

namespace RealmStudioShapeRenderingLib
{
    public sealed class DrawingErase : MapComponent2D, IDrawnMapComponent
    {
        private int _brushSize = 2;
        private List<SKPoint> _points = [];

        private readonly SKPaint _erasePaint = new()
        {
            Color = SKColors.Transparent,
            IsAntialias = true,
            BlendMode = SKBlendMode.Clear
        };

        [XmlIgnore]
        public List<SKPoint> Points
        {
            get => _points;
            set
            {
                _points = value ?? throw new ArgumentNullException(nameof(value), "Points cannot be null.");
            }
        }

        [XmlElement("Points")]
        public string PointsList
        {
            get => string.Join(";", Points.Select(p => $"{p.X},{p.Y}"));

            set
            {
                Points.Clear();

                if (string.IsNullOrWhiteSpace(value))
                {
                    return;
                }

                foreach (string pair in value.Split(';'))
                {
                    string[] parts = pair.Split(',');

                    Points.Add(new SKPoint(float.Parse(parts[0]), float.Parse(parts[1])));
                }
            }
        }

        [XmlElement]
        public int BrushSize
        {
            get => _brushSize;
            set
            {
                _brushSize = value;
            }
        }

        public void AddPoint(SKPoint point)
        {
            Points.Add(point);
            using SKPath p = Utilities.BuildPath(Points);
            Bounds = p.Bounds;
        }

        public override void Render(SKCanvas canvas, FontManager? fontManager = null, SKPath? clipPath = null)
        {
            foreach (SKPoint erasePoint in Points)
            {
                canvas.DrawCircle(erasePoint, BrushSize / 2, _erasePaint);
            }
        }

        public override bool HitTest(SKPoint worldPos)
        {
            // a DrawingErase object is not selectable
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
