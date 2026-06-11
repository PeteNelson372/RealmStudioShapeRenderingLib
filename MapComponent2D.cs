using SkiaSharp;
using System.Xml.Serialization;

namespace RealmStudioShapeRenderingLib
{
    [XmlInclude(typeof(Shape2D))]
    [XmlInclude(typeof(PaintedShape))]
    [XmlInclude(typeof(Landform))]
    [XmlInclude(typeof(WaterBody))]
    [XmlInclude(typeof(WaterSystem))]
    [XmlInclude(typeof(River))]
    [XmlInclude(typeof(Lake))]
    [XmlInclude(typeof(PaintedWaterBody))]
    [XmlInclude(typeof(MapPath))]
    [XmlInclude(typeof(MapSymbol))]
    [XmlInclude(typeof(MapLabel))]
    [XmlInclude(typeof(PlacedMapBox))]
    [XmlInclude(typeof(PlacedMapFrame))]
    [XmlInclude(typeof(MapGrid))]
    [XmlInclude(typeof(MapWindrose))]
    [XmlInclude(typeof(MapScale))]
    [XmlInclude(typeof(MapRegion))]
    [XmlInclude(typeof(MapVignette))]
    //[XmlInclude(typeof(MapHeightMap))]
    [XmlInclude(typeof(DrawnArrow))]
    [XmlInclude(typeof(DrawingErase))]
    [XmlInclude(typeof(DrawnDiamond))]
    [XmlInclude(typeof(DrawnEllipse))]
    [XmlInclude(typeof(DrawnFivePointStar))]
    [XmlInclude(typeof(DrawnLine))]
    [XmlInclude(typeof(DrawnPolygon))]
    [XmlInclude(typeof(DrawnRectangle))]
    [XmlInclude(typeof(DrawnRegularPolygon))]
    [XmlInclude(typeof(DrawnSixPointStar))]
    [XmlInclude(typeof(DrawnStamp))]
    [XmlInclude(typeof(DrawnTriangle))]
    [XmlInclude(typeof(PaintedLine))]
    [XmlInclude(typeof(DrawnPixelEdits))]
    public abstract class MapComponent2D: IShape2D, ISelectable
    {
        [XmlElement]
        public string Id { get; } = Guid.NewGuid().ToString();

        [XmlIgnore]
        public bool IsSelected { get; set; }

        [XmlElement]
        public virtual SKRect LocalBounds { get; set; }

        [XmlElement]
        public virtual SKRect Bounds { get; set; } // world bounds

        public abstract void Render(SKCanvas canvas, FontManager? fontManager = null);

        public abstract bool HitTest(SKPoint worldPos);

        public abstract IShapeState CaptureState();

        public abstract void RestoreState(IShapeState state);

        public virtual void FinalizeShapeGeometry(RealmStudioMap map) {}
    }
}
