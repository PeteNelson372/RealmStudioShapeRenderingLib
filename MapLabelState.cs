using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public class MapLabelState : IShapeState
    {
        public string Text { get; set; } = string.Empty;

        public SKPoint Location { get; set; }
        public float Rotation { get; set; }
        public float Scale { get; set; } = 1f;
        public bool Mirror { get; set; }

        public FontStyleModel FontStyle { get; set; } = new();

        public SKColor FontColor { get; set; } = new SKColor(61, 53, 30);

        public bool HasOutline { get; set; }
        public float OutlineWidth { get; set; }
        public SKColor OutlineColor { get; set; }

        public bool HasGlow { get; set; }
        public float GlowStrength { get; set; }
        public SKColor GlowColor { get; set; }

        // path/curve data
        public SKPath? CurvePath { get; set; }
    }
}
