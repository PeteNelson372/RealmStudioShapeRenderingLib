using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public sealed class DrawnLine : MapComponent2D, IDrawnMapComponent
    {
        private List<SKPoint> _points = [];
        private SKColor _color = SKColors.Black;
        private int _brushSize = 2;

        public List<SKPoint> Points
        {
            get => _points;
            set
            {
                _points = value ?? throw new ArgumentNullException(nameof(value), "Points cannot be null.");
            }
        }

        public SKColor Color
        {
            get => _color;
            set
            {
                _color = value;
            }
        }

        public int BrushSize
        {
            get => _brushSize;
            set
            {
                _brushSize = value;
            }
        }

        public override bool HitTest(SKPoint worldPos)
        {
            return Bounds.Contains(worldPos);
        }

        public override IShapeState CaptureState()
        {
            throw new NotImplementedException();
        }

        public override void RestoreState(IShapeState state)
        {
            throw new NotImplementedException();
        }

        public override void Render(SKCanvas canvas, FontManager? fontManager = null)
        {
            if (Points.Count < 2)
            {
                return;
            }

            SKPath path = Utilities.BuildPath(Points);

            path.GetBounds(out SKRect bounds);

            Bounds = bounds;

            using SKPaint paint = new()
            {
                Style = SKPaintStyle.Stroke,
                Color = Color,
                StrokeWidth = BrushSize,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round
            };

            canvas.DrawPath(path, paint);

            path.Dispose();
        }
    }
}
