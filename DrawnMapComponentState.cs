using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public class DrawnMapComponentState : IShapeState
    {
        public SKPoint TopLeft {get; set;} = SKPoint.Empty;
        public SKPoint BottomRight { get; set; } = SKPoint.Empty;
        public SKPoint Center { get; set; } = SKPoint.Empty;
        public float Radius { get; set; } = 0;
        public List<SKPoint> Points { get; set; } = [];
        public SKColor ComponentColor { get; set; } = SKColors.Black;
        public SKColor FillColor { get; set; } = SKColors.Transparent;
        public float TextureOpacity { get; set; } = 1.0f;
        public float TextureScale { get; set; } = 1.0f;
        public int BrushSize { get; set; } = 2;
        public float Rotation { get; set; } = 0;
        public DrawingFillType FillType { get; set; } = DrawingFillType.None;
        public string FillImageId { get; set; } = string.Empty;
        public SKImage? FillImage { get; set; }
        public string StampPath { get; set; } = string.Empty;
        public SKImage StampImage { get; set; } = SKImage.FromBitmap(new SKBitmap());
        public PathRenderStyle? RenderStyle { get; set; }
        public bool DrawPathOverSymbols { get; set; } = false;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
