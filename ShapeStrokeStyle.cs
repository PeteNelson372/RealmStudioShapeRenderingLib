#nullable enable

using SkiaSharp;
using System.Xml.Serialization;
namespace RealmStudioShapeRenderingLib
{
    public class ShapeStrokeStyle
    {
        [XmlElement]
        public ColorTextureMode StrokeStyle { get; set; } = ColorTextureMode.Color;

        [XmlElement]
        public SKColor StrokeColor { get; set; } = SKColors.Transparent;

        [XmlElement]
        public float StrokeWidth { get; set; } = 1.0f;

        [XmlElement]
        public ShapeTexture? StrokeTexture { get; set; } = null;
    }
}
