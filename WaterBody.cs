using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public class WaterBody : Shape2D
    {
        public WaterSystem? WaterSystem { get; internal set; } 
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;


        public WaterRenderSettings RenderSettings { get; set; } = new();

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
