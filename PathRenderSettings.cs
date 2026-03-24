using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public class PathRenderSettings
    {
        public PathType PathType { get; set; } = PathType.SolidLinePath;
        public SKColor PathColor { get; set; } = SKColor.Parse("#4B311A");
        public float PathWidth { get; set; } = 4f;
        public float PathTowerDistance { get; set; } = 10.0F;
        public float PathTowerSize { get; set; } = 1.2F;
        public SKBitmap? PathTexture { get; set; }
        public int PathTextureOpacity { get; set; } = 255;
        public float PathTextureScale { get; set; } = 1.0F;
        public bool DrawOverSymbols { get; set; }
        public bool ShowPathPoints { get; set; }
        public SKPaint? PathPaint { get; set; }
        public SKPath BoundaryPath { get; set; } = new();
    }
}
