#nullable enable

namespace RealmStudioShapeRenderingLib
{
    using RealmStudioX;
    using SkiaSharp;

    /// <summary>
    /// A painted, blob-style shape built from stamped circular brush strokes.
    /// Supports incremental painting and boolean erasing.
    /// </summary>
    public class PaintedShape : Shape2D
    {
        // -------------------------------------------------
        // Brush configuration
        // -------------------------------------------------

        public float BrushRadius { get; set; } = 12f;

        /// <summary>
        /// Minimum spacing between brush stamps as a fraction of radius.
        /// </summary>
        public float BrushSpacing { get; set; } = 0.5f;

        // -------------------------------------------------
        // Stroke state (transient)
        // -------------------------------------------------

        private SKPoint? _lastPoint;
        private readonly SKPath _strokePath = new();

        // -------------------------------------------------
        // Painting API (called by tools / commands)
        // -------------------------------------------------

        public void BeginStroke(SKPoint point)
        {
            _lastPoint = point;
            Stamp(point);
            CommitStroke();
        }

        public void AddStrokePoint(SKPoint point)
        {
            if (_lastPoint == null)
                return;

            float minDistance = BrushRadius * BrushSpacing;
            if (Utilities.Distance(_lastPoint.Value, point) < minDistance)
                return;

            _lastPoint = point;
            Stamp(point);
            CommitStroke();
        }

        public void EndStroke()
        {
            _lastPoint = null;
            _strokePath.Reset();
        }

        // -------------------------------------------------
        // Erasing (boolean subtraction)
        // -------------------------------------------------

        public void EraseCircle(SKPoint point, float radius)
        {
            if (_cachedPath.IsEmpty)
                return;

            using var erasePath = new SKPath();
            erasePath.AddCircle(point.X, point.Y, radius);

            SKPath result = _cachedPath.Op(erasePath, SKPathOp.Difference);
            SetGeometry(result);
        }

        // -------------------------------------------------
        // Geometry construction
        // -------------------------------------------------

        private void Stamp(SKPoint point)
        {
            _strokePath.AddCircle(point.X, point.Y, BrushRadius);
        }

        /// <summary>
        /// Commits the current stroke path into the shape geometry.
        /// Uses union to accumulate painted area.
        /// </summary>
        private void CommitStroke()
        {
            if (_strokePath.IsEmpty)
                return;

            SKPath result = _cachedPath.IsEmpty
                ? new SKPath(_strokePath)
                : _cachedPath.Op(_strokePath, SKPathOp.Union);

            _strokePath.Reset();
            SetGeometry(result);
        }

        public override void RestoreGeometry(SKPath geometry)
        {
            // Defensive copy so caller can't mutate internal state
            SetGeometry(new SKPath(geometry));
        }

        // -------------------------------------------------
        // Perimeter handling
        // -------------------------------------------------

        protected override void RebuildPerimeter()
        {
            // For painted shapes, the perimeter is the outline of the filled region
            PerimeterPath = new SKPath(HitPath);
        }

        // -------------------------------------------------
        // Rendering
        // -------------------------------------------------

        public override void Render(SKCanvas canvas, FontManager? _)
        {
            if (HitPath.IsEmpty)
                return;

            using var fill = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = SKColors.DarkGray,
                IsAntialias = true
            };

            using var outline = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1f,
                Color = SKColors.Black,
                IsAntialias = true
            };

            canvas.DrawPath(HitPath, fill);
            canvas.DrawPath(PerimeterPath, outline);
        }

        public static ushort[] ComputeDistanceFieldFast(
            SKBitmap maskBitmap,
            int width,
            int height,
            ushort maxDepth)
        {
            ushort[] dist = new ushort[width * height];
            var pixels = maskBitmap.Pixels;

            const ushort INF = ushort.MaxValue;

            // ------------------------------------------------
            // Initialization (parallel)
            // ------------------------------------------------

            Parallel.For(0, dist.Length, i =>
            {
                dist[i] = INF;
            });

            // ------------------------------------------------
            // Detect shoreline boundary (parallel by rows)
            // ------------------------------------------------

            Parallel.For(1, height - 1, y =>
            {
                int row = y * width;

                for (int x = 1; x < width - 1; x++)
                {
                    int idx = row + x;

                    if (pixels[idx].Alpha == 0)
                    {
                        continue;
                    }

                    if (pixels[idx - 1].Alpha == 0 ||
                        pixels[idx + 1].Alpha == 0 ||
                        pixels[idx - width].Alpha == 0 ||
                        pixels[idx + width].Alpha == 0)
                    {
                        dist[idx] = 0;
                    }
                }
            });

            // ------------------------------------------------
            // Forward pass (must remain sequential)
            // ------------------------------------------------

            for (int y = 1; y < height; y++)
            {
                int row = y * width;

                for (int x = 1; x < width; x++)
                {
                    int idx = row + x;

                    if (dist[idx] == 0)
                    {
                        continue;
                    }

                    ushort best = dist[idx];

                    best = Math.Min(best, (ushort)(dist[idx - 1] + 1));
                    best = Math.Min(best, (ushort)(dist[idx - width] + 1));
                    best = Math.Min(best, (ushort)(dist[idx - width - 1] + 1));
                    best = Math.Min(best, (ushort)(dist[idx - width + 1] + 1));

                    dist[idx] = best;
                }
            }

            // ------------------------------------------------
            // Backward pass (must remain sequential)
            // ------------------------------------------------

            for (int y = height - 2; y >= 0; y--)
            {
                int row = y * width;

                for (int x = width - 2; x >= 0; x--)
                {
                    int idx = row + x;

                    ushort best = dist[idx];

                    best = Math.Min(best, (ushort)(dist[idx + 1] + 1));
                    best = Math.Min(best, (ushort)(dist[idx + width] + 1));
                    best = Math.Min(best, (ushort)(dist[idx + width + 1] + 1));
                    best = Math.Min(best, (ushort)(dist[idx + width - 1] + 1));

                    if (best > maxDepth)
                    {
                        best = maxDepth;
                    }

                    dist[idx] = best;
                }
            }

            return dist;
        }
    }
}
