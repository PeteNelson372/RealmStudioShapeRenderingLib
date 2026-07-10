using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public interface IRectangularShape : IDrawnMapComponent
    {
        SKPoint TopLeft { get; set; }
        SKPoint BottomRight { get; set; }
    }
}
