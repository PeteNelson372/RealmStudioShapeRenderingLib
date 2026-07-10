using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public interface ICenterRadiusShape : IDrawnMapComponent
    {
        SKPoint Center { get; set; }
        float Radius { get; set; }
    }
}
