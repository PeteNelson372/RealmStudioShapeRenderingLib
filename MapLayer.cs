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
#nullable enable

using SkiaSharp;
using System.Xml.Serialization;

namespace RealmStudioShapeRenderingLib
{
    [XmlType("MapLayer")]
    public class MapLayer
    {
        private readonly List<Shape2D> _shapes = new(500);
        public IReadOnlyList<Shape2D> Shapes => _shapes;

        // -------------------------------------------------
        // Metadata
        // -------------------------------------------------

        [XmlAttribute]
        public Guid MapLayerGuid { get; set; } = Guid.NewGuid();

        [XmlAttribute]
        public string MapLayerName { get; set; } = "";

        [XmlAttribute]
        public float MapLayerOrder { get; set; }

        [XmlIgnore]
        public bool ShowLayer { get; set; } = true;

        [XmlAttribute]
        public bool Drawable { get; set; } = false;

        public SKRect LayerRect { get; set; } = SKRect.Empty; // the size of the MapLayer; should be same as map width and height

        // -------------------------------------------------
        // Render cache (runtime only)
        // -------------------------------------------------

        [XmlIgnore]
        private SKSurface? _cachedSurface;

        [XmlIgnore]
        private bool _modified = true;

        [XmlIgnore]
        private SKSizeI _cachedSize;

        // -------------------------------------------------
        // Shape management
        // -------------------------------------------------

        public void Add(Shape2D shape)
        {
            if (shape == null)
                return;

            _shapes.Add(shape);
            Invalidate();
        }

        public void Remove(Shape2D shape)
        {
            if (_shapes.Remove(shape))
            {
                Invalidate();
            }
        }

        public void Clear()
        {
            if (_shapes.Count == 0)
                return;

            _shapes.Clear();
            Invalidate();
        }

        // -------------------------------------------------
        // Invalidation
        // -------------------------------------------------

        public void Invalidate()
        {
            _modified = true;
        }

        // -------------------------------------------------
        // Rendering
        // -------------------------------------------------

        public void Render(SKCanvas canvas)
        {
            if (!ShowLayer)
                return;

            ArgumentNullException.ThrowIfNull(canvas);

            // Lazy rebuild
            if (_modified || _cachedSurface == null)
            {
                RebuildCache();
            }

            // Draw cached layer at origin (world space)
            using var paint = new SKPaint
            {
                BlendMode = SKBlendMode.SrcOver
            };

            canvas.DrawSurface(_cachedSurface!, 0, 0, paint);
        }

        private void RebuildCache()
        {
            SKSizeI size = new((int)LayerRect.Width, (int)LayerRect.Height);

            // Reallocate surface if needed
            if (_cachedSurface == null ||  _cachedSize != size)
            {
                _cachedSurface?.Dispose();

                _cachedSurface = SKSurface.Create(
                    new SKImageInfo(
                        (int)LayerRect.Width,
                        (int)LayerRect.Height,
                        SKColorType.Rgba8888,
                        SKAlphaType.Premul));

                _cachedSize = new SKSizeI((int)LayerRect.Width, (int)LayerRect.Height);
            }

            var surfaceCanvas = _cachedSurface.Canvas;

            // Clear to transparent
            surfaceCanvas.Clear(SKColors.Transparent);

            // IMPORTANT:
            // Shapes render in world space, so we render at identity transform
            surfaceCanvas.Save();
            surfaceCanvas.ResetMatrix();

            foreach (var shape in _shapes)
            {
                shape.Render(surfaceCanvas);
            }

            surfaceCanvas.Restore();

            _modified = false;
        }

        // -------------------------------------------------
        // Hit testing (unchanged)
        // -------------------------------------------------

        public Shape2D? HitTest(SKPoint worldPos)
        {
            for (int i = _shapes.Count - 1; i >= 0; i--)
            {
                var shape = _shapes[i];
                if (shape.Bounds.Contains(worldPos) &&
                    shape.HitPath.Contains(worldPos.X, worldPos.Y))
                {
                    return shape;
                }
            }

            return null;
        }
    }
}

