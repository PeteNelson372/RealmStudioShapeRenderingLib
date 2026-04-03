using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    using SkiaSharp;

    public class TransformWidget
    {
        public ITransformable2D? Target { get; set; }

        // --- Interaction state ---
        private TransformHandle _activeHandle = TransformHandle.None;

        private SKPoint _startMouse;
        private SKPoint _startLocation;
        private float _startRotation;
        private float _startScale;

        // --- Geometry cache (updated per-frame) ---
        private SKPoint[] _corners = new SKPoint[4];
        private SKPoint _top, _right, _bottom, _left;
        private SKPoint _rotateHandle;

        // --- Visual constants ---
        private float HandleSize = 4.5f;
        private float HitRadius = 8f;
        private float HandleStrokeWidth = 1f;
        private float RotateHandleOffset = 30f;

        // --- Public state helpers ---
        public bool IsActive => _activeHandle != TransformHandle.None;
        public TransformHandle ActiveHandle => _activeHandle;

        private TransformHandle _hoverHandle = TransformHandle.None;
        public TransformHandle HoverHandle => _hoverHandle;

        // =========================
        // Geometry
        // =========================

        private static SKPoint Mid(SKPoint a, SKPoint b)
            => new((a.X + b.X) * 0.5f, (a.Y + b.Y) * 0.5f);

        private void UpdateGeometry()
        {
            if (Target == null)
                return;

            _corners = Target.GetTransformedCorners();

            _top = Mid(_corners[0], _corners[1]);
            _right = Mid(_corners[1], _corners[2]);
            _bottom = Mid(_corners[2], _corners[3]);
            _left = Mid(_corners[3], _corners[0]);

            // Rotation handle: outward from top edge midpoint
            var center = Target.Location;
            var dir = new SKPoint(_top.X - center.X, _top.Y - center.Y);
            float len = MathF.Sqrt(dir.X * dir.X + dir.Y * dir.Y);

            if (len > 1e-5f)
                dir = new SKPoint(dir.X / len, dir.Y / len);
            else
                dir = new SKPoint(0, -1); // fallback

            _rotateHandle = new SKPoint(
                _top.X + dir.X * RotateHandleOffset,
                _top.Y + dir.Y * RotateHandleOffset);
        }

        // =========================
        // Rendering
        // =========================

        public void Render(SKCanvas canvas, float zoom)
        {
            if (Target == null)
            {
                return;
            }

            HandleSize = Utilities.Clamp(4.5f / zoom, 2f, 12f);

            HitRadius = 8f / zoom;
            HandleStrokeWidth = 1f / zoom;

            UpdateGeometry();

            var outlinePaint = PaintObjects.TransformHandleOutlinePaint;
            outlinePaint.StrokeWidth = HandleStrokeWidth;

            var handlePaint = PaintObjects.TransformHandlePaint.Clone();

            var handleOutlinePaint = PaintObjects.TransformHandleOutlinePaint.Clone();
            handleOutlinePaint.StrokeWidth = HandleStrokeWidth;

            var hoverFillPaint = PaintObjects.TransformHandleHoverFillPaint.Clone();

            var rotatePaint = PaintObjects.TransformRotatePaint.Clone();
            var rotateHoverPaint = PaintObjects.TransformRotateHoverPaint.Clone();

            // Draw rotated selection quad
            using (var path = new SKPath())
            {
                path.MoveTo(_corners[0]);
                path.LineTo(_corners[1]);
                path.LineTo(_corners[2]);
                path.LineTo(_corners[3]);
                path.Close();

                canvas.DrawPath(path, outlinePaint);
            }

            void DrawHandle(SKPoint p, TransformHandle handleType)
            {
                bool isHovered = (_hoverHandle == handleType);

                var fill = isHovered ? hoverFillPaint : handlePaint;
                var stroke = handleOutlinePaint;

                canvas.DrawCircle(p, HandleSize, fill);
                canvas.DrawCircle(p, HandleSize, stroke);
            }

            // Corners
            DrawHandle(_corners[0], TransformHandle.TopLeft);
            DrawHandle(_corners[1], TransformHandle.TopRight);
            DrawHandle(_corners[2], TransformHandle.BottomRight);
            DrawHandle(_corners[3], TransformHandle.BottomLeft);

            // Edges
            DrawHandle(_top, TransformHandle.Top);
            DrawHandle(_right, TransformHandle.Right);
            DrawHandle(_bottom, TransformHandle.Bottom);
            DrawHandle(_left, TransformHandle.Left);

            // Rotation handle
            canvas.DrawLine(_top, _rotateHandle, outlinePaint);

            bool rotateHovered = (_hoverHandle == TransformHandle.Rotate);

            var fill = rotateHovered ? rotateHoverPaint : rotatePaint;

            canvas.DrawCircle(_rotateHandle, HandleSize + 1, fill);
            canvas.DrawCircle(_rotateHandle, HandleSize + 1, handleOutlinePaint);
        }

        // =========================
        // Handle Hovering
        // =========================

        public void UpdateHover(SKPoint mouse, float zoom)
        {
            if (Target == null)
            {
                _hoverHandle = TransformHandle.None;
                return;
            }

            UpdateGeometry();

            float hitRadius = 10f / zoom;

            bool Near(SKPoint p)
            {
                return SKPoint.DistanceSquared(p, mouse) <= (hitRadius * hitRadius);
            }

            if (Near(_corners[0])) { _hoverHandle = TransformHandle.TopLeft; return; }
            if (Near(_corners[1])) { _hoverHandle = TransformHandle.TopRight; return; }
            if (Near(_corners[2])) { _hoverHandle = TransformHandle.BottomRight; return; }
            if (Near(_corners[3])) { _hoverHandle = TransformHandle.BottomLeft; return; }

            if (Near(_top)) { _hoverHandle = TransformHandle.Top; return; }
            if (Near(_right)) { _hoverHandle = TransformHandle.Right; return; }
            if (Near(_bottom)) { _hoverHandle = TransformHandle.Bottom; return; }
            if (Near(_left)) { _hoverHandle = TransformHandle.Left; return; }

            if (Near(_rotateHandle)) { _hoverHandle = TransformHandle.Rotate; return; }

            _hoverHandle = TransformHandle.None;
        }

        // =========================
        // Hit Testing
        // =========================

        public TransformHandle HitTest(SKPoint mouse)
        {
            if (Target == null)
            {
                return TransformHandle.None;
            }

            UpdateGeometry();

            bool Near(SKPoint p)
            {
                float dx = p.X - mouse.X;
                float dy = p.Y - mouse.Y;
                return (dx * dx + dy * dy) <= (HitRadius * HitRadius);
            }

            // Corners
            if (Near(_corners[0])) return TransformHandle.TopLeft;
            if (Near(_corners[1])) return TransformHandle.TopRight;
            if (Near(_corners[2])) return TransformHandle.BottomRight;
            if (Near(_corners[3])) return TransformHandle.BottomLeft;

            // Edges
            if (Near(_top)) return TransformHandle.Top;
            if (Near(_right)) return TransformHandle.Right;
            if (Near(_bottom)) return TransformHandle.Bottom;
            if (Near(_left)) return TransformHandle.Left;

            // Rotation
            if (Near(_rotateHandle)) return TransformHandle.Rotate;

            // Fallback: inside shape is move
            if (PointInQuad(mouse, _corners))
            {
                return TransformHandle.Move;
            }

            return TransformHandle.None;
        }

        private static bool PointInQuad(SKPoint p, SKPoint[] c)
        {
            float Sign(SKPoint p1, SKPoint p2, SKPoint p3)
            {
                return (p1.X - p3.X) * (p2.Y - p3.Y) -
                       (p2.X - p3.X) * (p1.Y - p3.Y);
            }

            bool b1 = Sign(p, c[0], c[1]) < 0.0f;
            bool b2 = Sign(p, c[1], c[2]) < 0.0f;
            bool b3 = Sign(p, c[2], c[3]) < 0.0f;
            bool b4 = Sign(p, c[3], c[0]) < 0.0f;

            return (b1 == b2) && (b2 == b3) && (b3 == b4);
        }

        // =========================
        // Interaction
        // =========================

        public TransformHandle OnMouseDown(SKPoint mouse)
        {
            if (Target == null)
            {
                return TransformHandle.None;
            }

            _activeHandle = HitTest(mouse);

            _startMouse = mouse;
            _startLocation = Target.Location;
            _startRotation = Target.Rotation;
            _startScale = Target.Scale;

            return _activeHandle;
        }

        public void OnMouseMove(SKPoint mouse)
        {
            if (Target == null || _activeHandle == TransformHandle.None)
            {
                return;
            }

            switch (_activeHandle)
            {
                case TransformHandle.Move:
                    ApplyMove(mouse);
                    break;

                case TransformHandle.Rotate:
                    ApplyRotation(mouse);
                    break;

                default:
                    ApplyScale(mouse); // uniform for now
                    break;
            }
        }

        public void OnMouseUp()
        {
            _activeHandle = TransformHandle.None;
        }

        // =========================
        // Transform Operations
        // =========================

        private void ApplyMove(SKPoint mouse)
        {
            Target!.Location = new SKPoint(
                _startLocation.X + (mouse.X - _startMouse.X),
                _startLocation.Y + (mouse.Y - _startMouse.Y));
        }

        private void ApplyRotation(SKPoint mouse)
        {
            var center = _startLocation;

            float Angle(SKPoint p)
                => MathF.Atan2(p.Y - center.Y, p.X - center.X);

            float start = Angle(_startMouse);
            float current = Angle(mouse);

            float delta = (current - start) * 180f / MathF.PI;

            Target!.Rotation = _startRotation + delta;
        }

        private void ApplyScale(SKPoint mouse)
        {
            var center = _startLocation;

            float startDist = SKPoint.Distance(center, _startMouse);
            float currentDist = SKPoint.Distance(center, mouse);

            if (startDist > 1e-5f)
            {
                float factor = currentDist / startDist;
                Target!.Scale = _startScale * factor;
            }
        }
    }
}
