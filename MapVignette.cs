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
    public class MapVignette : MapComponent2D
    {
        [XmlElement]
        public float VignetteStrength { get; set; } = 0.5f;

        [XmlIgnore]
        public SKColor VignetteColor { get; set; } = SKColor.Parse("#C9977B");

        [XmlElement("VignetteColor")]
        public string VignetteColorXml
        {
            get => XmlColorConverter.Serialize(VignetteColor);
            set => VignetteColor = XmlColorConverter.Deserialize(value);
        }

        [XmlElement]
        public VignetteShapeType VignetteShape { get; set; } = VignetteShapeType.Oval;

        public MapVignette() { }

        public void RenderOvalVignette(SKCanvas canvas)
        {
            SKColor gradientColor = VignetteColor;

            // -------------------------------------------------
            // Oval vignette geometry
            // -------------------------------------------------

            float centerX = Bounds.MidX;
            float centerY = Bounds.MidY;

            // Slightly oversized ellipse produces
            // a softer, more natural vignette.
            float radiusX = Bounds.Width * 0.75f;
            float radiusY = Bounds.Height * 0.75f;

            float t = Utilities.Clamp(
                VignetteStrength,
                0f,
                1f);

            // Ease-out curve
            float innerRadius =
                Utilities.Lerp(
                    0.70f,
                    0.01f,
                    t);

            // -------------------------------------------------
            // Radial gradient shader
            // -------------------------------------------------

            using SKShader radialGradient =
                SKShader.CreateRadialGradient(
                    new SKPoint(0, 0),
                    // Unit radius because we scale canvas.
                    1f,
                    [
                        SKColors.Transparent,
                        SKColors.Transparent,
                        gradientColor.WithAlpha((byte)(255 * VignetteStrength))
                    ],
                    [
                        0.0f,
                        innerRadius,
                        1.0f
                    ],

                    SKShaderTileMode.Clamp);

            using SKPaint paint = new()
            {
                Shader = radialGradient,
                IsAntialias = true,
                Color = gradientColor,
            };

            // -------------------------------------------------
            // Transform unit circle into ellipse
            // -------------------------------------------------

            using (new SKAutoCanvasRestore(canvas))
            {
                canvas.ClipRect(Bounds);

                canvas.Translate(centerX, centerY);

                canvas.Scale(radiusX, radiusY);

                // Draw unit circle which becomes ellipse
                canvas.DrawCircle(
                    0,
                    0,
                    1f,
                    paint);
            }
            
        }

        public void RenderRectangleVignette(SKCanvas canvas)
        {
            SKColor gradientColor = VignetteColor;

            int tenthLeftRight = (int)(Bounds.Width / 5);
            int tenthTopBottom = (int)(Bounds.Height / 5);

            using SKShader linGradLR = SKShader.CreateLinearGradient(new SKPoint(0, Bounds.Height / 2), new SKPoint(tenthLeftRight / 2, Bounds.Height / 2), [gradientColor.WithAlpha((byte)(255 * VignetteStrength)), SKColors.Transparent], SKShaderTileMode.Clamp);
            using SKShader linGradTB = SKShader.CreateLinearGradient(new SKPoint(Bounds.Width / 2, 0), new SKPoint(Bounds.Width / 2, tenthTopBottom), [gradientColor.WithAlpha((byte)(255 * VignetteStrength)), SKColors.Transparent], SKShaderTileMode.Clamp);
            using SKShader linGradRL = SKShader.CreateLinearGradient(new SKPoint(Bounds.Width, Bounds.Height / 2), new SKPoint(Bounds.Width - tenthLeftRight, Bounds.Height / 2), [gradientColor.WithAlpha((byte)(255 * VignetteStrength)), SKColors.Transparent], SKShaderTileMode.Clamp);
            using SKShader linGradBT = SKShader.CreateLinearGradient(new SKPoint(Bounds.Width / 2, Bounds.Height), new SKPoint(Bounds.Width / 2, Bounds.Height - tenthTopBottom), [gradientColor.WithAlpha((byte)(255 * VignetteStrength)), SKColors.Transparent], SKShaderTileMode.Clamp);

            using SKPaint paint = new()
            {
                Shader = linGradLR,
                IsAntialias = true,
                Color = gradientColor,
            };

            SKRect rect = new(0, 0, tenthLeftRight, Bounds.Height);
            canvas.DrawRect(rect, paint);

            paint.Shader = linGradTB;
            rect = new(0, 0, Bounds.Width, tenthTopBottom);
            canvas.DrawRect(rect, paint);

            paint.Shader = linGradRL;
            rect = new(Bounds.Width, 0, Bounds.Width - tenthLeftRight, Bounds.Height);
            canvas.DrawRect(rect, paint);

            paint.Shader = linGradBT;
            rect = new(0, Bounds.Height - tenthTopBottom, Bounds.Width, Bounds.Height);
            canvas.DrawRect(rect, paint);
        }

        public override void Render(SKCanvas canvas, FontManager? fontManager = null, SKPath? clipPath = null)
        {
            if (VignetteShape == VignetteShapeType.Rectangle)
            {
                RenderRectangleVignette(canvas);
            }
            else
            {
                RenderOvalVignette(canvas);
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
