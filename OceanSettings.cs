using RealmStudioX.WPF.EditorUtilities;
using SkiaSharp;
using System.Xml.Serialization;

namespace RealmStudioShapeRenderingLib
{
    public class OceanSettings
    {
        [XmlElement]
        public string? TextureId { get; set; }

        [XmlElement]
        public float Scale { get; set; } = 1f;

        [XmlElement]
        public bool Mirror { get; set; }

        [XmlElement]
        public bool EnableCoastlineBlur { get; set; } = true;

        [XmlElement]
        public float TextureOpacity { get; set; } = 1f;

        [XmlElement]
        public bool ColorOverlayEnabled { get; set; }

        [XmlIgnore]
        public SKColor OverlayColor { get; set; } = SKColors.Transparent;

        [XmlElement("OverlayColor")]
        public string OverlayColorXml
        {
            get => XmlColorConverter.Serialize(OverlayColor);
            set => OverlayColor = XmlColorConverter.Deserialize(value);
        }
    }
}
