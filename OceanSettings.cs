using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public class OceanSettings
    {
        public string? TextureId { get; set; }
        public float Scale { get; set; } = 1f;
        public bool Mirror { get; set; }
        public bool EnableCoastlineBlur { get; set; } = true;

        public float TextureOpacity { get; set; } = 1f;

        public bool ColorOverlayEnabled { get; set; }
        public SKColor OverlayColor { get; set; } = SKColors.Transparent;
    }
}
