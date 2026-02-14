namespace RealmStudioShapeRenderingLib
{
    using SkiaSharp;
    using System;
    using System.Collections.Generic;

    public sealed class Landform : PaintedShape
    {
        // -------------------------------------------------
        // Serialized settings (data-only)
        // -------------------------------------------------
        public string LandformName { get; set; } = string.Empty;

        public string LandformDescription { get; set; } = string.Empty;

        public string WorldAnvilArticleId { get; set; } = string.Empty;

        public CoastlineSettings Coastline { get; set; } = new();
        public LandformShadingSettings Shading { get; set; } = new();

        // -------------------------------------------------
        // Runtime-resolved state
        // -------------------------------------------------

        private ICoastlineStyle? _resolvedCoastlineStyle;
        private IReadOnlyList<CoastlineBand>? _resolvedCoastlineBands;
        private SKImage? _resolvedFillTexture;

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

            if (Shading.FillWithTexture &&
                !string.IsNullOrEmpty(Shading.LandformTextureId))
            {
                _resolvedFillTexture =
                    assets.GetImage(Shading.LandformTextureId);
            }

            _renderDirty = true;
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

            using var recorder = new SKPictureRecorder();
            var canvas = recorder.BeginRecording(Bounds);

            if (_resolvedCoastlineBands != null)
            {
                RenderCoastline(canvas, _resolvedCoastlineBands);
            }

            RenderBaseFill(canvas);
            RenderInteriorGradient(canvas);
            RenderOutline(canvas);

            _renderCache = recorder.EndRecording();
            _renderDirty = false;
        }

        private void RenderBaseFill(SKCanvas canvas)
        {
            SKShader shader;

            // Base color shader (always present)
            var colorShader = SKShader.CreateColor(Shading.LandformBackgroundColor);

            if (Shading.FillWithTexture && _resolvedFillTexture != null)
            {
                var textureShader = SKShader.CreateImage(
                    _resolvedFillTexture,
                    SKShaderTileMode.Repeat,
                    SKShaderTileMode.Repeat);

                // Color × texture
                shader = SKShader.CreateCompose(
                    colorShader,
                    textureShader,
                    SKBlendMode.Modulate);
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
        // Interior shading (centroid-based, configurable)
        // -------------------------------------------------

        private void RenderInteriorGradient(SKCanvas canvas)
        {
            var s = Shading;

            if (!s.EnableInteriorShading || s.Steps <= 0)
                return;

            var center = LandformShadingSettings.ComputeCentroid(HitPath);
            int steps = Math.Max(1, s.Steps);
            float maxRadius = s.LandShadingDepth;

            canvas.Save();
            canvas.ClipPath(HitPath, SKClipOperation.Intersect, true);

            for (int i = 0; i < steps; i++)
            {
                float rawT = i / (float)(steps - 1);
                float t = MathF.Pow(rawT, s.FalloffPower);

                float radius = maxRadius * (1f - rawT);

                var color = Utilities.LerpColor(
                    s.LandformOutlineColor,
                    s.LandformBackgroundColor,
                    t);

                byte alpha = (byte)(
                    s.MaxAlpha +
                    (s.MinAlpha - s.MaxAlpha) * t);

                using var paint = new SKPaint
                {
                    Style = SKPaintStyle.Fill,
                    Color = color.WithAlpha(alpha),
                    IsAntialias = true
                };

                canvas.DrawCircle(center, radius, paint);
            }

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
    }

}
