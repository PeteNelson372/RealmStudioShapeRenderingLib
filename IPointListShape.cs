using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public interface IPointListShape : IDrawnMapComponent
    {
        List<SKPoint> Points { get; set; }
        SKRect Bounds { get; set; }
    }
}
