using SkiaSharp;
using System.Xml.Serialization;

namespace RealmStudioShapeRenderingLib
{
    public class PathRenderStyle
    {
        [XmlElement]
        public PathType MapPathType { get; set; } = PathType.SolidLinePath;

        // -------------------------------------------------
        // Core appearance
        // -------------------------------------------------

        [XmlElement]
        public float Width { get; set; } = 6f;

        [XmlElement]
        public SKColor Color { get; set; } = new SKColor(75, 49, 26, 255);

        [XmlElement]
        public float Opacity { get; set; } = 1f;

        // -------------------------------------------------
        // Border / outline
        // -------------------------------------------------

        [XmlElement]
        public bool HasBorder { get; set; } = false;

        [XmlElement]
        public float BorderWidth { get; set; } = 2f;

        [XmlElement]
        public SKColor BorderColor { get; set; } = new SKColor(75, 49, 26, 255);

        // -------------------------------------------------
        // Dash / pattern
        // -------------------------------------------------

        [XmlElement]
        public float[]? DashPattern { get; set; }

        [XmlElement]
        public float DashPhase { get; set; } = 0f;

        // -------------------------------------------------
        // Gradient
        // -------------------------------------------------

        [XmlElement]
        public bool UseGradient { get; set; } = false;

        [XmlElement]
        public SKColor GradientStart { get; set; } = SKColors.White;

        [XmlElement]
        public SKColor GradientEnd { get; set; } = SKColors.Black;

        // -------------------------------------------------
        // Texture
        // -------------------------------------------------

        [XmlElement]
        public bool UseTexture { get; set; } = false;

        [XmlElement]
        public string TextureId { get; set; } = string.Empty;

        [XmlIgnore]
        public SKBitmap? Texture { get; set; }

        [XmlElement]
        public float TextureScale { get; set; } = 1f;

        [XmlElement]
        public float TextureRotation { get; set; } = 0f;

        [XmlElement]
        public float TextureOpacity { get; set; } = 1f;

        // -------------------------------------------------
        // Marker-based paths (tracks, footprints, etc.)
        // -------------------------------------------------

        [XmlElement]
        public bool UseMarkers { get; set; } = false;

        [XmlIgnore]
        public SKPicture? Marker { get; set; }

        [XmlElement]
        public float MarkerSpacing { get; set; } = 20f;

        [XmlElement]
        public float MarkerScale { get; set; } = 1f;

        [XmlElement]
        public bool AlternateMarkerFlip { get; set; } = false;

        [XmlElement]
        public float ChevronSpacing { get; set; } = 24f;

        [XmlElement]
        public float RailOffset { get; set; } = 4f;

        [XmlElement]
        public float TieSpacing{ get; set; } = 4f;

        [XmlElement]
        public float TieOverhang{ get; set; } = 2f;

        [XmlElement]
        public float TowerDistance { get; set; } = 10f;

        [XmlElement]
        public float TowerSize { get; set; } = 1.2f;

        [XmlElement]
        public bool DrawCrenelations { get; set; } = true;

        [XmlElement]
        public float CrenelWidthFactor { get; set; } = 0.4f;

        [XmlElement]
        public float CrenelHeightFactor { get; set; } = 0.3f;

        // -------------------------------------------------
        // Wall / structure rendering
        // -------------------------------------------------

        [XmlElement]
        public float StructureSize { get; set; } = 8f;

        [XmlElement]
        public float StructureSpacing { get; set; } = 20f;

        // -------------------------------------------------
        // One-sided rendering (cliffs, borders, etc.)
        // -------------------------------------------------

        [XmlElement]
        public bool OneSided { get; set; } = false;

        [XmlElement]
        public bool FlipSide { get; set; } = false;

        // -------------------------------------------------
        // Stroke behavior
        // -------------------------------------------------

        [XmlElement]
        public SKStrokeCap StrokeCap { get; set; } = SKStrokeCap.Round;

        [XmlElement]
        public SKStrokeJoin StrokeJoin { get; set; } = SKStrokeJoin.Round;

        [XmlElement]
        public int Smoothing { get; set; } = 0;

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
                ChevronSpacing = ChevronSpacing,
                RailOffset = RailOffset,
                TieOverhang = TieOverhang,
                TieSpacing = TieSpacing,
                TowerDistance = TowerDistance,
                TowerSize = TowerSize,
                DrawCrenelations = DrawCrenelations,
                CrenelHeightFactor = CrenelHeightFactor,
                CrenelWidthFactor = CrenelWidthFactor,

                StructureSize = StructureSize,
                StructureSpacing = StructureSpacing,

                OneSided = OneSided,
                FlipSide = FlipSide,

                StrokeCap = StrokeCap,
                StrokeJoin = StrokeJoin,
                Smoothing = Smoothing
            };
        }
    }
}

