using RealmStudioX.Infrastructure;
using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public sealed class RealmStudioProject
    {
        public const int ProjectFormatVersion = 1;

        public MapProjectMetadata Metadata { get; set; } = new();
        
        public List<MapProjectEntry> Maps { get; set; } = [];

        public string ActiveMapId { get; set; } = string.Empty;
    }

    public sealed class MapProjectEntry
    {
        public string MapId { get; set; } = string.Empty;

        public RealmStudioMap Map { get; set; } = new();

        public MapMetadata Metadata { get; set; } = new();

        public SKBitmap? Preview { get; set; }
    }
}
