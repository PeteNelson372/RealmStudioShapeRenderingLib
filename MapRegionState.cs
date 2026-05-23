using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public class MapRegionState : IShapeState
    {
        public string RegionName { get; set; } = string.Empty;
        public string RegionDescription { get; set; } = string.Empty;

        public SKColor RegionBorderColor { get; set; } = SKColor.Parse("#0056B3");
        public int RegionBorderWidth { get; set; } = 10;
        public int RegionInnerOpacity { get; set; } = 64;
        public int RegionBorderSmoothing { get; set; } = 20;
        public PathType RegionBorderType { get; set; } = PathType.SolidLinePath;

        public List<MapRegionPoint> MapRegionPoints = [];
    }
}
