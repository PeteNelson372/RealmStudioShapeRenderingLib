using SkiaSharp;
using System.Xml.Serialization;

namespace RealmStudioShapeRenderingLib
{
    public struct BrushStamp(SKPoint center, float radius)
    {
        [XmlAttribute] public SKPoint Center = center;
        [XmlAttribute] public float Radius = radius;
    }
}
