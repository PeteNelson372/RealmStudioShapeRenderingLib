#nullable enable

namespace RealmStudioShapeRenderingLib
{
    public sealed class AssetDescriptor
    {
        // -------------------------------------------------
        // Core identity
        // -------------------------------------------------

        public string Id { get; }
        public string Name { get; }
        public AssetType Type { get; }

        public MapSymbolType SymbolType { get; } 

        // -------------------------------------------------
        // File references
        // -------------------------------------------------

        /// <summary>
        /// Primary file path (image, vector, etc.)
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// Optional metadata file path (XML)
        /// </summary>
        public string? MetadataPath { get; }

        // -------------------------------------------------
        // Classification
        // -------------------------------------------------

        public string? Collection { get; }
        public IReadOnlyList<string> Tags => _tags;

        private readonly List<string> _tags = new();

        // -------------------------------------------------
        // Optional strongly-typed metadata
        // -------------------------------------------------

        /// <summary>
        /// Parsed metadata object (e.g. NinePatchDefinition, SymbolCollectionMetadata, etc.)
        /// </summary>
        public object? Metadata { get; }

        // -------------------------------------------------
        // Construction
        // -------------------------------------------------

        public AssetDescriptor(
            string id,
            string name,
            AssetType type,
            MapSymbolType symbolType,
            string filePath,
            string? metadataPath = null,
            object? metadata = null,
            string? collection = null,
            IEnumerable<string>? tags = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type;
            SymbolType = symbolType;
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            MetadataPath = metadataPath;
            Metadata = metadata;
            Collection = collection;

            if (tags != null)
                _tags.AddRange(tags);
        }

        // -------------------------------------------------
        // Helper Properties
        // -------------------------------------------------

        public string FileName =>
            Path.GetFileName(FilePath);

        public string FileExtension =>
            Path.GetExtension(FilePath).ToLowerInvariant();

        public bool HasMetadata =>
            Metadata != null;

        // -------------------------------------------------
        // Tag Management
        // -------------------------------------------------

        public void AddTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return;

            if (!_tags.Contains(tag))
                _tags.Add(tag);
        }

        public bool HasTag(string tag)
        {
            return _tags.Contains(tag);
        }

        public override string ToString()
        {
            return $"{Name} ({Type})";
        }
    }

}
