namespace RealmStudioShapeRenderingLib
{
    using SkiaSharp;
    using System;
    using System.Xml.Serialization;

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
        [XmlElement]
        public string LandformName { get; set; } = string.Empty;
        [XmlElement]
        public string LandformDescription { get; set; } = string.Empty;
        [XmlElement]
        public string WorldAnvilArticleId { get; set; } = string.Empty;
        [XmlElement]
        public CoastlineSettings Coastline { get; set; } = new();
        [XmlElement]
        public LandformShadingSettings Shading { get; set; } = new();
        [XmlIgnore]
        public LandformRenderMode RenderMode { get; set; } = LandformRenderMode.Final;
        [XmlIgnore]
        private SKImage? _interiorShadingMask;

        // -------------------------------------------------
        // Runtime-resolved state
        // -------------------------------------------------
        [XmlIgnore]
        private SKImage? _resolvedFillTexture;
        [XmlIgnore]
        private SKShader? _resolvedTextureShader;
        [XmlIgnore]
        private SKPicture? _interiorCache;
        [XmlIgnore]
        private SKPicture? _coastlineCache;
        [XmlIgnore]
        public SKPicture? InteriorPicture => _interiorCache;
        [XmlIgnore]
        public SKPicture? CoastlinePicture => _coastlineCache;
        [XmlIgnore]
        private bool _renderModified = true;
        [XmlIgnore]
        public bool IsInteractive { get; private set; }

        public override void FinalizeShapeGeometry(RealmStudioMap map)
        {
            RebuildPerimeter();
        }

        public void CloneSettingsFrom(Landform source)
        {
            Shading = source.Shading.Clone();
            Coastline = source.Coastline.Clone();

            LandformName = source.LandformName;
            LandformDescription = source.LandformDescription;
        }

        public void BeginInteractive()
        {
            IsInteractive = true;
        }

        public void EndInteractive()
        {
            RenderMode = LandformRenderMode.Final;
            IsInteractive = false;

            _interiorCache?.Dispose();
            _interiorCache = null;

            _coastlineCache?.Dispose();
            _coastlineCache = null;

            InvalidateRenderCache();
        }

        protected override void RebuildPerimeter()
        {
            base.RebuildPerimeter();
            InvalidateRenderCache();
        }

        public void InvalidateRenderCache()
        {
            _renderModified = true;
        }

        public void RenderCoastlineFinal(SKCanvas canvas)
        {
            if (_coastlineCache == null || _renderModified)
            {
                RebuildRenderCache();
            }

            if (_coastlineCache != null)
            {
                canvas.DrawPicture(_coastlineCache);
            }
        }

        public void RenderInteriorFinal(SKCanvas canvas)
        {
            if (_interiorCache == null || _renderModified)
            {
                RebuildRenderCache();
            }

            if (_interiorCache != null)
            {
                canvas.DrawPicture(_interiorCache);
            }

        }

        public void RenderCoastlineInteractive(SKCanvas canvas)
        {
            RenderCoastline(canvas);   // live perimeter-based rendering
        }

        public void RenderInteriorFast(SKCanvas canvas)
        {
            RenderFast(canvas);        // radial gradient version
        }

        // -------------------------------------------------
        // Geometry management
        // -------------------------------------------------

        public void ClearGeometry()
        {
            SetGeometry(new SKPath());
        }

        public void ReplaceGeometry(SKPath newPath)
        {
            if (newPath == null || newPath.IsEmpty)
            {
                ClearGeometry();
                return;
            }

            SetGeometry(new SKPath(newPath));
        }

        protected override void SetGeometry(SKPath path)
        {
            base.SetGeometry(path);
            InvalidateRenderCache();
        }

        protected override void OnGeometryChanged()
        {
            base.OnGeometryChanged();
            InvalidateRenderCache();
        }

        // -------------------------------------------------
        // Rendering
        // -------------------------------------------------
        public override void Render(SKCanvas canvas, FontManager? _, SKPath? clipPath = null)
        {
            // the Render method only handles interactive mode
            if (HitPath.IsEmpty)
            {
                return;
            }

            if (RenderMode == LandformRenderMode.Interactive)
            {
                RenderCoastline(canvas);
                RenderFast(canvas);
            }
        }

        public void RenderInteriorPass(SKCanvas canvas)
        {
            if (HitPath.IsEmpty)
            {
                return;
            }

            if (RenderMode == LandformRenderMode.Final)
            {
                if (_renderModified)
                {
                    RebuildRenderCache();
                }

                if (_interiorCache != null)
                {
                    canvas.DrawPicture(_interiorCache, 0, 0);
                }
            }
        }

        public void RenderCoastlinePass(SKCanvas canvas)
        {
            if (HitPath.IsEmpty)
            {
                return;
            }

            if (RenderMode == LandformRenderMode.Final)
            {
                if (_renderModified)
                {
                    RebuildRenderCache();
                }

                if (_coastlineCache != null)
                {
                    canvas.DrawPicture(_coastlineCache);
                }
            }
        }

        public void RenderLandformForHeightMap(
                RealmStudioMap map,
                SKCanvas canvas)
        {
            MapLayer heightMapLayer = MapBuilder.GetMapLayerByIndex(map, MapBuilder.HEIGHTMAPLAYER);

            if (heightMapLayer.Shapes.Count == 0)
            {
                return;
            }


            if (heightMapLayer.Shapes[0] is not MapHeightMap heightMap || heightMap.HeightMap == null)
            {
                return;
            }

            if (heightMap.HeightMapPalette == null)
            {
                return;
            }

            SKRect clipBounds = new(
                0,
                0,
                map.MapWidth,
                map.MapHeight);

            canvas.Save();

            // Don't allow anything to be rendered outside the map.
            canvas.ClipRect(clipBounds);

            // Clip the heightmap rendering to this landform.
            canvas.ClipPath(PerimeterPath);

            PerimeterPath.GetBounds(out SKRect landformBounds);

            int left = Math.Max(0, (int)Math.Floor(landformBounds.Left));
            int top = Math.Max(0, (int)Math.Floor(landformBounds.Top));

            int right = Math.Min(
                map.MapWidth - 1,
                (int)Math.Ceiling(landformBounds.Right));

            int bottom = Math.Min(
                map.MapHeight - 1,
                (int)Math.Ceiling(landformBounds.Bottom));

            if (left <= right && top <= bottom)
            {
                using SKBitmap bitmap = new(
                    new SKImageInfo(
                        right - left + 1,
                        bottom - top + 1,
                        SKColorType.Rgba8888,
                        SKAlphaType.Premul));

                heightMap.UpdateHeightMapBitmap(bitmap, heightMap.HeightMap, left, top, right, bottom);
                
                canvas.DrawBitmap(bitmap, left, top, SKSamplingOptions.Default);
            }


            canvas.Restore();

            // Draw the landform perimeter on top of the heightmap.
            canvas.DrawPath(
                PerimeterPath,
                PaintObjects.LandformHeightMapOutlinePaint);

            if (IsSelected)
            {
                PerimeterPath.GetBounds(out SKRect boundsRect);

                canvas.DrawRect(
                    boundsRect,
                    PaintObjects.LandformSelectPaint);
            }
        }

        // -------------------------------------------------
        // Render cache construction
        // -------------------------------------------------

        private void RebuildRenderCache()
        {
            _interiorCache?.Dispose();
            _interiorCache = null;

            _coastlineCache?.Dispose();
            _coastlineCache = null;

            _interiorCache = BuildInteriorPicture();
            _coastlineCache = BuildCoastlinePicture();

            _renderModified = false;
        }

        private SKPicture BuildInteriorPicture()
        {
            using var recorder = new SKPictureRecorder();
            var canvas = recorder.BeginRecording(Bounds);

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

            return recorder.EndRecording();
        }
        private SKPicture BuildCoastlinePicture()
        {
            using var recorder = new SKPictureRecorder();
            var canvas = recorder.BeginRecording(Bounds);

            RenderCoastline(canvas);

            return recorder.EndRecording();
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

            ushort[] dist = ComputeDistanceFieldFast(bitmap, w, h, maxDepth);

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

        private void RenderFast(SKCanvas canvas)
        {
            RenderBaseFill(canvas);

            var center = new SKPoint(Bounds.MidX, Bounds.MidY);

            float radius = Math.Max(Bounds.Width, Bounds.Height) * 0.5f;

            using var shader = SKShader.CreateRadialGradient(
                center,
                radius,
                [
                    Shading.LandformBackgroundColor.WithAlpha(0),
                    Shading.LandformOutlineColor.WithAlpha(Shading.MaxAlpha),
                ],
                [0f, 1f],
                SKShaderTileMode.Clamp);

            var paint = PaintObjects.LandformRenderFastPaint;
            paint.Shader = shader;

            using (new SKAutoCanvasRestore(canvas))
            {
                canvas.ClipPath(HitPath, SKClipOperation.Intersect, true);
                canvas.DrawRect(Bounds, paint);

                RenderOutline(canvas);
            }
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

            SKPaint p = PaintObjects.LandBaseFillPaint;
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
                case LandformCoastlineStyle.UniformBand:
                    RenderUniformBand(canvas);
                    break;
                case LandformCoastlineStyle.UniformBlend:
                    RenderUniformBlend(canvas);
                    break;
                case LandformCoastlineStyle.UniformOutline:
                    RenderUniformOutline(canvas);
                    break;
                case LandformCoastlineStyle.ThreeTiered:
                    RenderThreeTiered(canvas);
                    break;
                case LandformCoastlineStyle.RipplePattern:
                    RenderRipplePatternCoastline(canvas);
                    break;
                case LandformCoastlineStyle.DashPattern:
                    if (Coastline.DashTexture != null)
                    {
                        RenderDashPattern(canvas, Coastline.DashTexture);
                    }
                    else
                    {
                        // Fallback to uniform band if texture is missing
                        RenderUniformBand(canvas);
                    }

                    break;
                case LandformCoastlineStyle.HatchPattern:
                    if (Coastline.HatchTexture != null)
                    {
                        RenderHatchPattern(canvas, Coastline.HatchTexture);
                    }
                    else
                    {
                        // Fallback to uniform band if texture is missing
                        RenderUniformBand(canvas);
                    }
                    break;
                case LandformCoastlineStyle.UserDefined:
                    break; // TODO: user-defined rendering
                default:
                    throw new NotSupportedException($"Unsupported coastline style: {Coastline.CoastlineStyle}");
            }
        }


        private void RenderUniformOutline(SKCanvas canvas)
        {
            if (PerimeterPath == null || PerimeterPath.IsEmpty)
                return;

            float depth = Coastline.EffectDistance;
            if (depth <= 0f)
                return;

            float thickWidth = depth;

            // Adjustable outer ring ratio (0.10f – 0.20f works well)
            float outerRatio = Coastline.UniformOutlineOuterRingRatio;
            float thinWidth = depth * outerRatio;

            var baseColor = Coastline.CoastlineColor;

            var lightColor = baseColor.WithAlpha(100);
            var darkColor = baseColor.WithAlpha(220);

            canvas.Save();
            canvas.ClipPath(HitPath, SKClipOperation.Difference, true);

            // --- Thick band ---
            using var thickStrokePaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = thickWidth,
                StrokeJoin = SKStrokeJoin.Round,
                StrokeCap = SKStrokeCap.Round,
                IsAntialias = true
            };

            using SKPathBuilder thickPathBuilder = new();

            thickStrokePaint.GetFillPath(PerimeterPath, thickPathBuilder, 1.0f);
            var thickBand = thickPathBuilder.Snapshot();
            thickPathBuilder.Detach();

            // --- Outer band total ---
            using var outerStrokePaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = thickWidth + thinWidth * 2f,
                StrokeJoin = SKStrokeJoin.Round,
                StrokeCap = SKStrokeCap.Round,
                IsAntialias = true
            };

            using SKPathBuilder outerBandPathBuilder = new();

            outerStrokePaint.GetFillPath(PerimeterPath, outerBandPathBuilder, 1.0f);
            var outerBandTotal = outerBandPathBuilder.Snapshot();
            outerBandPathBuilder.Detach();

            // --- Subtract to isolate thin ring ---
            using var darkBand = outerBandTotal.Op(thickBand, SKPathOp.Difference);

            // --- Render ---
            using var lightPaint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = lightColor,
                IsAntialias = true
            };

            using var darkPaint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = darkColor,
                IsAntialias = true
            };

            if (thickBand != null && thickBand.PointCount > 3 && darkBand != null && darkBand.PointCount > 3)
            {
                canvas.DrawPath(thickBand, lightPaint);
                canvas.DrawPath(darkBand, darkPaint);
            }

            canvas.Restore();
        }

        private void RenderBaseCoastFade(SKCanvas canvas)
        {
            RenderStrokeFade(
                canvas,
                Coastline.EffectDistance,
                24,
                t =>
                {
                    float fade = 1f - t;
                    fade = MathF.Pow(fade, Coastline.FalloffPower * 0.9f);

                    return new SKPaint
                    {
                        Color = Coastline.CoastlineColor
                            .WithAlpha((byte)(Coastline.MaxAlpha * fade))
                    };
                });
        }

        private void RenderRipplePatternCoastline(SKCanvas canvas)
        {
            float depth = Coastline.EffectDistance;

            float wavelength = 12f;      // closer rings
            float ringThickness = 4f;

            int ringCount = (int)(depth / wavelength);

            canvas.Save();
            canvas.ClipPath(HitPath, SKClipOperation.Difference, true);

            // 1️. Draw soft base fade
            RenderBaseCoastFade(canvas);

            // 2️. Draw ripple crests
            for (int i = 1; i <= ringCount; i++)
            {
                float centerDistance = i * wavelength;

                float outerWidth = centerDistance + ringThickness * 0.5f;
                float innerWidth = centerDistance - ringThickness * 0.5f;
                if (innerWidth < 0) innerWidth = 0;

                float t = centerDistance / depth;
                float fade = 1f - t;
                fade = MathF.Pow(fade, Coastline.FalloffPower);

                // Crest stronger than base
                byte alpha = (byte)Math.Min(
                    255,
                    Coastline.MaxAlpha * 1.4f * fade);

                using var outerPaint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = outerWidth * 2f,
                    IsAntialias = true
                };

                using var innerPaint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = innerWidth * 2f,
                    IsAntialias = true
                };

                using SKPathBuilder outerPathBuilder = new();

                outerPaint.GetFillPath(PerimeterPath, outerPathBuilder, 1.0f);
                var outerPath = outerPathBuilder.Snapshot();
                outerPathBuilder.Detach();

                using SKPathBuilder innerPathBuilder = new();

                innerPaint.GetFillPath(PerimeterPath, innerPathBuilder, 1.0f);
                var innerPath = outerPathBuilder.Snapshot();
                innerPathBuilder.Detach();

                using var ringPath = outerPath.Op(innerPath, SKPathOp.Difference);

                if (ringPath != null && ringPath.PointCount > 0)
                {
                    var paint = PaintObjects.LandformRippleRingPaint;
                    paint.Color = Coastline.CoastlineColor.WithAlpha(alpha);

                    canvas.DrawPath(ringPath, paint);
                }
            }

            canvas.Restore();
        }

        private void RenderStrokeFade(
            SKCanvas canvas,
            float depth,
            int steps,
            Func<float, SKPaint> paintFactory)
        {
            canvas.Save();

            // Only draw outside land
            canvas.ClipPath(HitPath, SKClipOperation.Difference, true);

            for (int i = 0; i < steps; i++)
            {
                float t = i / (float)(steps - 1);

                float width = depth * t;

                using var paint = paintFactory(t);
                paint.Style = SKPaintStyle.Stroke;
                paint.StrokeWidth = width;
                paint.IsAntialias = true;

                canvas.DrawPath(PerimeterPath, paint);
            }

            canvas.Restore();
        }

        private void RenderUniformBlend(SKCanvas canvas)
        {
            RenderStrokeFade(
                canvas,
                Coastline.EffectDistance,
                24,
                t =>
                {
                    float fade = 1f - t;
                    fade = MathF.Pow(fade, Coastline.FalloffPower);

                    return new SKPaint
                    {
                        Color = Coastline.CoastlineColor
                            .WithAlpha((byte)(Coastline.MaxAlpha * fade)),
                        StrokeCap = SKStrokeCap.Round,
                        StrokeJoin = SKStrokeJoin.Round
                    };
                });
        }

        private void RenderUniformBand(SKCanvas canvas)
        {
            var paint = PaintObjects.CoastlineBasePaint;
            paint.StrokeWidth = Coastline.EffectDistance;
            paint.Color = Coastline.CoastlineColor.WithAlpha(Coastline.MaxAlpha);
            paint.StrokeCap = SKStrokeCap.Round;
            paint.StrokeJoin = SKStrokeJoin.Round;

            canvas.Save();
            canvas.ClipPath(HitPath, SKClipOperation.Difference, true);
            canvas.DrawPath(PerimeterPath, paint);
            canvas.Restore();
        }

        private void RenderThreeTiered(SKCanvas canvas)
        {
            float depth = Coastline.EffectDistance;

            float[] bands = { 0.33f, 0.66f, 1f };
            byte[] alphas = { 180, 100, 40 };

            canvas.Save();
            canvas.ClipPath(HitPath, SKClipOperation.Difference, true);

            for (int i = 0; i < bands.Length; i++)
            {
                var paint = PaintObjects.CoastlineBasePaint;
                paint.StrokeWidth = depth * bands[i];
                paint.Color = Coastline.CoastlineColor.WithAlpha(alphas[i]);
                paint.StrokeCap = SKStrokeCap.Round;
                paint.StrokeJoin = SKStrokeJoin.Round;

                canvas.DrawPath(PerimeterPath, paint);
            }

            canvas.Restore();
        }

        private void RenderHatchPattern(SKCanvas canvas, SKImage hatchTexture)
        {
            float depth = Coastline.EffectDistance;
            int steps = 24;

            RenderStrokeFade(
                canvas,
                depth,
                steps,
                t =>
                {
                    float fade = 1f - t;
                    fade = MathF.Pow(fade, Coastline.FalloffPower);

                    byte alpha = (byte)(Coastline.MaxAlpha * fade);

                    // Base color shader (with falloff alpha)
                    var colorShader = SKShader.CreateColor(
                        Coastline.CoastlineColor.WithAlpha(alpha));

                    // Hatch texture shader
                    var hatchShader = SKShader.CreateImage(
                        hatchTexture,
                        SKShaderTileMode.Repeat,
                        SKShaderTileMode.Repeat);

                    // Combine color shading × texture
                    var composed = SKShader.CreateCompose(
                        colorShader,
                        hatchShader,
                        SKBlendMode.Modulate);

                    return new SKPaint
                    {
                        Shader = composed,
                        IsAntialias = true,
                        StrokeCap = SKStrokeCap.Round,
                        StrokeJoin = SKStrokeJoin.Round,
                    };
                });
        }

        private void RenderDashPattern(SKCanvas canvas, SKImage dashTexture)
        {
            float depth = Coastline.EffectDistance;
            int steps = 24;

            RenderStrokeFade(
                canvas,
                depth,
                steps,
                t =>
                {
                    float fade = 1f - t;
                    fade = MathF.Pow(fade, Coastline.FalloffPower);

                    byte alpha = (byte)(Coastline.MaxAlpha * fade);

                    // Base color shader (with falloff alpha)
                    var colorShader = SKShader.CreateColor(
                        Coastline.CoastlineColor.WithAlpha(alpha));

                    // Dash texture shader
                    var dashShader = SKShader.CreateImage(
                        dashTexture,
                        SKShaderTileMode.Repeat,
                        SKShaderTileMode.Repeat);

                    // Combine color shading × texture
                    var composed = SKShader.CreateCompose(
                        colorShader,
                        dashShader,
                        SKBlendMode.Modulate);

                    return new SKPaint
                    {
                        Shader = composed,
                        IsAntialias = true,
                        StrokeCap = SKStrokeCap.Round,
                        StrokeJoin = SKStrokeJoin.Round,
                    };
                });
        }


        // -------------------------------------------------
        // Interior shading
        // -------------------------------------------------

        private void RenderInteriorShading(SKCanvas canvas)
        {
            if (_interiorShadingMask == null)
                return;

            var bounds = HitPath.Bounds;

            var paint = PaintObjects.LandformInteriorShadingPaint;
            paint.Color = Shading.LandformOutlineColor;

            canvas.Save();
            canvas.ClipPath(HitPath, SKClipOperation.Intersect, true);
            canvas.DrawImage(_interiorShadingMask, bounds.Left, bounds.Top, SKSamplingOptions.Default, paint);
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
                [inlandColor, coastColor],
                [0f, 1f],
                SKShaderTileMode.Clamp);

            var paint = PaintObjects.LandformInteriorGradientPaint;
            paint.Shader = shader;

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
            // Resolve coastline style
            if (Coastline.HatchTextureId != null && Coastline.HatchTexture == null)
            {
                SKImage? hatchImage = assets.GetImage(Coastline.HatchTextureId);

                if (hatchImage != null)
                {
                    SKBitmap hatchBitmap = SKBitmap.FromImage(hatchImage);

                    SKBitmap resizedSKBitmap = hatchBitmap.Resize(new SKImageInfo(100, 100), SKSamplingOptions.Default);

                    Coastline.HatchTexture = SKImage.FromBitmap(resizedSKBitmap);
                }
            }

            if (Coastline.DashTextureId != null && Coastline.DashTexture == null)
            {
                SKImage? dashImage = assets.GetImage(Coastline.DashTextureId);

                if (dashImage != null)
                {
                        SKBitmap dashBitmap = SKBitmap.FromImage(dashImage);

                        SKBitmap resizedSKBitmap = dashBitmap.Resize(new SKImageInfo(100, 100), SKSamplingOptions.Default);

                        Coastline.DashTexture = SKImage.FromBitmap(resizedSKBitmap);
                }
            }

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
