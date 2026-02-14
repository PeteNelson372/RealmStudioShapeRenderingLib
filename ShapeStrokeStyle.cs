#nullable enable

using SkiaSharp;
namespace RealmStudioShapeRenderingLib
{
    public class ShapeStrokeStyle
    {
        public ColorTextureMode StrokeStyle { get; set; } = ColorTextureMode.Color;
        public SKColor StrokeColor { get; set; } = SKColors.Transparent;
        public float StrokeWidth { get; set; } = 1.0f;
        public ShapeTexture? StrokeTexture { get; set; } = null;
    }
}
