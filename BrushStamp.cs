using SkiaSharp;
using System.Xml.Serialization;

namespace RealmStudioShapeRenderingLib
{
    public struct BrushStamp
    {
        [XmlAttribute] public SKPoint Center;
        [XmlAttribute] public float Radius;

        public BrushStamp(SKPoint center, float radius)
        {
            Center = center;
            Radius = radius;
        }
    }
}
