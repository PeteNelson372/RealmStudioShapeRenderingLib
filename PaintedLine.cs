using SkiaSharp;
using System.ComponentModel;
using System.Xml.Serialization;

namespace RealmStudioShapeRenderingLib
{
    public sealed class PaintedLine : MapComponent2D, IDrawnMapComponent, IDisposable
    {
        private bool _disposed;

        // =================================================
        // Stroke data
        // =================================================

        private readonly List<SKPoint> _points = [];

        // =================================================
        // Brush
        // =================================================

        private PreparedBrush? _brush;


        // =================================================
        // Clipped rendering
        // =================================================

        [DefaultValue(false)]
        public bool RequiresLandformClipping { get; set; } = false;

        [DefaultValue(false)]
        public bool RequiresWaterSystemClipping { get; set; } = false;

        // =================================================
        // Rendering surface
        // =================================================

        private SKSurface? _surface;

        private SKCanvas? _surfaceCanvas;

        private SKImage? _cachedImage;

        // =================================================
        // Paint resources
        // =================================================

        private readonly SKPaint _brushPaint = new()
        {
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
        };

        // =================================================
        // Dirty rect tracking
        // =================================================

        private SKRect _dirtyRect = SKRect.Empty;

        // =================================================
        // Stroke state
        // =================================================

        [XmlIgnore]
        public bool IsFinalized { get; private set; }

        // =================================================
        // Brush behavior
        // =================================================

        [XmlElement]
        public int DefaultSpacing { get; set; } = 8;

        [XmlElement]
        public int BrushSpacing { get; set; } = 8;

        [XmlElement]
        public bool RandomRotation { get; set; } = false;

        // =================================================
        // Properties
        // =================================================

        [XmlIgnore]
        public List<SKPoint> Points => _points;

        [XmlElement("Points")]
        public string PointsList
        {
            get => string.Join(";", Points.Select(p => $"{p.X},{p.Y}"));

            set
            {
                Points.Clear();

                if (string.IsNullOrWhiteSpace(value))
                {
                    return;
                }

                foreach (string pair in value.Split(';'))
                {
                    string[] parts = pair.Split(',');

                    Points.Add(
                        new SKPoint(
                            float.Parse(parts[0]),
                            float.Parse(parts[1])));
                }
            }
        }

        [XmlElement]
        public PreparedBrush? Brush
        {
            get => _brush;

            set
            {
                _brush = value;

            }
        }

        public SKRect DirtyRect => _dirtyRect;

        // =================================================
        // Initialization
        // =================================================

        public void Initialize(int width, int height)
        {
            _surface?.Dispose();

            _surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));

            _surfaceCanvas = _surface.Canvas;

            _surfaceCanvas.Clear(SKColors.Transparent);

            IsFinalized = false;
        }

        public override void FinalizeShapeGeometry(RealmStudioMap map)
        {
            Initialize(map.MapWidth, map.MapHeight);

            foreach (SKPoint point in Points)
            {
                StampBrush(point);
            }

            UpdateBounds();

            FinalizeStroke();
        }


        // =================================================
        // Add paint point
        // =================================================

        public void AddPoint(SKPoint point)
        {
            if (_surfaceCanvas == null)
            {
                throw new Exception("PaintedLine canvas is null. Was Initialize() called?");
            }

            if (_points.Count == 0)
            {
                _points.Add(point);
                StampBrush(point);
                return;
            }

            float minDistance = Brush!.BrushSize * (Brush!.BrushSpacing / 100.0f);

            if (SKPoint.Distance(_points[^1], point) >= minDistance)
            {
                _points.Add(point);
                StampBrush(point);
            }

            UpdateBounds();
        }

        // =================================================
        // Stamp brush
        // =================================================

        private void StampBrush(SKPoint point)
        {
            if (_surfaceCanvas == null)
            {
                return;
            }

            SKRect destRect = new(
                point.X - Brush!.BrushSize / 2f,
                point.Y - Brush!.BrushSize / 2f,
                point.X + Brush!.BrushSize / 2f,
                point.Y + Brush!.BrushSize / 2f);

            SKBitmap? renderBitmap = null;

            switch (Brush?.SourceBrush?.BrushSelectionMode)
            {
                case BrushSelectionMode.Single:
                    renderBitmap = Brush!.Bitmaps[0];
                    break;

                case BrushSelectionMode.Random:
                    renderBitmap = Brush!.Bitmaps[Random.Shared.Next(Brush.Bitmaps.Count)];
                    break;

                case BrushSelectionMode.Sequential:
                    renderBitmap = Brush!.Bitmaps[_points.Count % Brush.Bitmaps.Count];
                    break;
            }

            if (renderBitmap == null)
            {
                return;
            }

            //
            // World-aligned pattern brush
            //

            if (Brush != null && Brush.SourceBrush != null && Brush.SourceBrush.WorldAligned)
            {
                using SKPaint patternPaint = new();

                patternPaint.Shader = SKShader.CreateBitmap(renderBitmap, SKShaderTileMode.Repeat, SKShaderTileMode.Repeat);

                _surfaceCanvas.DrawCircle(point.X, point.Y, Brush.BrushSize / 2f, patternPaint);
            }

            //
            // Normal stamp brush
            //

            else
            {
                float angle = 0;

                if (RandomRotation)
                {
                    angle = Random.Shared.NextSingle() * 360f;
                }

                using (new SKAutoCanvasRestore(_surfaceCanvas))
                {
                    if (RandomRotation)
                    {
                        _surfaceCanvas.Translate(point.X, point.Y);
                        _surfaceCanvas.RotateDegrees(angle);
                        _surfaceCanvas.Translate(-point.X, -point.Y);
                    }

                    _surfaceCanvas.DrawBitmap(renderBitmap, destRect, SKSamplingOptions.Default);
                }
            }

            if (_dirtyRect.IsEmpty)
            {
                _dirtyRect = destRect;
            }
            else
            {
                _dirtyRect.Union(destRect);
            }
        }

        // =================================================
        // Finalize
        // =================================================

        public void FinalizeStroke()
        {
            if (_surface == null)
            {
                return;
            }

            _cachedImage?.Dispose();

            _cachedImage = _surface.Snapshot();

            IsFinalized = true;
        }

        // =================================================
        // Bounds
        // =================================================

        private void UpdateBounds()
        {
            if (_points.Count == 0)
            {
                return;
            }

            float minX = _points.Min(p => p.X);

            float minY = _points.Min(p => p.Y);

            float maxX = _points.Max(p => p.X);

            float maxY = _points.Max(p => p.Y);

            Bounds =
                new SKRect(
                    minX - Brush!.BrushSize,
                    minY - Brush!.BrushSize,
                    maxX + Brush!.BrushSize,
                    maxY + Brush!.BrushSize);
        }

        // =================================================
        // Render
        // =================================================

        public override void Render(SKCanvas canvas, FontManager? fontManager = null, SKPath? clipPath = null)
        {
            using (new SKAutoCanvasRestore(canvas))
            {
                if (RequiresLandformClipping || RequiresWaterSystemClipping)
                {
                    if (clipPath == null || clipPath.IsEmpty)
                    {
                        // clipping is required (PaintedLine drawn on LANDDRAWINGLAYER or WATERDRAWINGLAYER)
                        // but the clip path is empty or null, so don't render the PaintedLine
                        return;
                    }

                    canvas.ClipPath(clipPath);

                    // debugging
                    //canvas.DrawPath(clipPath, PaintObjects.DebugPaint2);
                }

                // ---------------------------------------------
                // Active stroke
                // ---------------------------------------------

                if (!IsFinalized && _surface != null)
                {
                    using SKImage image = _surface.Snapshot();

                    canvas.DrawImage(image, 0, 0, SKSamplingOptions.Default);

                    return;
                }

                // ---------------------------------------------
                // Cached finalized stroke
                // ---------------------------------------------

                if (_cachedImage != null)
                {
                    canvas.DrawImage(_cachedImage, 0, 0, SKSamplingOptions.Default);
                }
            }
        }

        // =================================================
        // Hit testing
        // =================================================

        public override bool HitTest(SKPoint worldPos)
        {
            return Bounds.Contains(worldPos);
        }

        // =================================================
        // Undo/redo
        // =================================================

        public override IShapeState CaptureState()
        {
            throw new NotImplementedException();
        }

        public override void RestoreState(IShapeState state)
        {
            throw new NotImplementedException();
        }

        // =================================================
        // Dispose
        // =================================================

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _brushPaint.Dispose();
                _cachedImage?.Dispose();
                _surfaceCanvas?.Dispose();
                _surface?.Dispose();
            }

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }
    }
}