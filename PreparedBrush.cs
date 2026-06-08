using SkiaSharp;
using System.Xml.Serialization;

namespace RealmStudioShapeRenderingLib
{
    public class PreparedBrush
    {
        [XmlElement]
        public MapBrush? SourceBrush { get; init; }

        [XmlElement]
        public SKColor Color { get; init; }

        [XmlElement]
        public int BrushSize { get; init; }

        [XmlElement]
        public int BrushSpacing { get; init; } = 0;

        [XmlIgnore]
        public List<SKBitmap> Bitmaps { get; init; } = [];
    }
}
