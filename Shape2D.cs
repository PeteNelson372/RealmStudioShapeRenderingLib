#nullable enable

namespace RealmStudioShapeRenderingLib
{
    using SkiaSharp;

    public abstract class Shape2D : IShape2D
    {
        public string Id => Guid.NewGuid().ToString();

        public event Action? GeometryChanged;

        protected virtual void OnGeometryChanged()
        {
            GeometryChanged?.Invoke();
        }

        // -------------------------------------------------
        // Geometry ownership
        // -------------------------------------------------

        protected SKPath _cachedPath = new();

        /// <summary>
        /// Path used for hit testing, clipping, and fills.
        /// </summary>
        public SKPath HitPath { get; protected set; } = new();

        /// <summary>
        /// Path representing the outline/perimeter of the shape.
        /// </summary>
        public SKPath PerimeterPath { get; protected set; } = new();

        /// <summary>
        /// Axis-aligned bounds in world space.
        /// </summary>
        public virtual SKRect Bounds => HitPath.Bounds;

        // -------------------------------------------------
        // Geometry lifecycle
        // -------------------------------------------------

        protected virtual void SetGeometry(SKPath newGeometry)
        {
            _cachedPath.Dispose();
            _cachedPath = newGeometry;

            HitPath = new SKPath(_cachedPath);

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
            PerimeterPath = new SKPath(HitPath);
        }

        // -------------------------------------------------
        // Rendering
        // -------------------------------------------------

        /// <summary>
        /// Default rendering behavior. Subclasses may override.
        /// </summary>
        public virtual void Render(SKCanvas canvas)
        {
            if (HitPath.IsEmpty)
                return;

            using var paint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = SKColors.Gray,
                IsAntialias = true
            };

            canvas.DrawPath(HitPath, paint);
        }
    }

}
