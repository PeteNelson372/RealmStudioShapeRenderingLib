#nullable enable

using System.Xml.Serialization;

namespace RealmStudioShapeRenderingLib
{
    /// <summary>
    /// Serializable map document describing a fantasy map.
    /// This class contains NO editor or runtime state.
    /// </summary>
    [XmlRoot("RealmStudioMap", Namespace = "RealmStudio", IsNullable = false)]
    [XmlInclude(typeof(MapLayer))]
    public sealed class RealmStudioMap
    {
        // -------------------------------------------------
        // Identity
        // -------------------------------------------------

        [XmlAttribute("guid")]
        public Guid MapGuid { get; set; } = Guid.NewGuid();

        [XmlAttribute("name")]
        public string MapName { get; set; } = string.Empty;

        [XmlAttribute("path")]
        public string MapPath { get; set; } = string.Empty;

        // -------------------------------------------------
        // Pixel dimensions (authoritative)
        // -------------------------------------------------

        /// <summary>
        /// Width of the map in pixels.
        /// </summary>
        [XmlAttribute("pixelWidth")]
        public int MapWidth { get; set; }

        /// <summary>
        /// Height of the map in pixels.
        /// </summary>
        [XmlAttribute("pixelHeight")]
        public int MapHeight { get; set; }

        // -------------------------------------------------
        // World dimensions (authoritative)
        // -------------------------------------------------

        /// <summary>
        /// Width of the map in world units (e.g. miles).
        /// </summary>
        [XmlAttribute("areaWidth")]
        public float MapAreaWidth { get; set; }

        /// <summary>
        /// Height of the map in world units.
        /// </summary>
        [XmlAttribute("areaHeight")]
        public float MapAreaHeight { get; set; }

        [XmlAttribute("areaUnits")]
        public string MapAreaUnits { get; set; } = string.Empty;

        // -------------------------------------------------
        // Metadata
        // -------------------------------------------------

        [XmlAttribute("realmType")]
        public RealmMapType RealmType { get; set; } = RealmMapType.World;

        [XmlAttribute("description")]
        public string RealmDescription { get; set; } = string.Empty;

        [XmlAttribute("theme")]
        public string MapTheme { get; set; } = string.Empty;

        // -------------------------------------------------
        // External integrations
        // -------------------------------------------------

        [XmlElement("WorldAnvil")]
        public WorldAnvilIntegrationParameters WorldAnvilIntegrationParams { get; set; } = new();

        // -------------------------------------------------
        // Layers
        // -------------------------------------------------

        [XmlArray("MapLayers")]
        [XmlArrayItem("Layer")]
        public List<MapLayer> MapLayers { get; set; } = new();

        // -------------------------------------------------
        // Saved state
        // -------------------------------------------------

        [XmlIgnore]
        public bool IsSaved { get; private set; }

        public void MarkChanged()
        {
            IsSaved = false;
        }

        public void MarkSaved()
        {
            IsSaved = true;
        }

        // -------------------------------------------------
        // Derived values (NOT serialized)
        // -------------------------------------------------

        /// <summary>
        /// World units per pixel in X.
        /// </summary>
        [XmlIgnore]
        public float UnitsPerPixelX =>
            MapWidth > 0 ? MapAreaWidth / MapWidth : 0f;

        /// <summary>
        /// World units per pixel in Y.
        /// </summary>
        [XmlIgnore]
        public float UnitsPerPixelY =>
            MapHeight > 0 ? MapAreaHeight / MapHeight : 0f;

        // -------------------------------------------------
        // Validation
        // -------------------------------------------------

        /// <summary>
        /// Validates map invariants.
        /// Call after loading or before rendering/export.
        /// </summary>
        public void Validate()
        {
            if (MapWidth <= 0 || MapHeight <= 0)
                throw new InvalidOperationException(
                    "Map pixel dimensions must be greater than zero.");

            if (MapAreaWidth <= 0 || MapAreaHeight <= 0)
                throw new InvalidOperationException(
                    "Map area dimensions must be greater than zero.");

            if (MapLayers == null)
                throw new InvalidOperationException(
                    "MapLayers collection must not be null.");
        }
    }
}
