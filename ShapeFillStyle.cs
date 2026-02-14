#nullable enable

using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public class ShapeFillStyle
    {
        public ColorTextureMode FillStyle { get; set; } = ColorTextureMode.Color;
        public SKColor FillColor { get; set; } = SKColors.Transparent;
        public ShapeTexture? FillTexture { get; set; } = null;
    }
}
