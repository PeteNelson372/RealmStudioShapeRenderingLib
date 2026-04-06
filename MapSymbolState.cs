using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public class MapSymbolState : IShapeState
    {
        public SKPoint Location { get; set; }
        public float Rotation { get; set; }
        public float Scale { get; set; }
        public bool Mirror { get; set; }
        public SKRect LocalBounds { get; set; }
        public SKColor TintColor { get; set; }
        public SKColor[] CustomColors { get; set; } = new SKColor[3];
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
