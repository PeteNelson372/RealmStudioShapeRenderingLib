using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public class River : WaterBody
    {
        public List<SKPoint> ControlPoints { get; } = [];

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
            var geom = RiverGeometryBuilder.BuildRiverPolygon(centerline, RenderSettings.RiverWidth, SourceFade, VariationSeed);

            SKPath riverPath = geom.Polygon;

            RestoreGeometry(riverPath);

            _leftBank = geom.LeftBank;
            _rightBank = geom.RightBank;
        }

        public void BeginInteractive()
        {
            IsInteractive = true;
        }

        public void EndInteractive()
        {
            IsInteractive = false;
        }

        public void RenderInteractive(SKCanvas canvas)
        {
            using var paint = new SKPaint()
            {
                Style = SKPaintStyle.Fill,
                Color = RenderSettings.ShallowWaterColor,
                IsAntialias = true,
            };

            canvas.DrawPath(HitPath, paint);

            RenderShoreline(canvas);
        }

        protected override void RenderShoreline(SKCanvas canvas)
        {
            if (_leftBank == null || _rightBank == null || _leftBank.Count <= 0 || _rightBank.Count <= 0) return;

            using var paint = new SKPaint()
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = RenderSettings.ShorelineWidth,
                Color = RenderSettings.ShorelineColor,
                BlendMode = SKBlendMode.SrcOver,
                IsAntialias = true,
            };

            using var leftpath = new SKPath();

            leftpath.MoveTo(_leftBank[0]);

            for (int i = 1; i < _leftBank.Count; i++)
            {
                leftpath.LineTo(_leftBank[i]);
            }

            canvas.DrawPath(leftpath, paint);

            using var rightpath = new SKPath();

            rightpath.MoveTo(_rightBank[0]);

            for (int i = 1; i < _rightBank.Count; ++i)
            {
                rightpath.LineTo(_rightBank[i]);
            }

            canvas.DrawPath(rightpath, paint);
        }
    }
}
