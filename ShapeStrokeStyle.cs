#nullable enable

using RealmStudioX.WPF.EditorUtilities;
using SkiaSharp;
using System.Xml.Serialization;
namespace RealmStudioShapeRenderingLib
{
    public class ShapeStrokeStyle
    {
        [XmlElement]
        public ColorTextureMode StrokeStyle { get; set; } = ColorTextureMode.Color;

        [XmlIgnore]
        public SKColor StrokeColor { get; set; } = SKColors.Transparent;

        [XmlElement("StrokeColor")]
        public string StrokeColorXml
        {
            get => XmlColorConverter.Serialize(StrokeColor);
            set => StrokeColor = XmlColorConverter.Deserialize(value);
        }

        [XmlElement]
        public float StrokeWidth { get; set; } = 1.0f;

        [XmlElement]
        public ShapeTexture? StrokeTexture { get; set; } = null;
    }
}
