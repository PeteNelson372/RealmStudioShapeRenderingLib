using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public class WaterSystem
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public HashSet<WaterBody> WaterBodies { get; set; } = [];

        public bool IsEmpty => WaterBodies.Count == 0;

        public SKPath MergedGeometry { get; set; } = new();

        public SKRect Bounds { get; private set; }

        public WaterRenderSettings RenderSettings { get; set; } = new();

        private SKImage? _shadingMask;
        private bool _renderModified = true;
        private bool _geometryModified = true;
        private int _maskOriginX = 0;
        private int _maskOriginY = 0;
        private bool _interactive;

        public void Add(WaterBody body)
        {
            WaterBodies.Add(body);
            body.WaterSystem = this;

            Bounds = Bounds.IsEmpty ? body.Bounds : SKRect.Union(Bounds, body.Bounds);

            if (MergedGeometry == null || MergedGeometry.IsEmpty)
            {
                MergedGeometry = new SKPath(body.HitPath);
                RenderSettings = WaterRenderSettings.Clone(body.RenderSettings);
                return;
            }
            else
            {
                body.RenderSettings = WaterRenderSettings.Clone(RenderSettings);
            }

            _geometryModified = true;

            InvalidateRenderCache();
        }

        public void Remove(WaterBody body)
        {
            WaterBodies.Remove(body);
            body.WaterSystem = null;

            _geometryModified = true;

            InvalidateRenderCache();
        }

        private void EnsureMergedGeometry()
        {
            if (!_geometryModified)
            {
                return;
            }

            MergedGeometry?.Dispose();
            MergedGeometry = new SKPath();

            if (WaterBodies.Count == 0)
            {
                _geometryModified = false;
                return;
            }

            var processed = new HashSet<WaterBody>();

            foreach (var body in WaterBodies)
            {
                if (processed.Contains(body))
                    continue;

                // Start a cluster
                var clusterUnion = new SKPath(body.HitPath);
                processed.Add(body);

                bool expanded;

                do
                {
                    expanded = false;

                    foreach (var other in WaterBodies)
                    {
                        if (processed.Contains(other))
                        {
                            continue;
                        }

                        if (!clusterUnion.Bounds.IntersectsWith(other.Bounds))
                        {
                            continue;
                        }

                        using var union = clusterUnion.Op(other.HitPath, SKPathOp.Union);

                        if (union != null && !union.IsEmpty)
                        {
                            clusterUnion.Dispose();
                            clusterUnion = new SKPath(union);

                            processed.Add(other);
                            expanded = true;
                        }
                    }

                } while (expanded);

                // Append cluster to final geometry
                MergedGeometry.AddPath(clusterUnion);

                clusterUnion.Dispose();
            }

            _geometryModified = false;
        }

        public void BeginInteractive()
        {
            _interactive = true;
        }

        public void EndInteractive()
        {
            _interactive = false;
            InvalidateRenderCache();
        }

        public void Render(SKCanvas canvas)
        {
            if (MergedGeometry == null || MergedGeometry.IsEmpty)
            {
                return;
            }

            EnsureMergedGeometry();

            if (_interactive)
            {
                RenderInteractive(canvas);
            }
            else
            {
                RenderInterior(canvas, MergedGeometry);
                RenderShoreline(canvas, MergedGeometry);
            }
        }


        private void RenderInteractive(SKCanvas canvas)
        {
            if (MergedGeometry == null || MergedGeometry.IsEmpty)
                return;

            using var fill = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = RenderSettings.ShallowWaterColor,
                IsAntialias = true
            };

            using var shoreline = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = RenderSettings.ShorelineWidth,
                Color = RenderSettings.ShorelineColor,
                IsAntialias = true
            };

            canvas.DrawPath(MergedGeometry, fill);
            canvas.DrawPath(MergedGeometry, shoreline);
        }
        
        private SKImage? BuildWaterShadingMask(SKPath geometry)
        {
            var bounds = geometry.Bounds;

            float shelfDepth = Math.Max(RenderSettings.ShelfDepth, RenderSettings.RiverWidth * 0.18f);
            float shallowDepth = RenderSettings.ShallowDepth;

            float padding = Math.Max(shallowDepth, shelfDepth) + 2;

            bounds.Inflate(padding, padding);

            _maskOriginX = (int)MathF.Floor(bounds.Left);
            _maskOriginY = (int)MathF.Floor(bounds.Top);

            int width = (int)MathF.Ceiling(bounds.Right) - _maskOriginX;
            int height = (int)MathF.Ceiling(bounds.Bottom) - _maskOriginY;

            if (width <= 0 || height <= 0)
                return null;

            using var maskBitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);

            using (var canvas = new SKCanvas(maskBitmap))
            {
                canvas.Clear(SKColors.Transparent);

                canvas.Translate(-_maskOriginX, -_maskOriginY);

                using var paint = new SKPaint
                {
                    Style = SKPaintStyle.Fill,
                    Color = SKColors.White,
                    IsAntialias = true
                };

                canvas.DrawPath(MergedGeometry, paint);
            }

            ushort[] dist = PaintedShape.ComputeDistanceFieldFast(
                maskBitmap,
                width,
                height,
                (ushort)shallowDepth);

            using var output = new SKBitmap(width, height);

            var maskPixels = maskBitmap.Pixels;

            for (int y = 0; y < height; y++)
            {
                int row = y * width;

                for (int x = 0; x < width; x++)
                {
                    int idx = row + x;

                    if (maskPixels[idx].Alpha == 0)
                    {
                        output.SetPixel(x, y, SKColors.Transparent);
                        continue;
                    }

                    ushort d = dist[idx];

                    float t = Math.Clamp((float)d / shallowDepth, 0f, 1f);

                    t = MathF.Sqrt(t);
                    t = MathF.Pow(t, RenderSettings.DeepBias);

                    SKColor color;

                    if (d < shelfDepth)
                    {
                        float shelfT = (float)d / shelfDepth;

                        var shelfColor = Utilities.LerpColor(
                            RenderSettings.ShallowWaterColor,
                            SKColors.White.WithAlpha(120),
                            0.35f);

                        color = Utilities.LerpColor(
                            shelfColor,
                            RenderSettings.ShallowWaterColor,
                            shelfT);
                    }
                    else
                    {
                        color = Utilities.LerpColor(
                            RenderSettings.ShallowWaterColor,
                            RenderSettings.DeepWaterColor,
                            t);
                    }

                    output.SetPixel(x, y, color);
                }
            }

            return SKImage.FromBitmap(output);
        }
        

        public void InvalidateRenderCache()
        {
            _renderModified = true;
        }

        private void EnsureRenderMask(SKPath geometry)
        {
            if (!_renderModified && _shadingMask != null)
            {
                return;
            }

            _shadingMask?.Dispose();
            _shadingMask = null;

            _shadingMask = BuildWaterShadingMask(geometry);

            _renderModified = false;
        }

        public void RenderInterior(SKCanvas canvas, SKPath geometry)
        {
            EnsureRenderMask(geometry);

            if (_shadingMask == null)
            {
                return;
            }

            using var paint = new SKPaint
            {
                BlendMode = SKBlendMode.SrcOver,
                IsAntialias = true
            };

            canvas.DrawImage(_shadingMask, _maskOriginX, _maskOriginY, paint);
        }

        public void RenderShoreline(SKCanvas canvas, SKPath geometry)
        {
            using var paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = RenderSettings.ShorelineColor,
                StrokeWidth = RenderSettings.ShorelineWidth,
                StrokeJoin = SKStrokeJoin.Round,
                StrokeCap = SKStrokeCap.Round,
                IsAntialias = true
            };

            canvas.DrawPath(geometry, paint);
        }
    }
}
