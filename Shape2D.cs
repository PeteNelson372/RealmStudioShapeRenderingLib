#nullable enable

namespace RealmStudioShapeRenderingLib
{
    using SkiaSharp;
    using System.Xml.Serialization;

    public abstract class Shape2D : MapComponent2D
    {
        public event Action? GeometryChanged;

        protected virtual void OnGeometryChanged()
        {
            GeometryChanged?.Invoke();
        }

        // -------------------------------------------------
        // Geometry ownership
        // -------------------------------------------------

        protected SKPath _cachedPath = new()
        {
            FillType = SKPathFillType.EvenOdd
        };

        // Path used for hit testing, clipping, and fills.        
        [XmlElement]
        public SKPath HitPath { get; protected set; } = new()
        {
            FillType = SKPathFillType.EvenOdd,
        };


        // Path representing the outline/perimeter of the shape.
        [XmlElement]
        public SKPath PerimeterPath { get; protected set; } = new()
        {
            FillType = SKPathFillType.EvenOdd,
        };

        // Axis-aligned bounds in world space.
        [XmlElement]
        public override SKRect Bounds => HitPath.Bounds;

        // -------------------------------------------------
        // Geometry
        // -------------------------------------------------

        protected virtual void SetGeometry(SKPath newGeometry)
        {
            _cachedPath.Dispose();
            _cachedPath = new(newGeometry)
            {
                FillType= SKPathFillType.EvenOdd,
            };

            HitPath = new SKPath(_cachedPath)
            {
                FillType = SKPathFillType.EvenOdd,
            };

            RebuildPerimeter();

            OnGeometryChanged();
        }

        public virtual void RestoreGeometry(SKPath path)
        {
            ArgumentNullException.ThrowIfNull(path);

            SetGeometry(new SKPath(path));
        }

        public virtual SKPath CloneGeometry()
        {
            return new SKPath(HitPath);
        }

        public virtual void Translate(float dx, float dy)
        {
            var matrix = SKMatrix.CreateTranslation(dx, dy);

            HitPath.Transform(matrix);
            RebuildPerimeter();

            OnGeometryChanged();
        }

        /// <summary>
        /// Rebuilds the perimeter path when geometry changes.
        /// Subclasses may override.
        /// </summary>
        protected virtual void RebuildPerimeter()
        {
            // Default perimeter = same as filled geometry
            PerimeterPath = new SKPath(HitPath)
            {
                FillType = SKPathFillType.EvenOdd
            };
        }

        // -------------------------------------------------
        // Undo / Redo Support
        // -------------------------------------------------

        public override IShapeState CaptureState()
        {
            return new Shape2DState(new SKPath(HitPath));
        }

        public override void RestoreState(IShapeState state)
        {
            RestoreGeometry(((Shape2DState)state).Geometry);
            OnGeometryChanged();
        }


        // -------------------------------------------------
        // Rendering
        // -------------------------------------------------

        /// <summary>
        /// Default rendering behavior. Subclasses must override.
        /// </summary>
        public override void Render(SKCanvas canvas, FontManager? _)
        {
            throw new ApplicationException("Shape2D.Render called. This method must be overridden.");
        }

        // -------------------------------------------------
        // HitTest
        // -------------------------------------------------
        public override bool HitTest(SKPoint worldPos)
        {
            return Bounds.Contains(worldPos) &&
                   HitPath.Contains(worldPos.X, worldPos.Y);
        }
    }

    public class Shape2DState(SKPath geometry) : IShapeState
    {
        public SKPath Geometry { get; } = geometry;        
    }

}
