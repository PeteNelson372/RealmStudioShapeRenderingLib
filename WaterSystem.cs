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
        private int _maskOriginX = 0;
        private int _maskOriginY = 0;

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

            using var union = MergedGeometry.Op(body.HitPath, SKPathOp.Union);

            if (union != null && !union.IsEmpty)
            {
                MergedGeometry?.Dispose();
                MergedGeometry = new SKPath(union);
            }

            InvalidateRenderCache();
        }

        public void Remove(WaterBody body)
        {
            WaterBodies.Remove(body);
            RebuildMergedGeometry();

            InvalidateRenderCache();
        }

        public void RebuildMergedGeometry()
        {
            MergedGeometry?.Dispose();
            MergedGeometry = new();

            foreach (var body in WaterBodies)
            {
                if (MergedGeometry.IsEmpty)
                {
                    MergedGeometry = new(body.HitPath);
                }
                else
                {
                    using var union = MergedGeometry.Op(body.HitPath, SKPathOp.Union);

                    if (union != null)
                    {
                        MergedGeometry.Dispose();
                        MergedGeometry = new SKPath(union);
                    }
                }
            }
        }

        public void Render(SKCanvas canvas)
        {
            if (MergedGeometry == null || MergedGeometry.IsEmpty)
            {
                return;
            }

            RenderInterior(canvas, MergedGeometry);
            RenderShoreline(canvas, MergedGeometry);            
        }


        private SKImage? BuildWaterShadingMask(SKPath geometry)
        {
            var bounds = geometry.Bounds;

            float padding = Math.Max(RenderSettings.ShallowDepth, RenderSettings.ShelfDepth) + 2;

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
                    IsAntialias = false,
                };

                canvas.DrawPath(MergedGeometry, paint);
            }

            ushort[] dist = PaintedShape.ComputeDistanceField(maskBitmap, width, height, (ushort)RenderSettings.ShallowDepth);

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

                    float t = Math.Clamp((float)d / RenderSettings.ShallowDepth, 0f, 1f);

                    t = MathF.Sqrt(t);

                    t = MathF.Pow(t, RenderSettings.DeepBias);

                    var color = SKColors.Transparent;

                    if (d < RenderSettings.ShelfDepth)
                    {
                        float shelfT = (float)d / RenderSettings.ShelfDepth;

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
