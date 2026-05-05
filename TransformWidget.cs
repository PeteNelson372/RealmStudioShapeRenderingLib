namespace RealmStudioShapeRenderingLib
{
    using SkiaSharp;

    public class TransformWidget
    {
        public ITransformable2D? Target { get; set; }

        // --- Interaction state ---
        private TransformHandle _activeHandle = TransformHandle.None;

        private SKPoint _startMouse;
        private SKPoint _previousMouse;
        private SKPoint _startLocation;
        private float _startRotation;
        private float _startScale;

        // --- Geometry cache (updated per-frame) ---
        private SKPoint[] _corners = new SKPoint[4];
        private SKPoint _top;
        private SKPoint _right;
        private SKPoint _bottom;
        private SKPoint _left;
        private SKPoint _rotateHandle;
        private SKPoint _zTop;
        private SKPoint _zForward;
        private SKPoint _zBackward;
        private SKPoint _zBottom;

        // --- Visual constants ---
        private float HandleSize = 5f;
        private float HitRadius = 10f;
        private float HandleStrokeWidth = 1f;
        private float RotateHandleOffset = 30f;
        private float ZHandleOffset = 20f;

        // --- Public state helpers ---
        public bool IsActive => _activeHandle != TransformHandle.None;
        public TransformHandle ActiveHandle => _activeHandle;

        private TransformHandle _hoverHandle = TransformHandle.None;
        public TransformHandle HoverHandle => _hoverHandle;

        // =========================
        // Geometry
        // =========================

        public void UpdateWidgetGeometry()
        {
            UpdateGeometry();
        }

        private static SKPoint Mid(SKPoint a, SKPoint b)
            => new((a.X + b.X) * 0.5f, (a.Y + b.Y) * 0.5f);

        private void UpdateGeometry()
        {
            if (Target == null)
            {
                return;
            }

            _corners = Target.GetTransformedCorners();

            _top = Mid(_corners[0], _corners[1]);
            _right = Mid(_corners[1], _corners[2]);
            _bottom = Mid(_corners[2], _corners[3]);
            _left = Mid(_corners[3], _corners[0]);

            // -------------------------------------------------
            // Direction from center → top (orientation-aware)
            // -------------------------------------------------
            var bounds = ((MapComponent2D)Target).Bounds;
            var center = new SKPoint(bounds.MidX, bounds.MidY);

            var dir = new SKPoint(_top.X - center.X, _top.Y - center.Y);
            float len = MathF.Sqrt(dir.X * dir.X + dir.Y * dir.Y);

            if (len > 1e-5f)
            {
                dir = new SKPoint(dir.X / len, dir.Y / len);
            }
            else
            {
                dir = new SKPoint(0, -1); // fallback
            }

            // -------------------------------------------------
            // Adaptive scaling based on symbol size
            // -------------------------------------------------

            float width = ((MapComponent2D)Target).Bounds.Width;
            float height = ((MapComponent2D)Target).Bounds.Height;

            // diagonal length
            float diag = MathF.Sqrt(width * width + height * height);

            // scale factor (tweak thresholds as needed)
            float sizeScale = Utilities.Clamp(diag / 200f, 0.5f, 1.0f);

            // scaled offsets
            float outward = ZHandleOffset * 1.5f * sizeScale;
            float along = ZHandleOffset * 1.5f * sizeScale;

            // -------------------------------------------------
            // RIGHT EDGE (top controls)
            // -------------------------------------------------

            // outward normal (center → right edge)
            var rightDir = new SKPoint(_right.X - center.X, _right.Y - center.Y);
            float lenR = MathF.Sqrt(rightDir.X * rightDir.X + rightDir.Y * rightDir.Y);

            if (lenR > 1e-5f)
            {
                rightDir = new SKPoint(rightDir.X / lenR, rightDir.Y / lenR);
            }
            else
            {
                rightDir = new SKPoint(1, 0);
            }

            // edge direction (top to bottom)
            var edgeDir = new SKPoint(
                _corners[2].X - _corners[1].X,
                _corners[2].Y - _corners[1].Y);

            float lenE = MathF.Sqrt(edgeDir.X * edgeDir.X + edgeDir.Y * edgeDir.Y);

            if (lenE > 1e-5f)
            {
                edgeDir = new SKPoint(edgeDir.X / lenE, edgeDir.Y / lenE);
            }
            else
            {
                edgeDir = new SKPoint(0, 1);
            }

            // anchors
            var topRight = _corners[1];

            // top pair (right side)
            _zTop = new SKPoint(
                topRight.X + rightDir.X * outward,
                topRight.Y + rightDir.Y * outward);

            _zForward = new SKPoint(
                _zTop.X + edgeDir.X * along,
                _zTop.Y + edgeDir.Y * along);

            // -------------------------------------------------
            // LEFT EDGE (bottom controls)
            // -------------------------------------------------

            // outward normal (center → left edge)
            var leftDir = new SKPoint(_left.X - center.X, _left.Y - center.Y);
            float lenL = MathF.Sqrt(leftDir.X * leftDir.X + leftDir.Y * leftDir.Y);

            if (lenL > 1e-5f)
            {
                leftDir = new SKPoint(leftDir.X / lenL, leftDir.Y / lenL);
            }
            else
            {
                leftDir = new SKPoint(-1, 0);
            }

            // edge direction (bottom to top for stacking upward)
            var edgeDirUp = new SKPoint(-edgeDir.X, -edgeDir.Y);

            // anchor
            var bottomLeft = _corners[3];

            // bottom pair (left side)
            _zBottom = new SKPoint(
                bottomLeft.X + leftDir.X * outward,
                bottomLeft.Y + leftDir.Y * outward);

            _zBackward = new SKPoint(
                _zBottom.X + edgeDirUp.X * along,
                _zBottom.Y + edgeDirUp.Y * along);

            // -------------------------------------------------
            // Rotation handle
            // -------------------------------------------------
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

            canvas.Save();

            float diag = SKPoint.Distance(_corners[0], _corners[2]);
            float sizeScale = Utilities.Clamp(diag / 200f, 0.5f, 1.0f);

            HandleSize = Utilities.Clamp((4.5f * sizeScale) / zoom, 3f, 20f);
            HitRadius = Utilities.Clamp(HandleSize, 3f, 20f);

            HandleStrokeWidth = 1f / zoom;
            RotateHandleOffset = 30f / zoom;
            ZHandleOffset = 25f / zoom;

            UpdateGeometry();

            var outlinePaint = PaintObjects.TransformHandleOutlinePaint;
            outlinePaint.StrokeWidth = HandleStrokeWidth;

            var handleOutlinePaint = PaintObjects.TransformHandleOutlinePaint.Clone();
            handleOutlinePaint.StrokeWidth = HandleStrokeWidth;

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

                SKPaint paint;
                SKPaint hoverPaint;

                switch (handleType)
                {
                    case TransformHandle.Top:
                    case TransformHandle.Bottom:
                    case TransformHandle.Left:
                    case TransformHandle.Right:
                    case TransformHandle.TopLeft:
                    case TransformHandle.TopRight:
                    case TransformHandle.BottomLeft:
                    case TransformHandle.BottomRight:
                        {
                            paint = PaintObjects.TransformHandlePaint.Clone();
                            hoverPaint = PaintObjects.TransformHandleHoverFillPaint.Clone();
                        }
                        break;
                    case TransformHandle.ZForward:
                    case TransformHandle.ZBackward:
                    case TransformHandle.ZTop:
                    case TransformHandle.ZBottom:
                        {
                            paint = PaintObjects.TransformZOrderPaint.Clone();
                            hoverPaint = PaintObjects.TransformZOrderHoverPaint.Clone();
                        }
                        break;
                    default:
                        {
                            paint = PaintObjects.TransformHandlePaint.Clone();
                            hoverPaint = PaintObjects.TransformHandleHoverFillPaint.Clone();
                        }
                        break;

                }

                var fill = isHovered ? hoverPaint : paint;
                var stroke = handleOutlinePaint;

                canvas.DrawCircle(p, HandleSize, fill);
                canvas.DrawCircle(p, HandleSize, stroke);
            }

            // Corners
            DrawHandle(_corners[0], TransformHandle.TopLeft);
            DrawHandle(_corners[1], TransformHandle.TopRight);
            DrawHandle(_corners[2], TransformHandle.BottomRight);
            DrawHandle(_corners[3], TransformHandle.BottomLeft);

            // Rotate handle connection line (line from top-center to rotate handle)
            canvas.DrawLine(_top, _rotateHandle, outlinePaint);

            // Edges
            DrawHandle(_top, TransformHandle.Top);
            DrawHandle(_right, TransformHandle.Right);
            DrawHandle(_bottom, TransformHandle.Bottom);
            DrawHandle(_left, TransformHandle.Left);

            // Rotation handle
            bool rotateHovered = (_hoverHandle == TransformHandle.Rotate);

            var fill = rotateHovered ? rotateHoverPaint : rotatePaint;

            canvas.DrawCircle(_rotateHandle, HandleSize + 1, fill);
            canvas.DrawCircle(_rotateHandle, HandleSize + 1, handleOutlinePaint);

            DrawHandle(_zForward, TransformHandle.ZForward);
            DrawHandle(_zBackward, TransformHandle.ZBackward);
            DrawHandle(_zTop, TransformHandle.ZTop);
            DrawHandle(_zBottom, TransformHandle.ZBottom);

            if (Target is MapLabel label)
            {
                using SKPaint paint = new()
                {
                    Style = SKPaintStyle.Fill,
                    Color = SKColors.LightGray,
                };

                // -------------------------------------------------
                // Match MapLabel bounds model
                // -------------------------------------------------
                SKRect bounds = SKRect.Empty;

                if (label.RenderFont != null && label.CurvePath != null)
                {
                    using SKFont f = label.GetFont();

                    if (!string.IsNullOrEmpty(label.Text))
                    {
                        f.MeasureText(label.Text, out bounds);
                    }
                    else
                    {
                        f.MeasureText("Mg", out bounds);
                    }

                    // APPLY SAME INFLATION AS MapLabel
                    float inflateX = MathF.Max(5, bounds.Width * 0.01f);
                    float inflateY = MathF.Max(5, bounds.Height * 0.01f);

                    bounds.Inflate(inflateX, inflateY);

                    // -------------------------------------------------
                    // CENTER-CENTER anchoring (matches MapLabel)
                    // -------------------------------------------------
                    float centerOffsetX = (bounds.Left + bounds.Right) * 0.5f;
                    float centerOffsetY = (bounds.Top + bounds.Bottom) * 0.5f;

                    float x = label.Location.X - centerOffsetX;
                    float y = label.Location.Y - centerOffsetY;

                    // -------------------------------------------------
                    // Draw text
                    // -------------------------------------------------
                    canvas.DrawText(label.Text, x, y, f, paint);

                    // -------------------------------------------------
                    // Draw the curve path
                    // -------------------------------------------------
                    canvas.DrawPath(label.CurvePath, PaintObjects.LabelPathPaint);
                }
            }

            canvas.Restore();
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

            if (Near(_zTop)) { _hoverHandle = TransformHandle.ZTop; return; }
            if (Near(_zForward)) { _hoverHandle = TransformHandle.ZForward; return; }
            if (Near(_zBottom)) { _hoverHandle = TransformHandle.ZBottom; return; }
            if (Near(_zBackward)) { _hoverHandle = TransformHandle.ZBackward; return; }

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
                return SKPoint.DistanceSquared(p, mouse) <= HitRadius * HitRadius;
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

            // Z Handles
            if (Near(_zTop)) return TransformHandle.ZTop;
            if (Near(_zForward)) return TransformHandle.ZForward;
            if (Near(_zBottom)) return TransformHandle.ZBottom;
            if (Near(_zBackward)) return TransformHandle.ZBackward;

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
            static float Sign(SKPoint p1, SKPoint p2, SKPoint p3)
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

            Target.BeginScale();

            _activeHandle = HitTest(mouse);

            // don't start dragging if the handle is one of the z-order handles
            if (_activeHandle == TransformHandle.ZTop
                || _activeHandle == TransformHandle.ZBottom
                || _activeHandle == TransformHandle.ZBackward
                || _activeHandle == TransformHandle.ZForward)
            {
                return _activeHandle;
            }

            _startMouse = mouse;
            _previousMouse = mouse;
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

            _previousMouse = mouse;
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

            if (Target is MapLabel label && label.CurvePath != null)
            {
                label.CurvePath.Offset(mouse.X - _previousMouse.X, mouse.Y - _previousMouse.Y);
            }
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
                Target!.ApplyScale(factor);
            }
        }
    }
}
