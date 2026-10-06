using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public class ImportLandform : Shape2D
    {
        public ImportLandform(SKPath geometry)
        {
            ArgumentNullException.ThrowIfNull(geometry);

            RestoreGeometry(geometry);
        }

        public override void Render(SKCanvas canvas, FontManager? fontManager, SKPath? clipPath = null)
        {
                canvas.DrawPath(HitPath, PaintObjects.DebugPaint2);
        }
    }
}
