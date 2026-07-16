using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public interface ITransformable2D: IRotatable
    {
        SKPoint Location { get; set; }
        float Scale { get; set; }

        SKRect GetLocalBounds();
        SKPoint[] GetTransformedCorners();

        void BeginScale();
        void ApplyScale(float factor);
    }
}
