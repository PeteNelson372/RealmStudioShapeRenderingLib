using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public class WaterBody : Shape2D
    {
        public WaterSystem? WaterSystem { get; internal set; } 
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public bool IsInteractive { get; set; } = true;

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

            SetGeometry(new SKPath(newPath));
        }

        public void ClearGeometry()
        {
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
