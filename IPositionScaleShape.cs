using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public interface IPositionImageShape : IDrawnMapComponent
    {
        SKPoint TopLeft { get; set; }
        float Scale { get; set; }
        SKImage StampImage { get; set; }
    }
}
