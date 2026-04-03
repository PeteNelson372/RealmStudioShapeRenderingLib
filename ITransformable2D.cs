using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public interface ITransformable2D
    {
        SKPoint Location { get; set; }
        float Rotation { get; set; }
        float Scale { get; set; }

        SKRect GetLocalBounds();
        SKPoint[] GetTransformedCorners();
    }
}
