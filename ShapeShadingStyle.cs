#nullable enable

using RealmStudioX.WPF.EditorUtilities;
using SkiaSharp;
using System.Xml.Serialization;

namespace RealmStudioShapeRenderingLib
{
    public class ShapeShadingStyle
    {
        [XmlIgnore]
        public SKColor? Color;

        [XmlElement("Color")]
        public string ColorXml
        {
            get => Color != null ? XmlColorConverter.Serialize((SKColor)Color) : string.Empty;
            set => Color = XmlColorConverter.Deserialize(value);
        }

        [XmlElement]
        public SKShader? Texture;

        [XmlElement]
        public float Width;
    }
}
