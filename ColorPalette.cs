using RealmStudioX.WPF.EditorUtilities;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Drawing;
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

        public void AddColor(SKColor color)
        {
            ColorEntries.Add(new ColorPaletteEntry
            {
                DisplayName = GetColorName(color),
                Color = color,
                IsLocked = false
            });
        }

        public bool RemoveColor(SKColor color)
        {
            var entry = ColorEntries.FirstOrDefault(c => c.Color == color);

            if (entry == null || entry.IsLocked)
                return false;

            return ColorEntries.Remove(entry);
        }

        public bool ContainsColor(SKColor color)
        {
            return ColorEntries.Any(c => c.Color == color);
        }

        public static string GetColorName(SKColor color)
        {
            string c = color.ToString().ToUpper();

            string colorName = ColorTranslator.FromHtml(c).Name.ToUpper();

            if (colorName.StartsWith("FF"))
            {
                return "#" + colorName;
            }

            return colorName;
        }
    }

    public class ColorPaletteEntry
    {
        [XmlAttribute]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [XmlIgnore]
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
