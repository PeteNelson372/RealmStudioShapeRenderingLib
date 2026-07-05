using RealmStudioX.WPF.EditorUtilities;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace RealmStudioShapeRenderingLib
{
    public class ColorPalette
    {
        [XmlAttribute]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [XmlAttribute]
        public ColorPaletteType PaletteType { get; set; }

        [XmlAttribute]
        public bool CanAddColors { get; set; } = true;

        [XmlAttribute]
        public string Name { get; set; } = "";

        [XmlArray("ColorEntries")]
        [XmlArrayItem("ColorEntry", Type = typeof(ColorPaletteEntry))]
        public ObservableCollection<ColorPaletteEntry> ColorEntries { get; } = [];

        public void AddColor(SKColor color, string name = "")
        {
            ColorEntries.Add(new ColorPaletteEntry
            {
                DisplayName = name,
                Color = color,
                IsLocked = false
            });
        }

        public bool RemoveColor(string id)
        {
            var entry = ColorEntries.FirstOrDefault(c => c.Id == id);

            if (entry == null || entry.IsLocked)
                return false;

            return ColorEntries.Remove(entry);
        }
    }

    public class ColorPaletteEntry
    {
        [XmlAttribute]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [XmlAttribute]
        public string DisplayName { get; set; } = "";

        [XmlIgnore]
        public SKColor Color { get; set; }

        [XmlAttribute("Color")]
        public string ColorXml
        {
            get => XmlColorConverter.Serialize(Color);
            set => Color = XmlColorConverter.Deserialize(value);
        }
        
        [XmlAttribute]
        public bool IsLocked { get; set; }
    }
}
