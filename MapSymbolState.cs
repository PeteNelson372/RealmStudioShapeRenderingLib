using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public class MapSymbolState : IShapeState
    {
        public SKPoint Location { get; init; }
        public float Rotation { get; init; }
        public float Scale { get; init; }
        public bool Flip { get; init; }
        public SKColor[] CustomColors { get; init; } = new SKColor[3];
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
    }
}
