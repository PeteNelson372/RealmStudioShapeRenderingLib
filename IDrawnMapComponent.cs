using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public interface IDrawnMapComponent
    {
        public void Render(SKCanvas canvas, FontManager? fontManager = null, SKPath? clipPath = null);
    }
}
