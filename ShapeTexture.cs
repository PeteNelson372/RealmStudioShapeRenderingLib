#nullable enable
using SkiaSharp;
using System.Xml.Serialization;

namespace RealmStudioShapeRenderingLib
{
    public class ShapeTexture
    {
        public ShapeTexture()
        {
            TextureName = string.Empty;
            TexturePath = string.Empty;
        }

        public ShapeTexture(string name, string path)
        {
            TextureName = name;
            TexturePath = path;
        }

        public ShapeTexture(string name, string path, SKBitmap bitmap)
        {
            TextureName = name;
            TexturePath = path;
            TextureBitmap = bitmap;
        }

        [XmlElement]
        public string TextureName { get; set; }

        [XmlElement]
        public string TexturePath { get; set; }

        [XmlIgnore]
        public SKBitmap? TextureBitmap { get; set; }
    }
}
