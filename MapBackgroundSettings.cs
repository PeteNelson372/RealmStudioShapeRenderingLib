using System.Xml.Serialization;

namespace RealmStudioShapeRenderingLib
{
    public class MapBackgroundSettings
    {
        [XmlElement]
        public string? TextureId { get; set; }

        [XmlElement]
        public float Scale { get; set; } = 1f;

        [XmlElement]
        public bool Mirror { get; set; }

        [XmlIgnore]
        public bool Enabled { get; set; }
    }
}
