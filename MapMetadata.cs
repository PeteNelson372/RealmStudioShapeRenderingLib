using RealmStudioShapeRenderingLib;

namespace RealmStudioX.Infrastructure
{
    public sealed class MapMetadata
    {
        public const int MapMetadataFormatVersion = 1;

        public string MapId { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public RealmMapType RealmType { get; set; }

        public int MapWidth { get; set; }

        public int MapHeight { get; set; }

        public float AreaWidth { get; set; }

        public float AreaHeight { get; set; }

        public string AreaUnits { get; set; } = string.Empty;

        public string Theme { get; set; } = string.Empty;

        public DateTime Created { get; set; }

        public DateTime Modified { get; set; }

        public string PreviewFile { get; set; } = string.Empty;
    }
}

