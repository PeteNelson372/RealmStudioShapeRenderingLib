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
using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public class PlacedMapBox : MapComponent2D, ITransformable2D
    {
        public MapBox? BaseBox;

        public Guid BoxGuid { get; set; } = Guid.NewGuid();

        public SKBitmap? BoxBitmap { get; set; }

        public SKColor BoxTint { get; set; } = SKColors.White;

        public SKPaint? BoxPaint { get; set; }

        public float BoxCenterLeft { get; set; }
        public float BoxCenterTop { get; set; }
        public float BoxCenterRight { get; set; }
        public float BoxCenterBottom { get; set; }

        public SKPoint Location { get; set; }
        public float Rotation { get; set; }
        public float Scale { get; set; } = 1f;
        public bool Mirror { get; set; }

        public PlacedMapBox() { }

        public PlacedMapBox(PlacedMapBox box)
        {
            BaseBox = box.BaseBox;
            BoxBitmap = box.BoxBitmap?.Copy();
            BoxTint = box.BoxTint;
            BoxPaint = box.BoxPaint?.Clone();
            BoxCenterLeft = box.BoxCenterLeft;
            BoxCenterTop = box.BoxCenterTop;
            BoxCenterRight = box.BoxCenterRight;
            BoxCenterBottom = box.BoxCenterBottom;
        }

        public void SetBoxBitmap(SKBitmap b)
        {
            BoxBitmap = b;
        }

        private void GetBoxCenterFromMapBox()
        {
            if (BoxBitmap == null) { return; }
        }

        public override void Render(SKCanvas canvas, FontManager? fontManager = null)
        {
            try
            {
                // the box center can be outside the bounds of the bitmap if
                // the box is drawn to be very narrow in height or width
                if (BoxBitmap != null)
                {
                    canvas.DrawBitmapNinePatch(BoxBitmap,
                        new SKRectI((int)BoxCenterLeft, (int)BoxCenterTop, (int)BoxCenterRight, (int)BoxCenterBottom),
                        Bounds,
                        BoxPaint);

                    if (IsSelected)
                    {
                        canvas.DrawRect(Bounds, PaintObjects.BoxSelectPaint);
                    }
                }
            }
            catch { }
        }

        public override bool HitTest(SKPoint worldPos)
        {
            throw new NotImplementedException();
        }

        public override IShapeState CaptureState()
        {
            throw new NotImplementedException();
        }

        public override void RestoreState(IShapeState state)
        {
            throw new NotImplementedException();
        }

        public SKRect GetLocalBounds()
        {
            throw new NotImplementedException();
        }

        public SKPoint[] GetTransformedCorners()
        {
            throw new NotImplementedException();
        }

        public void BeginScale()
        {
            throw new NotImplementedException();
        }

        public void ApplyScale(float factor)
        {
            throw new NotImplementedException();
        }
    }
}
