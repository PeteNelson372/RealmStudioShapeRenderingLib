using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public class RegionRenderStyle
    {
        public PathType MapPathType { get; set; } = PathType.SolidLinePath;


        // -------------------------------------------------
        // Core appearance
        // -------------------------------------------------

        public float Width { get; set; } = 6f;

        public SKColor Color { get; set; } = new SKColor(75, 49, 26, 255);

        public int Opacity { get; set; } = 255;

        // -------------------------------------------------
        // Border / outline
        // -------------------------------------------------

        public bool HasBorder { get; set; } = false;

        public float BorderWidth { get; set; } = 2f;

        public SKColor BorderColor { get; set; } = new SKColor(75, 49, 26, 255);

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
        // One-sided rendering (cliffs, borders, etc.)
        // -------------------------------------------------

        public bool OneSided { get; set; } = false;

        public bool FlipSide { get; set; } = false;

        // -------------------------------------------------
        // Stroke behavior
        // -------------------------------------------------

        public SKStrokeCap StrokeCap { get; set; } = SKStrokeCap.Round;

        public SKStrokeJoin StrokeJoin { get; set; } = SKStrokeJoin.Round;

        public int Smoothing { get; set; } = 0;


        // -------------------------------------------------
        // Clone (important for undo/redo safety)
        // -------------------------------------------------

        public RegionRenderStyle Clone()
        {
            return new RegionRenderStyle
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

                OneSided = OneSided,
                FlipSide = FlipSide,

                StrokeCap = StrokeCap,
                StrokeJoin = StrokeJoin,
                Smoothing = Smoothing
            };
        }
    }
}

