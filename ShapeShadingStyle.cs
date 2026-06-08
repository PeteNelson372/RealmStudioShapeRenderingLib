#nullable enable

using SkiaSharp;
using System.Xml.Serialization;

namespace RealmStudioShapeRenderingLib
{
    public class ShapeShadingStyle
    {
        [XmlElement]
        public SKColor? Color;

        [XmlElement]
        public SKShader? Texture;

        [XmlElement]
        public float Width;
    }
}
