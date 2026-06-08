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
    public sealed class DrawnPixelEdits : MapComponent2D, IDrawnMapComponent
    {
        private List<PixelEdit> _mapPixelEdits = [];

        [XmlArray]
        [XmlArrayItem("PixelEdit", Type = typeof(PixelEdit))]
        public List<PixelEdit> MapPixelEdits
        {
            get => _mapPixelEdits;
            set => _mapPixelEdits = value;
        }

        public override void Render(SKCanvas canvas, FontManager? fontManager = null)
        {
            using SKPaint paint = new()
            {
                IsAntialias = false,
                Style = SKPaintStyle.Fill
            };

            foreach (var edit in _mapPixelEdits)
            {
                paint.Color = edit.NewColor;

                canvas.DrawRect(
                    edit.Location.X,
                    edit.Location.Y,
                    1,
                    1,
                    paint);
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

    public class PixelEdit
    {
        [XmlElement]
        public SKPoint Location = new SKPoint();
        [XmlElement]
        public SKColor OriginalColor = SKColor.Empty;
        [XmlElement]
        public SKColor NewColor = SKColor.Empty;
    }
}
