using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public class WaterBody : PaintedShape
    {
        public string? WaterSystemId { get; set; } 
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public WaterRenderSettings RenderSettings { get; set; } = new();

        private SKImage? _shadingMask;
        private bool _renderModified = true;
        private int _maskOriginX = 0;
        private int _maskOriginY = 0;

        public override void Render(SKCanvas canvas)
        {
            if (HitPath.IsEmpty) return;

            RenderInterior(canvas);
            RenderShoreline(canvas);
        }

        public void InvalidateRenderCache()
        {
            _renderModified = true;
        }

        private void EnsureRenderMask()
        {
            if (!_renderModified) return;

            _shadingMask?.Dispose();

            _shadingMask = BuildWaterShadingMask();

            _renderModified = false;
        }

        private void RenderInterior(SKCanvas canvas)
        {
            EnsureRenderMask();

            if (_shadingMask == null)
            {
                return;
            }

            using var paint = new SKPaint
            {
                BlendMode = SKBlendMode.SrcOver,
                IsAntialias = false
            };

            canvas.Save();
            canvas.ClipPath(HitPath, SKClipOperation.Intersect, true);
            canvas.DrawImage(_shadingMask, _maskOriginX, _maskOriginY, paint);
            canvas.Restore();
        }

        private void RenderShoreline(SKCanvas canvas)
        {
            canvas.Save();

            canvas.ClipPath(HitPath, SKClipOperation.Intersect, true);

            using var paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = RenderSettings.ShorelineColor,
                StrokeWidth = RenderSettings.ShorelineWidth * 2,
                StrokeJoin = SKStrokeJoin.Round,
                StrokeCap = SKStrokeCap.Round,
                IsAntialias = true
            };

            canvas.DrawPath(PerimeterPath, paint);

            canvas.Restore();
        }

        private SKImage? BuildWaterShadingMask()
        {
            var bounds = HitPath.Bounds;

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

                canvas.DrawPath(HitPath, paint);
            }

            ushort[] dist = ComputeDistanceField(maskBitmap, width, height, (ushort)RenderSettings.ShallowDepth);

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

                    //float t = MathF.Min(1f, (float)d / RenderSettings.ShallowDepth);

                    //t = MathF.Pow(t, RenderSettings.DeepBias);

                    float t = Math.Clamp((float) d / RenderSettings.ShallowDepth, 0f, 1f );

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
    }
}
