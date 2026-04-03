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
using Svg.Skia;

namespace RealmStudioShapeRenderingLib
{    public class MapSymbol(SKRect bounds) : MapComponent2D, ITransformable2D
    {
        public MapSymbolDefinition SymbolDefinition { get; set; } = new();

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public SKPoint Location { get; set; }
        
        public float Rotation { get; set; }
        
        public float Scale { get; set; } = 1f;

        public bool Mirror { get; set; }

        public SKColor TintColor { get; set; } = SKColors.White;

        public SKColor[] CustomSymbolColors { get; set; } = new SKColor[3];

        public override SKRect Bounds { get; set; } = bounds;

        private SymbolImageResource? symbolImage = null;

        public override IShapeState CaptureState()
        {
            return new MapSymbolState
            {
                Location = Location,
                Rotation = Rotation,
                Scale = Scale,
                Mirror = Mirror,
                
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
            ArgumentNullException.ThrowIfNull(SymbolDefinition.BoundsMetadata, nameof(SymbolDefinition.BoundsMetadata));

            var local = SymbolDefinition.BoundsMetadata.ToSKRect();

            float left = local.Left * Scale;
            float top = local.Top * Scale;
            float right = local.Right * Scale;
            float bottom = local.Bottom * Scale;

            var p1 = new SKPoint(left, top);
            var p2 = new SKPoint(right, top);
            var p3 = new SKPoint(right, bottom);
            var p4 = new SKPoint(left, bottom);

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

            p1.Offset(Location);
            p2.Offset(Location);
            p3.Offset(Location);
            p4.Offset(Location);

            float minX = MathF.Min(MathF.Min(p1.X, p2.X), MathF.Min(p3.X, p4.X));
            float minY = MathF.Min(MathF.Min(p1.Y, p2.Y), MathF.Min(p3.Y, p4.Y));
            float maxX = MathF.Max(MathF.Max(p1.X, p2.X), MathF.Max(p3.X, p4.X));
            float maxY = MathF.Max(MathF.Max(p1.Y, p2.Y), MathF.Max(p3.Y, p4.Y));

            Bounds = new SKRect(minX, minY, maxX, maxY);
        }

        public SKRect GetLocalBounds()
        {
            return SymbolDefinition.BoundsMetadata!.ToSKRect();
        }

        public SKPoint[] GetTransformedCorners()
        {
            ArgumentNullException.ThrowIfNull(SymbolDefinition.BoundsMetadata, nameof(SymbolDefinition.BoundsMetadata));

            var r = SymbolDefinition.BoundsMetadata.ToSKRect();

            float rad = Rotation * MathF.PI / 180f;
            float cos = MathF.Cos(rad);
            float sin = MathF.Sin(rad);

            SKPoint Transform(float x, float y)
            {
                // scale
                x *= Scale;
                y *= Scale;

                // rotate
                float rx = x * cos - y * sin;
                float ry = x * sin + y * cos;

                // translate
                return new SKPoint(
                    rx + Location.X,
                    ry + Location.Y
                );
            }

            return new[]
            {
                Transform(r.Left,  r.Top),
                Transform(r.Right, r.Top),
                Transform(r.Right, r.Bottom),
                Transform(r.Left,  r.Bottom)
            };
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

        private bool PointInQuad(SKPoint p, SKPoint[] c)
        {
            float Sign(SKPoint p1, SKPoint p2, SKPoint p3)
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

        public override void Render(SKCanvas canvas)
        {
            var context = RenderContextScope.Current;

            symbolImage ??= context.ImageCache.Get(SymbolDefinition.SymbolFilePath);

            if (symbolImage == null)
            {
                return;
            }

            canvas.Save();

            // -------------------------------------------------
            // 1. Move to cursor (world position)
            // -------------------------------------------------
            canvas.Translate(Location);

            // -------------------------------------------------
            // 2. Apply rotation
            // -------------------------------------------------
            canvas.RotateDegrees(Rotation);

            // -------------------------------------------------
            // 3. Apply scale + mirror
            // -------------------------------------------------

            float sx = Mirror ? -Scale : Scale;
            float sy = Scale;

            if (SymbolDefinition.SymbolFormat != SymbolFileFormat.Vector)
            {
                canvas.Scale(sx, sy);
            }

            // -------------------------------------------------
            // 4. Draw centered
            // -------------------------------------------------
            DrawSymbolCentered(canvas, Location, Scale, Mirror, symbolImage);

            canvas.Restore();
        }

        public static void DrawSymbolCentered(SKCanvas canvas, SKPoint location, float scale, bool mirror, SymbolImageResource resource)
        {
            var context = RenderContextScope.Current;
            float zoom = context.Zoom;

            switch (resource)
            {
                case BitmapResource bmp:
                    canvas.Translate(-bmp.Image.Width / 2f, -bmp.Image.Height / 2f);
                    canvas.DrawImage(bmp.Image, 0, 0);
                    break;

                case SvgResource svg:
                    {
                        //float sx = mirror ? -scale : scale;
                        //float sy = scale;

                        //float worldScale = MathF.Max(MathF.Abs(sx), MathF.Abs(sy));
                        //float finalScale = worldScale * zoom;

                        var image = svg.GetImage(scale);

                        using SKPaint p = new()
                        {
                            IsAntialias = true
                        };

                        canvas.DrawImage(image,
                            -image.Width / 2f,
                            -image.Height / 2f, p);

                        break;
                    }
            }
        }

    }

}
