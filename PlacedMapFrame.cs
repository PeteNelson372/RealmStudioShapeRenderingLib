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
    public class PlacedMapFrame : MapComponent2D
    {
        [XmlElement]
        public MapFrame? FrameDefinition {  get; set; }

        [XmlIgnore]
        public bool FrameEnabled { get; set; } = true;

        [XmlIgnore]
        public SKColor FrameTint { get; set; } = SKColors.WhiteSmoke;

        [XmlElement("FrameTint")]
        public string FrameTintXml
        {
            get => XmlColorConverter.Serialize(FrameTint);
            set => FrameTint = XmlColorConverter.Deserialize(value);
        }

        [XmlElement]
        public float FrameScale { get; set; } = 1.0F;

        [XmlIgnore]
        public SKBitmap? PatchA { get; set; }   // top left corner

        [XmlIgnore]
        public SKBitmap? PatchB { get; set; }  // top middle

        [XmlIgnore]
        public SKBitmap? PatchC { get; set; }  // top right corner

        [XmlIgnore]
        public SKBitmap? PatchD { get; set; }  // left side

        [XmlIgnore]
        public SKBitmap? PatchE { get; set; }  // middle

        [XmlIgnore]
        public SKBitmap? PatchF { get; set; }  // right side

        [XmlIgnore]
        public SKBitmap? PatchG { get; set; }  // bottom left corner

        [XmlIgnore]
        public SKBitmap? PatchH { get; set; }  // bottom middle

        [XmlIgnore]
        public SKBitmap? PatchI { get; set; }  // bottom right corner

        public override IShapeState CaptureState()
        {
            throw new NotImplementedException();
        }

        public override bool HitTest(SKPoint worldPos)
        {
            return false;
        }

        public override void Render(SKCanvas canvas, FontManager? fontManager = null, SKPath? clipPath = null)
        {
            if (!FrameEnabled)
            {
                return;
            }

            if (PatchA == null
                || PatchB == null
                || PatchC == null
                || PatchD == null
                || PatchF == null
                || PatchG == null
                || PatchH == null
                || PatchI == null)
            {
                return;
            }

            using SKPaint paint = new()
            {
                Style = SKPaintStyle.Fill,
                IsAntialias = false,
                ColorFilter = SKColorFilter.CreateBlendMode(FrameTint, SKBlendMode.Modulate)
            };

            int width = (int)Bounds.Width;
            int height = (int)Bounds.Height;

            float scale =
                Math.Max(FrameScale, 0.01f);

            // -------------------------------------------------
            // Corner sizes
            // -------------------------------------------------

            int leftWidth =
                Math.Max(
                    (int)Math.Round(
                        PatchA.Width * scale),
                    1);

            int rightWidth =
                Math.Max(
                    (int)Math.Round(
                        PatchC.Width * scale),
                    1);

            int topHeight =
                Math.Max(
                    (int)Math.Round(
                        PatchA.Height * scale),
                    1);

            int bottomHeight =
                Math.Max(
                    (int)Math.Round(
                        PatchG.Height * scale),
                    1);

            // -------------------------------------------------
            // Draw corners
            // -------------------------------------------------

            DrawPatch(
                canvas,
                PatchA,
                new SKRectI(
                    0,
                    0,
                    leftWidth,
                    topHeight),
                paint);

            DrawPatch(
                canvas,
                PatchC,
                new SKRectI(
                    width - rightWidth,
                    0,
                    width,
                    topHeight),
                paint);

            DrawPatch(
                canvas,
                PatchG,
                new SKRectI(
                    0,
                    height - bottomHeight,
                    leftWidth,
                    height),
                paint);

            DrawPatch(
                canvas,
                PatchI,
                new SKRectI(
                    width - rightWidth,
                    height - bottomHeight,
                    width,
                    height),
                paint);

            // -------------------------------------------------
            // Top edge
            // -------------------------------------------------

            DrawHorizontalTiledEdge(
                canvas,
                PatchB,
                leftWidth,
                0,
                width - leftWidth - rightWidth,
                topHeight,
                paint);

            // -------------------------------------------------
            // Bottom edge
            // -------------------------------------------------

            DrawHorizontalTiledEdge(
                canvas,
                PatchH,
                leftWidth,
                height - bottomHeight,
                width - leftWidth - rightWidth,
                bottomHeight,
                paint);

            // -------------------------------------------------
            // Left edge
            // -------------------------------------------------

            DrawVerticalTiledEdge(
                canvas,
                PatchD,
                0,
                topHeight,
                leftWidth,
                height - topHeight - bottomHeight,
                paint);

            // -------------------------------------------------
            // Right edge
            // -------------------------------------------------

            DrawVerticalTiledEdge(
                canvas,
                PatchF,
                width - rightWidth,
                topHeight,
                rightWidth,
                height - topHeight - bottomHeight,
                paint);

            // -------------------------------------------------
            // Optional center
            // -------------------------------------------------

            if (PatchE != null)
            {
                DrawTiledCenter(
                    canvas,
                    PatchE,
                    leftWidth,
                    topHeight,
                    width - leftWidth - rightWidth,
                    height - topHeight - bottomHeight,
                    paint);
            }
        }

        private static void DrawPatch(
            SKCanvas canvas,
            SKBitmap bitmap,
            SKRectI dest,
            SKPaint paint)
        {
            canvas.DrawBitmap(
                bitmap,
                dest,
                paint);
        }

        private static void DrawHorizontalTiledEdge(
            SKCanvas canvas,
            SKBitmap bitmap,
            int x,
            int y,
            int width,
            int height,
            SKPaint paint)
        {
            if (width <= 0 || height <= 0)
            {
                return;
            }

            int tileCount =
                Math.Max(
                    (int)Math.Round(
                        (float)width / bitmap.Width),
                    1);

            int tileWidth =
                (int)Math.Ceiling(
                    (float)width / tileCount);

            int left = x;

            for (int i = 0; i < tileCount; i++)
            {
                int right =
                    (i == tileCount - 1)
                    ? x + width
                    : left + tileWidth;

                SKRectI dest = new(
                    left,
                    y,
                    right,
                    y + height);

                canvas.DrawBitmap(
                    bitmap,
                    dest,
                    paint);

                left = right;
            }
        }

        private static void DrawVerticalTiledEdge(
            SKCanvas canvas,
            SKBitmap bitmap,
            int x,
            int y,
            int width,
            int height,
            SKPaint paint)
        {
            if (width <= 0 || height <= 0)
            {
                return;
            }

            int tileCount =
                Math.Max(
                    (int)Math.Round(
                        (float)height / bitmap.Height),
                    1);

            int tileHeight =
                (int)Math.Ceiling(
                    (float)height / tileCount);

            int top = y;

            for (int i = 0; i < tileCount; i++)
            {
                int bottom =
                    (i == tileCount - 1)
                    ? y + height
                    : top + tileHeight;

                SKRectI dest = new(
                    x,
                    top,
                    x + width,
                    bottom);

                canvas.DrawBitmap(
                    bitmap,
                    dest,
                    paint);

                top = bottom;
            }
        }

        private static void DrawTiledCenter(
            SKCanvas canvas,
            SKBitmap bitmap,
            int x,
            int y,
            int width,
            int height,
            SKPaint paint)
        {
            if (width <= 0 || height <= 0)
            {
                return;
            }

            int cols =
                Math.Max(
                    (int)Math.Round(
                        (float)width / bitmap.Width),
                    1);

            int rows =
                Math.Max(
                    (int)Math.Round(
                        (float)height / bitmap.Height),
                    1);

            int tileWidth =
                (int)Math.Ceiling(
                    (float)width / cols);

            int tileHeight =
                (int)Math.Ceiling(
                    (float)height / rows);

            int top = y;

            for (int row = 0; row < rows; row++)
            {
                int bottom =
                    (row == rows - 1)
                    ? y + height
                    : top + tileHeight;

                int left = x;

                for (int col = 0; col < cols; col++)
                {
                    int right =
                        (col == cols - 1)
                        ? x + width
                        : left + tileWidth;

                    SKRectI dest = new(
                        left,
                        top,
                        right,
                        bottom);

                    canvas.DrawBitmap(
                        bitmap,
                        dest,
                        paint);

                    left = right;
                }

                top = bottom;
            }
        }

        public override void RestoreState(IShapeState state)
        {
            throw new NotImplementedException();
        }
    }
}
