namespace RealmStudioShapeRenderingLib
{
    using SkiaSharp;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

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

        private SKImage? _interiorShadingMask;

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

            _interiorShadingMask?.Dispose();
            _interiorShadingMask = BuildInteriorShadingMask();

            using var recorder = new SKPictureRecorder();
            var canvas = recorder.BeginRecording(Bounds);

            if (_resolvedCoastlineBands != null)
            {
                RenderCoastline(canvas, _resolvedCoastlineBands);
            }

            RenderBaseFill(canvas);

            if (Bounds.Width < 2 * Shading.LandShadingDepth)
            {
                RenderInteriorGradient(canvas);
            }
            else
            {
                RenderInteriorShading(canvas);
            }

            RenderOutline(canvas);

            _renderCache = recorder.EndRecording();
            _renderDirty = false;
        }

        private SKImage? BuildInteriorShadingMask()
        {
            var s = Shading;

            if (!s.EnableInteriorShading || s.LandShadingDepth <= 0)
                return null;

            var bounds = HitPath.Bounds;

            int w = (int)Math.Ceiling(bounds.Width);
            int h = (int)Math.Ceiling(bounds.Height);

            if (w <= 0 || h <= 0)
                return null;

            if (_distanceBitmap == null || _distanceW != w || _distanceH != h)
            {
                _distanceBitmap?.Dispose();
                _distanceBitmap = new SKBitmap(w, h, SKColorType.Alpha8, SKAlphaType.Premul);
                _distanceW = w;
                _distanceH = h;
            }

            var canvas = new SKCanvas(_distanceBitmap);

            canvas.Clear(SKColors.Transparent);
            canvas.Translate(-bounds.Left, -bounds.Top);

            // 1️ Draw filled landform mask (white = inside)
            using (var fill = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = SKColors.White,
                IsAntialias = true
            })
            {
                canvas.DrawPath(HitPath, fill);
            }

            canvas.Flush();

            float maxDepth = s.LandShadingDepth;

            ushort uMaxDepth = (ushort)Math.Clamp((int)MathF.Round(Shading.LandShadingDepth), 1, ushort.MaxValue);

            // 2️ Distance transform (two-pass)
            ushort[] distance = ComputeDistanceField(_distanceBitmap, w, h, uMaxDepth);

            // 3️ Convert distance to alpha
            var pixels = _distanceBitmap.GetPixelSpan();

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;

                    if (pixels[idx] == 0)
                        continue;

                    float dist = distance[idx];

                    if (dist > maxDepth)
                    {
                        pixels[idx] = 0;
                    }
                    else
                    {
                        float t = 1f - (dist / maxDepth);
                        byte alpha = (byte)(s.MaxAlpha * t);
                        pixels[idx] = alpha;
                    }
                }
            }

            return SKImage.FromBitmap(_distanceBitmap);
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

            // Initialize
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
                        min = Math.Min(min, (ushort)(dist[idx - 1] + 1));
                    if (y > 0)
                        min = Math.Min(min, (ushort)(dist[idx - w] + 1));
                    if (x > 0 && y > 0)
                        min = Math.Min(min, (ushort)(dist[idx - w - 1] + 1));
                    if (x < w - 1 && y > 0)
                        min = Math.Min(min, (ushort)(dist[idx - w + 1] + 1));

                    if (min > maxDepth)
                        min = maxDepth;

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
                        min = Math.Min(min, (ushort)(dist[idx + 1] + 1));
                    if (y < h - 1)
                        min = Math.Min(min, (ushort)(dist[idx + w] + 1));
                    if (x < w - 1 && y < h - 1)
                        min = Math.Min(min, (ushort)(dist[idx + w + 1] + 1));
                    if (x > 0 && y < h - 1)
                        min = Math.Min(min, (ushort)(dist[idx + w - 1] + 1));

                    if (min > maxDepth)
                        min = maxDepth;

                    dist[idx] = min;
                }
            }

            return dist;
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

            using var paint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Shader = shader,
                IsAntialias = true
            };

            canvas.Save();
            canvas.ClipPath(HitPath, SKClipOperation.Intersect, true);
            canvas.DrawPath(HitPath, paint);
            canvas.Restore();
        }


        // -------------------------------------------------
        // Coastline rendering (outside landform)
        // -------------------------------------------------

        private void RenderCoastline(
            SKCanvas canvas,
            IReadOnlyList<CoastlineBand> bands)
        {
            if (bands.Count == 0)
                return;

            var bounds = Bounds;

            canvas.Save();
            canvas.ClipPath(HitPath, SKClipOperation.Difference, true);

            foreach (var band in bands)
            {
                RenderCoastBand(canvas, band, bounds);
            }

            canvas.Restore();
        }

        private void RenderCoastBand(
            SKCanvas canvas,
            CoastlineBand band,
            SKRect bounds)
        {
            float width = band.EndDistance - band.StartDistance;
            if (width <= 0)
                return;

            int steps = Math.Max(6, (int)(width / 6f));
            float maxRadius = MathF.Max(bounds.Width, bounds.Height);

            for (int i = 0; i < steps; i++)
            {
                float rawT = i / (float)(steps - 1);
                float t = MathF.Pow(rawT, band.FalloffPower);

                float distance = band.StartDistance + width * rawT;
                float alpha = band.Alpha * (1f - t);

                if (alpha <= 0.001f)
                    continue;

                using var paint = CreateCoastPaint(band, alpha);
                using var expanded = new SKPath(PerimeterPath);

                float scale = 1f + distance / maxRadius;

                expanded.Transform(
                    SKMatrix.CreateScale(
                        scale,
                        scale,
                        bounds.MidX,
                        bounds.MidY));

                canvas.DrawPath(expanded, paint);
            }
        }

        private static SKPaint CreateCoastPaint(
            CoastlineBand band,
            float alpha)
        {
            var color = band.Color.WithAlpha(
                (byte)(band.Color.Alpha * alpha));

            var colorShader = SKShader.CreateColor(color);
            SKShader shader = colorShader;

            if (band.TextureImage != null)
            {
                var textureShader = SKShader.CreateImage(
                    band.TextureImage,
                    SKShaderTileMode.Repeat,
                    SKShaderTileMode.Repeat);

                shader = SKShader.CreateCompose(
                    colorShader,
                    textureShader,
                    SKBlendMode.Modulate);
            }

            return new SKPaint
            {
                Style = SKPaintStyle.Fill,
                IsAntialias = true,
                Shader = shader
            };
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
            var center = LandformShadingSettings.ComputeCentroid(HitPath);

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
    }

}
