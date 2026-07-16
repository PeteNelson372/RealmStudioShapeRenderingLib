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
    public class MapSymbol() : MapComponent2D, ITransformable2D, IAlignable, IRotatable
    {
        [XmlElement]
        public required MapSymbolDefinition SymbolDefinition { get; init; }

        [XmlElement]
        public string Name { get; set; } = string.Empty;

        [XmlElement]
        public string Description { get; set; } = string.Empty;

        [XmlElement]
        public string WorldAnvilArticleId { get; set; } = string.Empty;

        [XmlElement]
        public SKPoint Location { get; set; }
        
        [XmlElement]
        public float Rotation { get; set; }
        
        [XmlElement]
        public float Scale { get; set; } = 1f;

        [XmlElement]
        public bool Mirror { get; set; }

        [XmlIgnore]
        public bool IsTransformTarget { get; set; } = false;

        [XmlIgnore]
        public SKColor TintColor { get; set; } = SKColors.White;

        [XmlElement("TintColor")]
        public string TintColorXml
        {
            get => XmlColorConverter.Serialize(TintColor);
            set => TintColor = XmlColorConverter.Deserialize(value);
        }

        [XmlIgnore]
        public SKColor[] CustomSymbolColors { get; set; } = new SKColor[3];

        [XmlArray("CustomSymbolColors")]
        [XmlArrayItem("SymbolColor")]
        public string[] CustomSymbolColorsXml
        {
            get => [.. CustomSymbolColors.Select(XmlColorConverter.Serialize)];

            set
            {
                if (value == null)
                {
                    CustomSymbolColors = [];
                    return;
                }

                CustomSymbolColors = [.. value.Select(XmlColorConverter.Deserialize)];
            }
        }

        [XmlElement]
        public override SKRect LocalBounds { get; set; }

        private SymbolImageResource? symbolImage = null;

        private float _startScale;

        public override void FinalizeShapeGeometry(RealmStudioMap map)
        {
            UpdateBounds();
        }

        public override IShapeState CaptureState()
        {
            return new MapSymbolState
            {
                Location = Location,
                Rotation = Rotation,
                Scale = Scale,
                Mirror = Mirror,
                LocalBounds = LocalBounds,
                TintColor = TintColor,
                CustomColors = (SKColor[])CustomSymbolColors.Clone(),

                Name = Name,
                Description = Description
            };
        }

        public override void RestoreState(IShapeState state)
        {
            if (state is not MapSymbolState s)
            {
                return;
            }

            Location = s.Location;
            Rotation = s.Rotation;
            Scale = s.Scale;
            Mirror = s.Mirror;
            LocalBounds = s.LocalBounds;
            TintColor = s.TintColor;

            // Copy colors (important: do not reassign array reference if it's reused elsewhere)
            if (s.CustomColors != null && s.CustomColors.Length == 3)
            {
                Array.Copy(s.CustomColors, CustomSymbolColors, 3);
            }

            Name = s.Name;
            Description = s.Description;

            UpdateBounds();
        }

        public void UpdateBounds()
        {
            var local = LocalBounds;

            // -------------------------------------------------
            // 1. Apply scale + mirror (match Render!)
            // -------------------------------------------------
            float sx = Mirror ? -Scale : Scale;
            float sy = Scale;

            float left = local.Left * sx;
            float top = local.Top * sy;
            float right = local.Right * sx;
            float bottom = local.Bottom * sy;

            // -------------------------------------------------
            // 2. Normalize in case mirror flipped axes
            // -------------------------------------------------
            float minLocalX = MathF.Min(left, right);
            float maxLocalX = MathF.Max(left, right);
            float minLocalY = MathF.Min(top, bottom);
            float maxLocalY = MathF.Max(top, bottom);

            var p1 = new SKPoint(minLocalX, minLocalY);
            var p2 = new SKPoint(maxLocalX, minLocalY);
            var p3 = new SKPoint(maxLocalX, maxLocalY);
            var p4 = new SKPoint(minLocalX, maxLocalY);

            // -------------------------------------------------
            // 3. Apply rotation
            // -------------------------------------------------
            float radians = Rotation * MathF.PI / 180f;
            float cos = MathF.Cos(radians);
            float sin = MathF.Sin(radians);

            SKPoint Rotate(SKPoint p) => new(
                p.X * cos - p.Y * sin,
                p.X * sin + p.Y * cos
            );

            p1 = Rotate(p1);
            p2 = Rotate(p2);
            p3 = Rotate(p3);
            p4 = Rotate(p4);

            // -------------------------------------------------
            // 4. Translate to world position
            // -------------------------------------------------
            p1.Offset(Location);
            p2.Offset(Location);
            p3.Offset(Location);
            p4.Offset(Location);

            // -------------------------------------------------
            // 5. Compute AABB (axis-aligned bounding box)
            // -------------------------------------------------
            float minX = MathF.Min(MathF.Min(p1.X, p2.X), MathF.Min(p3.X, p4.X));
            float minY = MathF.Min(MathF.Min(p1.Y, p2.Y), MathF.Min(p3.Y, p4.Y));
            float maxX = MathF.Max(MathF.Max(p1.X, p2.X), MathF.Max(p3.X, p4.X));
            float maxY = MathF.Max(MathF.Max(p1.Y, p2.Y), MathF.Max(p3.Y, p4.Y));

            Bounds = new SKRect(minX, minY, maxX, maxY);
        }

        public SKRect GetLocalBounds()
        {
            return LocalBounds;
        }

        public SKPoint[] GetTransformedCorners()
        {
            var r = LocalBounds;

            float sx = Mirror ? -Scale : Scale;
            float sy = Scale;

            float rad = Rotation * MathF.PI / 180f;
            float cos = MathF.Cos(rad);
            float sin = MathF.Sin(rad);

            SKPoint Transform(float x, float y)
            {
                // 1. Start in local space

                // 2. Apply scale (first in canvas, last in math → reverse order)
                x *= sx;
                y *= sy;

                // 3. Apply rotation
                float rx = x * cos - y * sin;
                float ry = x * sin + y * cos;

                // 4. Apply translation
                return new SKPoint(
                    rx + Location.X,
                    ry + Location.Y
                );
            }

            return
            [
                Transform(r.Left,  r.Top),
                Transform(r.Right, r.Top),
                Transform(r.Right, r.Bottom),
                Transform(r.Left,  r.Bottom)
            ];
        }

        public void BeginScale()
        {
            _startScale = Scale;
        }

        public void ApplyScale(float factor)
        {
            Scale = _startScale * factor;
        }

        public override bool HitTest(SKPoint worldPos)
        {
             if (!Bounds.Contains(worldPos))
             {
                return false;
             }

            if (!PointInQuad(worldPos, GetTransformedCorners()))
            {
                return false;
            }

            return true;
        }

        private static bool PointInQuad(SKPoint p, SKPoint[] c)
        {
            static float Sign(SKPoint p1, SKPoint p2, SKPoint p3)
            {
                return (p1.X - p3.X) * (p2.Y - p3.Y) -
                       (p2.X - p3.X) * (p1.Y - p3.Y);
            }

            bool b1 = Sign(p, c[0], c[1]) < 0.0f;
            bool b2 = Sign(p, c[1], c[2]) < 0.0f;
            bool b3 = Sign(p, c[2], c[3]) < 0.0f;
            bool b4 = Sign(p, c[3], c[0]) < 0.0f;

            return (b1 == b2) && (b2 == b3) && (b3 == b4);
        }

        private SKPaint? CreatePaint(SymbolImageResource resource)
        {
            switch (SymbolDefinition.BaseColorType)
            {
                case MapSymbolBaseColorType.FullColor:
                    return null;

                case MapSymbolBaseColorType.GrayScale:
                    return CreateGrayscaleTintPaint(TintColor);

                case MapSymbolBaseColorType.RGBMask:
                    return CreateRGBMaskPaint(CustomSymbolColors);

                default:
                    return null;
            }
        }

        private static SKPaint CreateGrayscaleTintPaint(SKColor tint)
        {
            float r = tint.Red / 255f;
            float g = tint.Green / 255f;
            float b = tint.Blue / 255f;

            var matrix = new float[]
            {
                r, 0, 0, 0, 0,
                0, g, 0, 0, 0,
                0, 0, b, 0, 0,
                0, 0, 0, 1, 0
            };

            return new SKPaint
            {
                ColorFilter = SKColorFilter.CreateColorMatrix(matrix),
                IsAntialias = true
            };
        }

        private static SKPaint CreateRGBMaskPaint(SKColor[] colors)
        {
            var r = colors[0];
            var g = colors[1];
            var b = colors[2];

            var matrix = new float[]
            {
                r.Red / 255f,   g.Red / 255f,   b.Red / 255f,   0, 0,
                r.Green / 255f, g.Green / 255f, b.Green / 255f, 0, 0,
                r.Blue / 255f,  g.Blue / 255f,  b.Blue / 255f,  0, 0,
                0,              0,              0,              1, 0
            };

            return new SKPaint
            {
                ColorFilter = SKColorFilter.CreateColorMatrix(matrix),
                IsAntialias = true
            };
        }

        public override void Render(SKCanvas canvas, FontManager? _, SKPath? clipPath = null)
        {
            var context = RenderContextScope.Current;

            symbolImage ??= context.ImageCache.Get(SymbolDefinition.SymbolFilePath);

            if (symbolImage == null)
            {
                return;
            }

            canvas.Save();

            // 1. Move to world position
            canvas.Translate(Location);

            // 2. Apply rotation
            canvas.RotateDegrees(Rotation);

            // 3. Apply scale + mirror
            float sx = Mirror ? -Scale : Scale;
            float sy = Scale;
            canvas.Scale(sx, sy);

            // 5. Draw
            var paint = CreatePaint(symbolImage);
            DrawSymbolCentered(canvas, LocalBounds, Scale, symbolImage, paint);

            if (IsSelected && !IsTransformTarget)
            {
                //canvas.DrawRect(Bounds, PaintObjects.MapSymbolSelectPaint);
            }


            // Debug bounds
            //canvas.DrawRect(LocalBounds, new SKPaint
            //{
            //    Style = SKPaintStyle.Stroke,
            //    Color = SKColors.Red,
            //    StrokeWidth = 2,
            //    IsAntialias = true
            //});
            

            canvas.Restore();
        }

        public static void DrawSymbolCentered(
            SKCanvas canvas,
            SKRect localBounds,
            float scale,
            SymbolImageResource resource,
            SKPaint? paint)
        {
            switch (resource)
            {
                case BitmapResource bmp:
                    {
                        var src = new SKRect(0, 0, bmp.Image.Width, bmp.Image.Height);
                        canvas.DrawImage(bmp.Image, src, localBounds, SKSamplingOptions.Default, paint);
                        break;
                    }

                case SvgResource svg:
                    {
                        var image = svg.GetImage(scale);

                        var src = new SKRect(0, 0, image.Width, image.Height);
                        canvas.DrawImage(image, src, localBounds, SKSamplingOptions.Default, paint);
                        break;
                    }

                default:
                    throw new InvalidOperationException("Unsupported symbol image resource type");
            }
        }
    }

}
