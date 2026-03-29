namespace RealmStudioShapeRenderingLib
{
    using SkiaSharp;
    using System.Xml.Serialization;

    public class MapSymbolDefinition
    {
        // -------------------------------------------------
        // Identity
        // -------------------------------------------------

        [XmlAttribute("id")]
        public string Id { get; set; } = string.Empty;

        [XmlAttribute("symbolName")]
        public string SymbolName { get; set; } = string.Empty;


        // -------------------------------------------------
        // Classification
        // -------------------------------------------------

        [XmlAttribute("type")]
        public MapSymbolType SymbolType { get; set; } = MapSymbolType.NotSet;

        [XmlAttribute("baseColor")]
        public MapSymbolBaseColorType BaseColorType { get; set; } = MapSymbolBaseColorType.NotSet;

        [XmlAttribute("format")]
        public SymbolFileFormat SymbolFormat { get; set; } = SymbolFileFormat.NotSet;

        // -------------------------------------------------
        // File
        // -------------------------------------------------

        [XmlElement("File")]
        public string SymbolFilePath { get; set; } = string.Empty;

        // -------------------------------------------------
        // Tags
        // -------------------------------------------------

        [XmlArray("Tags")]
        [XmlArrayItem("Tag")]
        public List<string> SymbolTags { get; set; } = [];

        // -------------------------------------------------
        // Bounds (XML surrogate)
        // -------------------------------------------------

        [XmlElement("Bounds")]
        public SymbolBoundsMetadata? BoundsMetadata { get; set; }

        // -------------------------------------------------
        // Runtime-only fields
        // -------------------------------------------------

        [XmlIgnore]
        public SKRect Bounds { get; set; } = SKRect.Empty;

        [XmlIgnore]
        public string CollectionId { get; set; } = string.Empty;

        [XmlIgnore]
        public string CollectionName { get; set; } = string.Empty;

        [XmlIgnore]
        public string CollectionPath { get; set; } = string.Empty;

        // -------------------------------------------------
        // Post-load fixup
        // -------------------------------------------------

        public void FinalizeAfterLoad(string collectionId, string collectionName, string baseDir)
        {
            CollectionId = collectionId;
            CollectionName = collectionName;
            CollectionPath = baseDir;

            if (BoundsMetadata != null)
            {
                Bounds = BoundsMetadata.ToSKRect();
            }
        }
    }

}
