using SkiaSharp;
using System.Xml.Serialization;

namespace RealmStudioShapeRenderingLib
{
    public class PaintedWaterBody : WaterBody
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

        // -------------------------------------------------
        // Brush configuration
        // -------------------------------------------------

        [XmlElement]
        public float BrushRadius { get; set; } = 12f;

        // Minimum spacing between brush stamps as a fraction of radius.
        [XmlElement]
        public float BrushSpacing { get; set; } = 0.5f;

        // -------------------------------------------------
        // Stroke state (transient)
        // -------------------------------------------------

        private SKPoint? _lastPoint;
        private readonly SKPathBuilder _strokePathBuilder = new();

        // -------------------------------------------------
        // Painting API (called by tools / commands)
        // -------------------------------------------------

        public void BeginStroke(SKPoint point)
        {
            _lastPoint = point;
            Stamp(point);
            CommitStroke();
        }

        public void AddStrokePoint(SKPoint point)
        {
            if (_lastPoint == null)
                return;

            float minDistance = BrushRadius * BrushSpacing;
            if (Utilities.Distance(_lastPoint.Value, point) < minDistance)
                return;

            _lastPoint = point;
            Stamp(point);
            CommitStroke();
        }

        public void EndStroke()
        {
            _lastPoint = null;
            _strokePathBuilder.Reset();
        }



        // -------------------------------------------------
        // Geometry construction
        // -------------------------------------------------

        private void Stamp(SKPoint point)
        {
            _strokePathBuilder.AddCircle(point.X, point.Y, BrushRadius);
        }

        /// <summary>
        /// Commits the current stroke path into the shape geometry.
        /// Uses union to accumulate painted area.
        /// </summary>
        private void CommitStroke()
        {
            var _strokePath = _strokePathBuilder.Snapshot();
            _strokePathBuilder.Detach();

            if (_strokePath.IsEmpty)
                return;

            SKPath result = HitPath.IsEmpty
                ? new SKPath(_strokePath)
                : HitPath.Op(_strokePath, SKPathOp.Union);

            SetGeometry(result);
        }

        public override void RestoreGeometry(SKPath geometry)
        {
            // Defensive copy so caller can't mutate internal state
            SetGeometry(new SKPath(geometry));
        }

        // -------------------------------------------------
        // Perimeter handling
        // -------------------------------------------------

        protected override void RebuildPerimeter()
        {
            // For painted shapes, the perimeter is the outline of the filled region
            PerimeterPath = new SKPath(HitPath);
        }

        // -------------------------------------------------
        // Rendering
        // -------------------------------------------------

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
    }
}
