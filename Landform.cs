namespace RealmStudioShapeRenderingLib
{
    using SkiaSharp;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    public enum LandformRenderMode
    {
        Interactive,
        Final
    }

    public sealed class Landform : PaintedShape, IRequiresAssetResolution
    {
        // -------------------------------------------------
        // Serialized settings (data-only)
        // -------------------------------------------------
        public string LandformName { get; set; } = string.Empty;

        public string LandformDescription { get; set; } = string.Empty;

        public string WorldAnvilArticleId { get; set; } = string.Empty;

        public CoastlineSettings Coastline { get; set; } = new();
        public LandformShadingSettings Shading { get; set; } = new();

        public LandformRenderMode RenderMode { get; set; } = LandformRenderMode.Final;

        private SKImage? _interiorShadingMask;

        private SKRect _exteriorMaskBounds;

        private SKBitmap? _distanceBitmap;
        private int _distanceW;
        private int _distanceH;

        // -------------------------------------------------
        // Runtime-resolved state
        // -------------------------------------------------

        private ICoastlineStyle? _resolvedCoastlineStyle;
        private IReadOnlyList<CoastlineBand>? _resolvedCoastlineBands;
        private SKImage? _resolvedFillTexture;
        private SKShader? _resolvedTextureShader;

        private SKPicture? _renderCache;
        private bool _renderDirty = true;

        // -------------------------------------------------
        // Geometry change hook
        // -------------------------------------------------

        protected override void RebuildPerimeter()
        {
            base.RebuildPerimeter();
            InvalidateRenderCache();
        }

        public void InvalidateRenderCache()
        {
            _renderDirty = true;
            _resolvedCoastlineStyle = null;
            _resolvedCoastlineBands = null;
        }

        // -------------------------------------------------
        // Resolve phase (called by scene / layer / renderer)
        // -------------------------------------------------

        public void ResolveRenderAssets(IAssetProvider assets)
        {
            // Resolve coastline style & bands
            _resolvedCoastlineStyle =
                CoastlineStyleFactory.Create(Coastline, assets);

            _resolvedCoastlineBands =
                _resolvedCoastlineStyle.CreateBands(this);

            // Resolve base fill texture
            _resolvedFillTexture = null;

            if (Shading.UseTextureBackground &&
                !string.IsNullOrEmpty(Shading.LandformTextureId))
            {
                _resolvedFillTexture =
                    assets.GetImage(Shading.LandformTextureId);
            }

            if (_resolvedFillTexture != null)
            {
                _resolvedTextureShader = SKShader.CreateImage(
                    _resolvedFillTexture,
                    SKShaderTileMode.Repeat,
                    SKShaderTileMode.Repeat);
            }

            InvalidateRenderCache();
        }

        protected override void SetGeometry(SKPath path)
        {
            base.SetGeometry(path);
            InvalidateRenderCache();
        }

        // -------------------------------------------------
        // Shape2D.Render (pure render entry point)
        // -------------------------------------------------
        
        public override void Render(SKCanvas canvas)
        {
            if (HitPath.IsEmpty)
                return;

            if (RenderMode == LandformRenderMode.Interactive)
            {
                RenderCoastline(canvas);
                RenderFast(canvas);
                return;
            }

            if (_renderDirty)
            {
                RebuildRenderCache();
            }

            if (_renderCache != null)
            {
                canvas.DrawPicture(_renderCache);
            }
        }

        // -------------------------------------------------
        // Render cache construction
        // -------------------------------------------------

        private void RebuildRenderCache()
        {
            _renderCache?.Dispose();
            _renderCache = null;

            using var recorder = new SKPictureRecorder();
            var canvas = recorder.BeginRecording(Bounds);

            RenderCoastline(canvas);

            RenderBaseFill(canvas);

            if (Bounds.Width < 2 * Shading.LandShadingDepth)
            {
                RenderInteriorGradient(canvas);
            }
            else
            {
                _interiorShadingMask?.Dispose();
                _interiorShadingMask = BuildInteriorShadingMask();

                RenderInteriorShading(canvas);
            }

            RenderOutline(canvas);

            _renderCache = recorder.EndRecording();
            _renderDirty = false;
        }

        private SKImage? BuildInteriorShadingMask()
        {
            if (HitPath.IsEmpty)
                return null;

            var bounds = Bounds;
            int w = (int)MathF.Ceiling(bounds.Width);
            int h = (int)MathF.Ceiling(bounds.Height);

            if (w <= 0 || h <= 0)
                return null;

            using var bitmap = new SKBitmap(
                w,
                h,
                SKColorType.Alpha8,
                SKAlphaType.Premul);

            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(SKColors.Transparent);
                canvas.Translate(-bounds.Left, -bounds.Top);

                using var fill = new SKPaint
                {
                    Style = SKPaintStyle.Fill,
                    Color = SKColors.White
                };

                canvas.DrawPath(HitPath, fill);
            }

            // Depth scaled to landform size
            float minDim = MathF.Min(bounds.Width, bounds.Height);
            ushort maxDepth = (ushort)Math.Clamp(
                (int)(minDim * Shading.DepthScale),
                1,
                ushort.MaxValue);

            ushort[] dist = ComputeDistanceField(bitmap, w, h, maxDepth);

            var pixels = bitmap.GetPixelSpan();

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;

                    if (pixels[idx] == 0)
                        continue;

                    ushort d = dist[idx];

                    float normalized = 1f - (d / (float)maxDepth);
                    if (normalized <= 0f)
                    {
                        pixels[idx] = 0;
                        continue;
                    }

                    float shaped = MathF.Pow(normalized, Shading.InteriorCurvePower);

                    float alphaFloat = Shading.MaxAlpha * shaped;

                    pixels[idx] = (byte)Math.Clamp(alphaFloat, 0f, 255f);
                }
            }

            return SKImage.FromBitmap(bitmap);
        }



        private static ushort[] ComputeDistanceField(
            SKBitmap bitmap,
            int w,
            int h,
            ushort maxDepth)
        {
            const ushort INF = ushort.MaxValue;

            ushort[] dist = new ushort[w * h];
            var pixels = bitmap.GetPixelSpan();

            // Initialize:
            // White (ocean) = INF
            // Black (land) = 0
            for (int i = 0; i < dist.Length; i++)
            {
                dist[i] = pixels[i] > 0 ? INF : (ushort)0;
            }

            // Forward pass
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;

                    ushort current = dist[idx];
                    if (current == 0)
                        continue;

                    ushort min = current;

                    if (x > 0)
                        min = Math.Min(min, (ushort)Math.Min(dist[idx - 1] + 1, maxDepth));
                    if (y > 0)
                        min = Math.Min(min, (ushort)Math.Min(dist[idx - w] + 1, maxDepth));
                    if (x > 0 && y > 0)
                        min = Math.Min(min, (ushort)Math.Min(dist[idx - w - 1] + 1, maxDepth));
                    if (x < w - 1 && y > 0)
                        min = Math.Min(min, (ushort)Math.Min(dist[idx - w + 1] + 1, maxDepth));

                    dist[idx] = min;
                }
            }

            // Backward pass
            for (int y = h - 1; y >= 0; y--)
            {
                for (int x = w - 1; x >= 0; x--)
                {
                    int idx = y * w + x;

                    ushort current = dist[idx];
                    if (current == 0)
                        continue;

                    ushort min = current;

                    if (x < w - 1)
                        min = Math.Min(min, (ushort)Math.Min(dist[idx + 1] + 1, maxDepth));
                    if (y < h - 1)
                        min = Math.Min(min, (ushort)Math.Min(dist[idx + w] + 1, maxDepth));
                    if (x < w - 1 && y < h - 1)
                        min = Math.Min(min, (ushort)Math.Min(dist[idx + w + 1] + 1, maxDepth));
                    if (x > 0 && y < h - 1)
                        min = Math.Min(min, (ushort)Math.Min(dist[idx + w - 1] + 1, maxDepth));

                    dist[idx] = min;
                }
            }

            return dist;
        }



        private void RenderFast(SKCanvas canvas)
        {
            RenderBaseFill(canvas);

            var center = new SKPoint(Bounds.MidX, Bounds.MidY);

            float radius = Math.Max(Bounds.Width, Bounds.Height) * 0.5f;

            using var shader = SKShader.CreateRadialGradient(
                center,
                radius,
                new[]
                {
                    Shading.LandformBackgroundColor.WithAlpha(0),
                    Shading.LandformOutlineColor.WithAlpha(Shading.MaxAlpha),
                },
                new float[] { 0f, 1f },
                SKShaderTileMode.Clamp);

            using var paint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Shader = shader,
                BlendMode = SKBlendMode.Multiply,
                IsAntialias = true
            };

            canvas.Save();
            canvas.ClipPath(HitPath, SKClipOperation.Intersect, true);
            canvas.DrawRect(Bounds, paint);
            canvas.Restore();

            RenderOutline(canvas);
        }


        private void RenderBaseFill(SKCanvas canvas)
        {
            SKShader shader;

            // Base color shader (always present)
            var colorShader = SKShader.CreateColor(Shading.LandformBackgroundColor);

            if (Shading.UseTextureBackground && _resolvedTextureShader != null)
            {
                shader = _resolvedTextureShader;
            }
            else
            {
                shader = colorShader;
            }

            SKPaint p = PaintObjects.LandBaseFillPaint.Clone();
            p.Shader = shader;

            canvas.Save();
            canvas.ClipPath(HitPath, SKClipOperation.Intersect, true);
            canvas.DrawPath(HitPath, p);
            canvas.Restore();
        }


        // -------------------------------------------------
        // Coastline rendering (outside landform)
        // -------------------------------------------------

        private void RenderCoastline(SKCanvas canvas)
        {
            switch (Coastline.CoastlineStyle)
            {
                case LandformCoastlineStyle.None:
                    break;
                case LandformCoastlineStyle.UniformBlend:
                    RenderUniformBlendCoastline(canvas);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported coastline style: {Coastline.CoastlineStyle}");
            }
        }

        private void RenderUniformBlendCoastline(SKCanvas canvas)
        {
            if (PerimeterPath == null || PerimeterPath.IsEmpty)
                return;

            float depth = Coastline.EffectDistance;
            int steps = 24; // tweak for smoothness

            canvas.Save();

            // Clip OUTSIDE land
            canvas.ClipPath(HitPath, SKClipOperation.Difference, true);

            for (int i = 0; i < steps; i++)
            {
                float t = i / (float)(steps - 1);

                float width = depth * t;
                float alphaFactor = 1f - t;
                alphaFactor = MathF.Pow(alphaFactor, Coastline.FalloffPower);

                byte alpha = (byte)(Coastline.MaxAlpha * alphaFactor);

                using var paint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = width,
                    Color = Coastline.CoastlineColor.WithAlpha(alpha),
                    IsAntialias = true
                };

                canvas.DrawPath(PerimeterPath, paint);
            }

            canvas.Restore();
        }

        // -------------------------------------------------
        // Interior shading
        // -------------------------------------------------

        private void RenderInteriorShading(SKCanvas canvas)
        {
            if (_interiorShadingMask == null)
                return;

            var bounds = HitPath.Bounds;

            using var paint = new SKPaint
            {
                Color = Shading.LandformOutlineColor,
                BlendMode = SKBlendMode.Multiply,
                IsAntialias = true
            };

            canvas.Save();
            canvas.ClipPath(HitPath, SKClipOperation.Intersect, true);
            canvas.DrawImage(_interiorShadingMask, bounds.Left, bounds.Top, paint);
            canvas.Restore();
        }

        private void RenderInteriorGradient(SKCanvas canvas)
        {
            var s = Shading;

            if (!s.EnableInteriorShading || s.LandShadingDepth <= 0)
                return;

            var bounds = HitPath.Bounds;
            var center = Utilities.ComputeCentroid(HitPath);

            float maxRadius = s.LandShadingDepth;

            // Coastline = strong alpha
            var coastColor = s.LandformOutlineColor.WithAlpha(s.MaxAlpha);

            // Inland = completely transparent
            var inlandColor = s.LandformOutlineColor.WithAlpha(0);

            using var shader = SKShader.CreateRadialGradient(
                center,
                maxRadius,
                new[] { inlandColor, coastColor },
                new[] { 0f, 1f },
                SKShaderTileMode.Clamp);

            using var paint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Shader = shader,
                BlendMode = SKBlendMode.Multiply,
                IsAntialias = true
            };

            canvas.Save();
            canvas.ClipPath(HitPath, SKClipOperation.Intersect, true);
            canvas.DrawRect(bounds, paint);
            canvas.Restore();
        }

        // -------------------------------------------------
        // Outline rendering
        // -------------------------------------------------

        private void RenderOutline(SKCanvas canvas)
        {
            if (Shading.LandformOutlineWidth <= 0)
                return;

            using var paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = Shading.LandformOutlineColor,
                StrokeWidth = Shading.LandformOutlineWidth,
                IsAntialias = true
            };

            canvas.DrawPath(PerimeterPath, paint);
        }

        public void ResolveAssets(IAssetProvider assets)
        {
            _resolvedFillTexture = null;
            _resolvedTextureShader = null;

            if (Shading.UseTextureBackground &&
                !string.IsNullOrEmpty(Shading.LandformTextureId))
            {
                _resolvedFillTexture =
                    assets.GetImage(Shading.LandformTextureId);

                if (_resolvedFillTexture != null)
                {
                    _resolvedTextureShader =
                        SKShader.CreateImage(
                            _resolvedFillTexture,
                            SKShaderTileMode.Repeat,
                            SKShaderTileMode.Repeat);
                }
            }

            InvalidateRenderCache();
        }

        // Noise functions

        private static float Noise2D(int x, int y, int seed)
        {
            unchecked
            {
                int n = x;
                n = (n << 13) ^ n;
                int hash = (n * (n * n * 15731 + 789221) + 1376312589);

                n = y ^ hash ^ seed;
                n = (n << 13) ^ n;
                hash = (n * (n * n * 15731 + 789221) + 1376312589);

                // Map to 0–1
                return 0.5f * (1f + (hash & 0x7fffffff) / (float)int.MaxValue);
            }
        }

        private static float SmoothNoise(float x, float y, int seed)
        {
            int x0 = (int)MathF.Floor(x);
            int y0 = (int)MathF.Floor(y);
            int x1 = x0 + 1;
            int y1 = y0 + 1;

            float sx = x - x0;
            float sy = y - y0;

            float n00 = Noise2D(x0, y0, seed);
            float n10 = Noise2D(x1, y0, seed);
            float n01 = Noise2D(x0, y1, seed);
            float n11 = Noise2D(x1, y1, seed);

            float ix0 = Utilities.Lerp(n00, n10, sx);
            float ix1 = Utilities.Lerp(n01, n11, sx);

            return Utilities.Lerp(ix0, ix1, sy);
        }



    }
}
