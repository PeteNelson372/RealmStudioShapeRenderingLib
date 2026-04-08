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
using RealmStudioX;
using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public class MapLabel : MapComponent2D
    {
        public string Text { get; set; } = string.Empty;

        public SKPoint Location { get; set; }
        public float Rotation { get; set; }
        public float Scale { get; set; } = 1f;
        public bool Mirror { get; set; }

        public FontStyleModel FontStyle { get; set; } = new();

        public SKColor FontColor { get; set; } = SKColors.White;

        public bool HasOutline { get; set; }
        public float OutlineWidth { get; set; }
        public SKColor OutlineColor { get; set; }

        public bool HasGlow { get; set; }
        public float GlowStrength { get; set; }
        public SKColor GlowColor { get; set; }

        // path/curve data
        public SKPath? CurvePath { get; set; }

        // Optional: arc parameters
        public float ArcRadius { get; set; }
        public float ArcAngle { get; set; }

        public MapLabel() { }

        public override void Render(SKCanvas canvas, FontManager? fontManager)
        {
            ArgumentNullException.ThrowIfNull(nameof(fontManager));

            if (string.IsNullOrEmpty(Text))
            {
                return;
            }

            var typeface = fontManager!.GetTypeface(FontStyle);

            using var font = new SKFont(typeface, FontStyle.Size);

            using var fillPaint = new SKPaint
            {
                Color = FontColor,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            float x = Location.X;
            float y = Location.Y;

            canvas.Save();

            // -------------------------------------------------
            // Apply rotation
            // -------------------------------------------------
            if (Math.Abs(Rotation) > 0.001f)
            {
                canvas.Translate(x, y);
                canvas.RotateDegrees(Rotation);
                x = 0;
                y = 0;
            }

            // -------------------------------------------------
            // GLOW (draw first, behind everything)
            // -------------------------------------------------
            if (GlowStrength > 0)
            {
                using var glowPaint = new SKPaint
                {
                    Color = GlowColor,
                    IsAntialias = true,
                    Style = SKPaintStyle.Fill,
                    MaskFilter = SKMaskFilter.CreateBlur(
                        SKBlurStyle.Normal,
                        GlowStrength)
                };

                canvas.DrawText(Text, x, y, font, glowPaint);
            }

            // -------------------------------------------------
            // OUTLINE (stroke)
            // -------------------------------------------------
            if (OutlineWidth > 0)
            {
                using var strokePaint = new SKPaint
                {
                    Color = OutlineColor,
                    IsAntialias = true,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = OutlineWidth,
                    StrokeJoin = SKStrokeJoin.Round
                };

                canvas.DrawText(Text, x, y, font, strokePaint);
            }

            // -------------------------------------------------
            // FILL (main text)
            // -------------------------------------------------
            canvas.DrawText(Text, x, y, font, fillPaint);

            canvas.Restore();
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
