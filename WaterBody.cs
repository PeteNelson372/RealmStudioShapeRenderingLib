using SkiaSharp;
using System.Xml.Serialization;

namespace RealmStudioShapeRenderingLib
{
    [XmlInclude(typeof(River))]
    [XmlInclude(typeof(Lake))]
    [XmlInclude(typeof(PaintedWaterBody))]
    public class WaterBody : Shape2D
    {
        [XmlIgnore]
        public WaterSystem? WaterSystem { get; set; }

        [XmlElement]
        public string Name { get; set; } = string.Empty;

        [XmlElement]
        public string Description { get; set; } = string.Empty;

        [XmlIgnore]
        public bool IsInteractive { get; set; } = true;

        [XmlElement]
        public WaterRenderSettings RenderSettings { get; set; } = new();

        public void BeginInteractive()
        {
            IsInteractive = true;
        }

        public void EndInteractive()
        {
            IsInteractive = false;
        }

        public void ReplaceGeometry(SKPath newPath)
        {
            if (newPath == null || newPath.IsEmpty)
            {
                ClearGeometry();
                return;
            }

            WaterSystem?.GeometryModified();
            SetGeometry(new SKPath(newPath));
        }

        public void ClearGeometry()
        {
            WaterSystem?.GeometryModified();
            SetGeometry(new SKPath());
        }

        protected virtual void RenderShoreline(SKCanvas canvas)
        {
            using var paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = RenderSettings.ShorelineColor,
                StrokeWidth = RenderSettings.ShorelineWidth * 2,
                StrokeJoin = SKStrokeJoin.Round,
                StrokeCap = SKStrokeCap.Round,
                IsAntialias = true
            };

            canvas.DrawPath(PerimeterPath, paint);
        }
    }
}
