using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public class PlacedBoxState : IShapeState
    {
        public MapBox? BaseBox;

        public SKBitmap? BoxBitmap { get; set; }

        public SKColor BoxTint { get; set; } = SKColors.White;

        public float BoxCenterLeft { get; set; }
        public float BoxCenterTop { get; set; }
        public float BoxCenterRight { get; set; }
        public float BoxCenterBottom { get; set; }

        public SKPoint TopLeft { get; set; }
        public SKPoint BottomRight { get; set; }
        public float Rotation { get; set; }
        public float Scale { get; set; } = 1f;
        public bool Mirror { get; set; }
    }
}
