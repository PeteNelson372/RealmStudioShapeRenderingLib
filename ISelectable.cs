using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public interface ISelectable
    {
        public bool IsSelected { get; set; }
        public SKRect Bounds { get; }

        bool HitTest(SKPoint worldPos);
    }
}
