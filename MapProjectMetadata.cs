namespace RealmStudioShapeRenderingLib
{
    public sealed class MapProjectMetadata
    {
        public const int ProjectMetadataFormatVersion = 1;

        public string ProjectId { get; set; } = Guid.NewGuid().ToString();

        public string ProjectName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public RealmProjectType RealmType { get; set; }

        public string ProjectFilePath { get; set; } = string.Empty;

        public DateTime Created { get; set; }

        public DateTime Modified { get; set; }
    }
}
