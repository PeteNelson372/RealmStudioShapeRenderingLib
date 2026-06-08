using System.Xml.Serialization;

namespace RealmStudioShapeRenderingLib
{
    public class OceanShorelineSettings
    {
        [XmlElement]
        public float ShoreDepth { get; set; } = 120f; // pixels outward

        [XmlElement]
        public byte MaxAlpha { get; set; } = 120;

        [XmlElement]
        public bool EnableFoam { get; set; } = true;

        [XmlElement]
        public byte FoamAlpha { get; set; } = 160;
    }
}
