using SkiaSharp;
using System.Xml.Serialization;

namespace RealmStudioShapeRenderingLib
{
    public class PathRenderSettings
    {
        [XmlElement]
        public PathType PathType { get; set; } = PathType.SolidLinePath;

        [XmlElement]
        public SKColor PathColor { get; set; } = SKColor.Parse("#4B311A");

        [XmlElement]
        public float PathWidth { get; set; } = 4f;

        [XmlElement]
        public float PathTowerDistance { get; set; } = 10.0F;

        [XmlElement]
        public float PathTowerSize { get; set; } = 1.2F;

        [XmlElement]
        public SKBitmap? PathTexture { get; set; }

        [XmlElement]
        public int PathTextureOpacity { get; set; } = 255;

        [XmlElement]
        public float PathTextureScale { get; set; } = 1.0F;

        [XmlElement]
        public bool DrawOverSymbols { get; set; }

        [XmlElement]
        public bool ShowPathPoints { get; set; }

        [XmlIgnore]
        public SKPaint? PathPaint { get; set; }

        [XmlElement]
        public SKPath BoundaryPath { get; set; } = new();
    }
}
