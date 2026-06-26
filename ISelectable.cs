using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public interface ISelectable
    {
        public string Id { get; }
        public bool IsSelected { get; set; }
        public SKRect Bounds { get; }

        bool HitTest(SKPoint worldPos);
    }
}
