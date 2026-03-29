using SkiaSharp;
using System.Xml.Serialization;

namespace RealmStudioShapeRenderingLib
{
    public class SymbolBoundsMetadata
    {
        [XmlAttribute("x")]
        public float X { get; set; }

        [XmlAttribute("y")]
        public float Y { get; set; }

        [XmlAttribute("width")]
        public float Width { get; set; }

        [XmlAttribute("height")]
        public float Height { get; set; }

        public SKRect ToSKRect() => new(X, Y, X + Width, Y + Height);
    }
}
