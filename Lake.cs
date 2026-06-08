using System.Xml.Serialization;

namespace RealmStudioShapeRenderingLib
{
    public class Lake : WaterBody
    {
        [XmlElement]
        public float NoiseScale { get; set; }

        [XmlElement]
        public float NoiseStrength { get; set; }
    }
}
