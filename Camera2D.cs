using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public class Camera2D
    {
        public event Action? ViewChanged;

        // mouse cursor points
        private SKPoint _scrollPoint = new(0, 0);
        private SKPoint _drawingPoint = new(0, 0);

        private SKPoint _currentMouseLocation = SKPoint.Empty;
        private SKPoint _previousMouseLocation = SKPoint.Empty;

        private SKPoint _currentCursorPoint = new(0, 0);
        private SKPoint _previousCursorPoint = new(0, 0);

        private bool _isPanning;
        private DateTime _lastMouseMoveTime;

        private SKRect _viewport = SKRect.Empty;

        public bool IsPanning
        {
            get { return _isPanning; }
            set
            {
                _isPanning = value;
                ViewChanged?.Invoke();
            }
        }

        public DateTime LastMouseMoveTime
        {
            get { return _lastMouseMoveTime; }
            set { _lastMouseMoveTime = value; }
        }

        public SKPoint ScrollPoint
        {
            get { return _scrollPoint; }
            set { _scrollPoint = value; }
        }

        public SKPoint DrawingPoint
        {
            get { return _drawingPoint; }
            set { _drawingPoint = value; }
        }

        public SKPoint CurrentMouseLocation
        {
            get { return _currentMouseLocation; }
            set { _currentMouseLocation = value; }
        }

        public SKPoint PreviousMouseLocation
        {
            get { return _previousMouseLocation; }
            set { _previousMouseLocation = value; }
        }

        public SKPoint CurrentCursorPoint
        {
            get { return _currentCursorPoint; }
            set { _currentCursorPoint = value; }
        }

        public SKPoint PreviousCursorPoint
        {
            get { return _previousCursorPoint; }
            set { _previousCursorPoint = value; }
        }

        public SKRect Viewport
        {
            get { return _viewport; }
            set { _viewport = value; }
        }

        private SKPoint _velocity = new(0, 0);

        public float Zoom { get; private set; } = 1.0f;
        public SKPoint Pan { get; private set; } = new SKPoint(0, 0);

        private const float MinZoom = 0.05f;
        private const float MaxZoom = 20f;

        public SKMatrix GetMatrix()
        {
            var m = SKMatrix.CreateScale(Zoom, Zoom);
            m.PostConcat(SKMatrix.CreateTranslation(Pan.X, Pan.Y));
            return m;
        }

        public void Apply(SKCanvas canvas)
        {
            canvas.Translate(Pan.X, Pan.Y);
            canvas.Scale(Zoom);
        }

        public void PanBy(SKPoint deltaPixels, float viewWidth, float viewHeight)
        {
            Pan = new SKPoint(Pan.X + deltaPixels.X, Pan.Y + deltaPixels.Y);
            UpdateViewport(viewWidth, viewHeight);

            ViewChanged?.Invoke();
        }

        public void SetPan(SKPoint pan, float viewWidth, float viewHeight)
        {
            Pan = pan;
            UpdateViewport(viewWidth, viewHeight);

            ViewChanged?.Invoke();
        }

        public void SetZoom(float zoom, float viewWidth, float viewHeight)
        {
            Zoom = Utilities.Clamp(zoom, MinZoom, MaxZoom);
            UpdateViewport(viewWidth, viewHeight);

            ViewChanged?.Invoke();
        }

        public void ZoomAtScreenPoint(float newZoom, SKPoint screenPoint, float viewWidth, float viewHeight)
        {
            float oldZoom = Zoom;
            newZoom = Utilities.Clamp(newZoom, MinZoom, MaxZoom);

            if (Math.Abs(newZoom - oldZoom) < 0.0001f)
                return;

            float ratio = newZoom / oldZoom;

            Pan = new SKPoint(
                screenPoint.X - (screenPoint.X - Pan.X) * ratio,
                screenPoint.Y - (screenPoint.Y - Pan.Y) * ratio
            );

            Zoom = newZoom;
            UpdateViewport(viewWidth, viewHeight);

            ViewChanged?.Invoke();
        }

        public void Reset(float viewWidth, float viewHeight)
        {
            Zoom = 1.0f;
            Pan = new SKPoint(0, 0);
            _velocity = new SKPoint(0, 0);
            UpdateViewport(viewWidth, viewHeight);

            ViewChanged?.Invoke();
        }

        public void ZoomToFit(float mapWidth, float mapHeight)
        {
            float zoomX = Viewport.Width / mapWidth;
            float zoomY = Viewport.Height / mapHeight;

            float zoom = MathF.Min(zoomX, zoomY);

            SetZoom(zoom, Viewport.Width, Viewport.Height);

            SetPan(new SKPoint(0, 0), Viewport.Width, Viewport.Height);

            ViewChanged?.Invoke();
        }

        private void UpdateViewport(float viewWidth, float viewHeight)
        {
            float left = (-Pan.X) / Zoom;
            float top = (-Pan.Y) / Zoom;

            float right = (viewWidth - Pan.X) / Zoom;
            float bottom = (viewHeight - Pan.Y) / Zoom;

            Viewport = new SKRect(left, top, right, bottom);
        }

        public void ClampToWorld(SKRect worldBounds, SKSize viewport)
        {
            float viewWorldW = viewport.Width / Zoom;
            float viewWorldH = viewport.Height / Zoom;

            // If world is smaller than viewport → center
            float panX, panY;

            if (worldBounds.Width <= viewWorldW)
            {
                panX = (viewport.Width - worldBounds.Width * Zoom) / 2f;
            }
            else
            {
                float minPanX = -worldBounds.Right * Zoom + viewport.Width;
                float maxPanX = -worldBounds.Left * Zoom;
                panX = Utilities.Clamp(Pan.X, minPanX, maxPanX);
            }

            if (worldBounds.Height <= viewWorldH)
            {
                panY = (viewport.Height - worldBounds.Height * Zoom) / 2f;
            }
            else
            {
                float minPanY = -worldBounds.Bottom * Zoom + viewport.Height;
                float maxPanY = -worldBounds.Top * Zoom;
                panY = Utilities.Clamp(Pan.Y, minPanY, maxPanY);
            }

            Pan = new SKPoint(panX, panY);
        }

        public void AddVelocity(SKPoint pixelsPerSecond)
        {
            _velocity += pixelsPerSecond;
        }

        public void UpdateInertia(float deltaSeconds, SKRect worldBounds, SKSize viewport)
        {
            // Stop when very slow
            if (_velocity.X * _velocity.X + _velocity.Y * _velocity.Y < 0.01f)
            {
                _velocity = new SKPoint(0, 0);
                return;
            }

            // Advance pan
            Pan = new SKPoint(
                Pan.X + _velocity.X * deltaSeconds,
                Pan.Y + _velocity.Y * deltaSeconds
            );

            // Friction (explicit)
            _velocity = new SKPoint(
                _velocity.X * 0.95f,
                _velocity.Y * 0.95f
            );

            ClampToWorld(worldBounds, viewport);
        }
    }
}
