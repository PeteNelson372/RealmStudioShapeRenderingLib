using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public class PreparedBrush
    {
        public MapBrush? SourceBrush { get; init; }

        public SKColor Color { get; init; }

        public int BrushSize { get; init; }

        public int BrushSpacing { get; init; } = 0;

        public List<SKBitmap> Bitmaps { get; init; } = [];
    }
}
