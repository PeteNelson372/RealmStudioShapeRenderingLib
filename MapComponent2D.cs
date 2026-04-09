using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public abstract class MapComponent2D: IShape2D, ISelectable
    {
        public string Id { get; } = Guid.NewGuid().ToString();

        public bool IsSelected { get; set; }

        public virtual SKRect LocalBounds { get; set; }

        public virtual SKRect Bounds { get; set; } // world bounds

        public abstract void Render(SKCanvas canvas, FontManager? fontManager = null);

        public abstract bool HitTest(SKPoint worldPos);

        public abstract IShapeState CaptureState();

        public abstract void RestoreState(IShapeState state);
    }
}
