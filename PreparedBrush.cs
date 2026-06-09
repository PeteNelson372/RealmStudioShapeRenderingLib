using RealmStudioX.WPF.EditorUtilities;
using SkiaSharp;
using System.Xml.Serialization;

namespace RealmStudioShapeRenderingLib
{
    public class PreparedBrush
    {
        [XmlElement]
        public MapBrush? SourceBrush { get; init; }

        [XmlIgnore]
        public SKColor Color { get; set; }

        [XmlElement("Color")]
        public string ColorXml
        {
            get => XmlColorConverter.Serialize(Color);
            set => Color = XmlColorConverter.Deserialize(value);
        }

        [XmlElement]
        public int BrushSize { get; init; }

        [XmlElement]
        public int BrushSpacing { get; init; } = 0;

        [XmlIgnore]
        public List<SKBitmap> Bitmaps { get; init; } = [];
    }
}
