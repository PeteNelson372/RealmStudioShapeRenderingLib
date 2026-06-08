using SkiaSharp;
using System.Xml.Serialization;

namespace RealmStudioShapeRenderingLib
{
    public abstract class MapComponent2D: IShape2D, ISelectable
    {
        [XmlArray("MapLayerComponents")]
        [XmlArrayItem("Shape2D", Type = typeof(Shape2D))]
        [XmlArrayItem("PaintedShape", Type = typeof(PaintedShape))]
        [XmlArrayItem("Landform", Type = typeof(Landform))]
        [XmlArrayItem("WaterBody", Type = typeof(WaterBody))]
        [XmlArrayItem("WaterSystem", Type = typeof(WaterBody))]
        [XmlArrayItem("River", Type = typeof(River))]
        [XmlArrayItem("Lake", Type = typeof(Lake))]
        [XmlArrayItem("MapPath", Type = typeof(MapPath))]
        [XmlArrayItem("MapSymbol", Type = typeof(MapSymbol))]
        [XmlArrayItem("MapLabel", Type = typeof(MapLabel))]
        [XmlArrayItem("PlacedMapBox", Type = typeof(PlacedMapBox))]
        [XmlArrayItem("PlacedMapFrame", Type = typeof(PlacedMapFrame))]
        [XmlArrayItem("MapGrid", Type = typeof(MapGrid))]
        [XmlArrayItem("Windrose", Type = typeof(MapWindrose))]
        [XmlArrayItem("MapScale", Type = typeof(MapScale))]
        [XmlArrayItem("MapRegion", Type = typeof(MapRegion))]
        [XmlArrayItem("MapVignette", Type = typeof(MapVignette))]
        //[XmlArrayItem("MapHeightMap", Type = typeof(MapHeightMap))]
        [XmlArrayItem("DrawnArrow", Type = typeof(DrawnArrow))]
        [XmlArrayItem("DrawingErase", Type = typeof(DrawingErase))]
        [XmlArrayItem("DrawnDiamond", Type = typeof(DrawnDiamond))]
        [XmlArrayItem("DrawnEllipse", Type = typeof(DrawnEllipse))]
        [XmlArrayItem("DrawnFivePointStar", Type = typeof(DrawnFivePointStar))]
        [XmlArrayItem("DrawnLine", Type = typeof(DrawnLine))]
        [XmlArrayItem("DrawnPolygon", Type = typeof(DrawnPolygon))]
        [XmlArrayItem("DrawnRectangle", Type = typeof(DrawnRectangle))]
        [XmlArrayItem("DrawnRegularPolygon", Type = typeof(DrawnRegularPolygon))]
        [XmlArrayItem("DrawnSixPointStar", Type = typeof(DrawnSixPointStar))]
        [XmlArrayItem("DrawnStamp", Type = typeof(DrawnStamp))]
        [XmlArrayItem("DrawnTriangle", Type = typeof(DrawnTriangle))]
        [XmlArrayItem("PaintedLine", Type = typeof(PaintedLine))]
        [XmlArrayItem("DrawnPixelEdits", Type = typeof(DrawnPixelEdits))]

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
    }
}
