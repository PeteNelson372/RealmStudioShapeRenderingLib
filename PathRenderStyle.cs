using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public class PathRenderStyle
    {
        public PathType MapPathType { get; set; } = PathType.SolidLinePath;

        // -------------------------------------------------
        // Core appearance
        // -------------------------------------------------

        public float Width { get; set; } = 6f;

        public SKColor Color { get; set; } = SKColors.Black;

        public float Opacity { get; set; } = 1f;

        // -------------------------------------------------
        // Border / outline
        // -------------------------------------------------

        public bool HasBorder { get; set; } = false;

        public float BorderWidth { get; set; } = 2f;

        public SKColor BorderColor { get; set; } = SKColors.Black;

        // -------------------------------------------------
        // Dash / pattern
        // -------------------------------------------------

        public float[]? DashPattern { get; set; }

        public float DashPhase { get; set; } = 0f;

        // -------------------------------------------------
        // Gradient
        // -------------------------------------------------

        public bool UseGradient { get; set; } = false;

        public SKColor GradientStart { get; set; } = SKColors.White;

        public SKColor GradientEnd { get; set; } = SKColors.Black;

        // -------------------------------------------------
        // Texture
        // -------------------------------------------------

        public bool UseTexture { get; set; } = false;

        public string TextureId { get; set; } = string.Empty;

        public SKImage? Texture { get; set; }

        public float TextureScale { get; set; } = 1f;

        public float TextureRotation { get; set; } = 0f;

        public float TextureOpacity { get; set; } = 1f;

        // -------------------------------------------------
        // Marker-based paths (tracks, footprints, etc.)
        // -------------------------------------------------

        public bool UseMarkers { get; set; } = false;

        public SKPicture? Marker { get; set; }

        public float MarkerSpacing { get; set; } = 20f;

        public float MarkerScale { get; set; } = 1f;

        public bool AlternateMarkerFlip { get; set; } = false;

        // -------------------------------------------------
        // Wall / structure rendering
        // -------------------------------------------------

        public float StructureSize { get; set; } = 8f;

        public float StructureSpacing { get; set; } = 20f;

        // -------------------------------------------------
        // One-sided rendering (cliffs, borders, etc.)
        // -------------------------------------------------

        public bool OneSided { get; set; } = false;

        public bool FlipSide { get; set; } = false;

        // -------------------------------------------------
        // Stroke behavior
        // -------------------------------------------------

        public SKStrokeCap StrokeCap { get; set; } = SKStrokeCap.Round;

        public SKStrokeJoin StrokeJoin { get; set; } = SKStrokeJoin.Round;

        // -------------------------------------------------
        // Cached helpers (optional)
        // -------------------------------------------------

        public SKPaint CreateStrokePaint()
        {
            var paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = Width,
                Color = Color.WithAlpha((byte)(Opacity * 255)),
                IsAntialias = true,
                StrokeCap = StrokeCap,
                StrokeJoin = StrokeJoin
            };

            if (DashPattern != null)
            {
                paint.PathEffect = SKPathEffect.CreateDash(DashPattern, DashPhase);
            }

            return paint;
        }

        public SKPaint CreateBorderPaint()
        {
            return new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = Width + BorderWidth * 2,
                Color = BorderColor,
                IsAntialias = true,
                StrokeCap = StrokeCap,
                StrokeJoin = StrokeJoin
            };
        }

        public SKPaint CreateGradientPaint(SKPath path)
        {
            var bounds = path.Bounds;

            return new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = Width,
                IsAntialias = true,
                Shader = SKShader.CreateLinearGradient(
                    new SKPoint(bounds.Left, bounds.Top),
                    new SKPoint(bounds.Right, bounds.Bottom),
                    new[] { GradientStart, GradientEnd },
                    null,
                    SKShaderTileMode.Clamp)
            };
        }

        public SKPaint CreateTexturePaint()
        {
            if (Texture == null)
                throw new InvalidOperationException("Texture is null");

            var matrix = SKMatrix.CreateScale(TextureScale, TextureScale);

            if (TextureRotation != 0f)
            {
                var rot = SKMatrix.CreateRotationDegrees(TextureRotation);
                SKMatrix.Concat(ref matrix, matrix, rot);
            }

            return new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = Width,
                IsAntialias = true,
                Shader = SKShader.CreateBitmap(
                    SKBitmap.FromImage(Texture),
                    SKShaderTileMode.Repeat,
                    SKShaderTileMode.Repeat,
                    matrix)
            };
        }

        // -------------------------------------------------
        // Clone (important for undo/redo safety)
        // -------------------------------------------------

        public PathRenderStyle Clone()
        {
            return new PathRenderStyle
            {
                MapPathType = MapPathType,

                Width = Width,
                Color = Color,
                Opacity = Opacity,

                HasBorder = HasBorder,
                BorderWidth = BorderWidth,
                BorderColor = BorderColor,

                DashPattern = DashPattern != null ? (float[])DashPattern.Clone() : null,
                DashPhase = DashPhase,

                UseGradient = UseGradient,
                GradientStart = GradientStart,
                GradientEnd = GradientEnd,

                UseTexture = UseTexture,
                TextureId = TextureId,
                Texture = Texture,
                TextureScale = TextureScale,
                TextureRotation = TextureRotation,
                TextureOpacity = TextureOpacity,

                UseMarkers = UseMarkers,
                Marker = Marker,
                MarkerSpacing = MarkerSpacing,
                MarkerScale = MarkerScale,
                AlternateMarkerFlip = AlternateMarkerFlip,

                StructureSize = StructureSize,
                StructureSpacing = StructureSpacing,

                OneSided = OneSided,
                FlipSide = FlipSide,

                StrokeCap = StrokeCap,
                StrokeJoin = StrokeJoin
            };
        }
    }
}

