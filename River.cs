using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public class River : WaterBody
    {
        public List<SKPoint> ControlPoints { get; } = [];

        public float Width { get; set; } = 20f;

        public bool SourceFade { get; set; } = true;

        public float VariationSeed { get; set; }

        public bool IsInteractive { get; set; } = true;

        private List<SKPoint>? _leftBank;
        private List<SKPoint>? _rightBank;

        public River()
        {
            VariationSeed = Random.Shared.NextSingle() * 1000f;
        }

        public void RebuildGeometry()
        {
            var centerline = BezierBuilder.BuildSpline(ControlPoints);
            var geom = RiverGeometryBuilder.BuildRiverPolygon(centerline, Width, SourceFade, VariationSeed);

            SKPath riverPath = geom.Polygon;

            RestoreGeometry(riverPath);

            _leftBank = geom.LeftBank;
            _rightBank = geom.RightBank;

            InvalidateRenderCache();
        }

        public void BeginInteractive()
        {
            IsInteractive = true;
        }

        public void EndInteractive()
        {
            IsInteractive = false;

            InvalidateRenderCache();
        }

        public override void Render(SKCanvas canvas)
        {
            if (HitPath.IsEmpty) return;

            if (IsInteractive)
            {
                RenderInteractive(canvas);
                return;
            }

            base.Render(canvas);
        }

        private void RenderInteractive(SKCanvas canvas)
        {
            using var paint = new SKPaint()
            {
                Style = SKPaintStyle.Fill,
                Color = RenderSettings.ShallowWaterColor,
                IsAntialias = true,
            };

            canvas.DrawPath(HitPath, paint);
        }

        protected override void RenderShoreline(SKCanvas canvas)
        {
            if (_leftBank == null || _rightBank == null) return;

            using var paint = new SKPaint()
            {
                Style= SKPaintStyle.Stroke,
                StrokeWidth = RenderSettings.ShorelineWidth,
                Color = RenderSettings.ShorelineColor,
                IsAntialias = true,
            };

            using var path = new SKPath();

            path.MoveTo(_leftBank[0]);

            for (int i = 1; i < _leftBank.Count; i++)
            {
                path.LineTo(_leftBank[i]);
            }

            canvas.DrawPath(path, paint);

            path.Reset();

            path.MoveTo(_rightBank[0]);

            for (int i = 0; i < _rightBank.Count; ++i)
            {
                path.MoveTo(_rightBank[i]);
            }

            canvas.DrawPath(path, paint);
        }
    }
}
