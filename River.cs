using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{

    public class RiverState : IShapeState
    {
        public List<SKPoint> ControlPoints = [];
    }

    public class River : WaterBody
    {
        public List<SKPoint> ControlPoints { get; } = [];

        public float VariationSeed { get; set; }

        public EditablePolylineEditor Editor { get; }

        private List<SKPoint>? _leftBank;
        private List<SKPoint>? _rightBank;

        public River()
        {
            VariationSeed = Random.Shared.NextSingle() * 1000f;
            Editor = new EditablePolylineEditor(ControlPoints)
            {
                OnChanged = () =>
                {
                    RebuildGeometry();
                }
            };
        }

        public void RebuildGeometry()
        {
            if (ControlPoints.Count < 2)
            {
                return;
            }

            var centerline = BezierBuilder.BuildSpline(ControlPoints);
            var geom = RiverGeometryBuilder.BuildRiverPolygon(centerline, RenderSettings.RiverWidth, RenderSettings.RiverSourceFadeIn, VariationSeed);

            SKPath riverPath = geom.Polygon;

            RestoreGeometry(riverPath);

            _leftBank = geom.LeftBank;
            _rightBank = geom.RightBank;
        }

        // -------------------------------------------------
        // Undo / Redo Support
        // -------------------------------------------------

        public override IShapeState CaptureState()
        {
            RiverState state = new()
            {
                ControlPoints = [.. ControlPoints]
            };

            return state;
        }

        public override void RestoreState(IShapeState state)
        {
            var s = (RiverState)state;

            ControlPoints.Clear();

            ControlPoints.AddRange(s.ControlPoints);

            OnGeometryChanged();
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
