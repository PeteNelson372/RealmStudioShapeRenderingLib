using SkiaSharp;
using System.Xml.Serialization;

namespace RealmStudioShapeRenderingLib
{
    public class PaintedWaterBody : WaterBody
    {
        [XmlArray]
        [XmlArrayItem("Point", Type = typeof(SKPoint))]
        public List<SKPoint> ControlPoints { get; } = [];

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
        private readonly SKPath _strokePath = new();

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
            _strokePath.Reset();
        }



        // -------------------------------------------------
        // Geometry construction
        // -------------------------------------------------

        private void Stamp(SKPoint point)
        {
            _strokePath.AddCircle(point.X, point.Y, BrushRadius);
        }

        /// <summary>
        /// Commits the current stroke path into the shape geometry.
        /// Uses union to accumulate painted area.
        /// </summary>
        private void CommitStroke()
        {
            if (_strokePath.IsEmpty)
                return;

            SKPath result = _cachedPath.IsEmpty
                ? new SKPath(_strokePath)
                : _cachedPath.Op(_strokePath, SKPathOp.Union);

            _strokePath.Reset();
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
