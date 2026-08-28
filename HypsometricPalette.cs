using RealmStudioX.WPF.EditorUtilities;
using SkiaSharp;
using System.Xml.Serialization;

namespace RealmStudioShapeRenderingLib
{
    public class HypsometricPalette
    {
        [XmlAttribute]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [XmlAttribute]
        public string Name { get; set; } = string.Empty;

        [XmlAttribute]
        public bool IsLocked { get; set; }

        [XmlArray("Tints")]
        [XmlArrayItem("Tint", Type = typeof(HypsometricTint))]
        public List<HypsometricTint> Tints = [];

        public void SortTints()
        {
            Tints.Sort((a, b) =>
                a.NormalizedHeight.CompareTo(b.NormalizedHeight));
        }
    }

    public class HypsometricTint
    {
        [XmlAttribute]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // range is -1.0 to 1.0 inclusive
        [XmlAttribute]
        public float NormalizedHeight { get; set; }

        [XmlIgnore]
        public SKColor Color { get; set; }

        [XmlAttribute("Color")]
        public string ColorXml
        {
            get => XmlColorConverter.Serialize(Color);
            set => Color = XmlColorConverter.Deserialize(value);
        }
    }
}
