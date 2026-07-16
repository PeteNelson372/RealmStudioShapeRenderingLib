using SkiaSharp;
using System.Xml.Serialization;

namespace RealmStudioShapeRenderingLib
{

    public class RiverState : IShapeState
    {
        public List<SKPoint> ControlPoints = [];
    }

    public class River : WaterBody
    {
        [XmlIgnore]
        public List<SKPoint> ControlPoints { get; } = [];

        [XmlElement("ControlPoints")]
        public string PointsList
        {
            get => string.Join(";", ControlPoints.Select(p => $"{p.X},{p.Y}"));

            set
            {
                ControlPoints.Clear();

                if (string.IsNullOrWhiteSpace(value))
                {
                    return;
                }

                foreach (string pair in value.Split(';'))
                {
                    string[] parts = pair.Split(',');

                    ControlPoints.Add(new SKPoint(float.Parse(parts[0]), float.Parse(parts[1])));
                }
            }
        }

        [XmlElement]
        public float VariationSeed { get; set; }

        [XmlIgnore]
        public EditablePolylineEditor Editor { get; }

        private List<SKPoint>? _leftBank;
        public List<SKPoint>? LeftBank => _leftBank;

        private List<SKPoint>? _rightBank;
        public List<SKPoint>? RightBank => _rightBank;

        public River()
        {
            Editor = new EditablePolylineEditor(ControlPoints)
            {
                OnChanged = () =>
                {
                    RebuildGeometry();
                    WaterSystem?.GeometryModified();
                    WaterSystem?.InvalidateRenderCache();
                }
            };
        }

        public override void FinalizeShapeGeometry(RealmStudioMap map)
        {
            SetGeometry(Utilities.BuildPath(ControlPoints));

            RebuildGeometry();
            WaterSystem?.GeometryModified();
            WaterSystem?.InvalidateRenderCache();
        }

        public void RebuildGeometry()
        {
            if (ControlPoints.Count < 2)
            {
                return;
            }

            var centerline = BezierBuilder.BuildSpline(ControlPoints);
            var geom = RiverGeometryBuilder.BuildRiverPolygon(centerline,
                RenderSettings.RiverWidth,
                RenderSettings.RiverSourceFadeIn,
                VariationSeed,
                RenderSettings.MeanderStrength);

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

            RebuildGeometry();

            WaterSystem!.GeometryModified();
            WaterSystem.InvalidateRenderCache();

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

            using var leftPathBuilder = new SKPathBuilder();

            leftPathBuilder.MoveTo(_leftBank[0]);

            for (int i = 1; i < _leftBank.Count; i++)
            {
                leftPathBuilder.LineTo(_leftBank[i]);
            }

            using var leftpath = leftPathBuilder.Snapshot();
            leftPathBuilder.Detach();

            canvas.DrawPath(leftpath, paint);

            using var rightPathBuilder = new SKPathBuilder();

            rightPathBuilder.MoveTo(_rightBank[0]);

            for (int i = 1; i < _rightBank.Count; ++i)
            {
                rightPathBuilder.LineTo(_rightBank[i]);
            }

            using var rightpath = rightPathBuilder.Snapshot();
            rightPathBuilder.Detach();

            canvas.DrawPath(rightpath, paint);
        }
    }

}
