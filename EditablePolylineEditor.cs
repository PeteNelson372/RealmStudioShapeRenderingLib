using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public class EditablePolylineEditor
    {
        private List<SKPoint> _points;

        public List<SKPoint> Points
        {
            get { return _points; }
            set { _points = value; }
        }

        private int _hoverIndex = -1;
        private int _activeIndex = -1;
        private bool _isDragging = false;

        public EditablePolylineEditor(List<SKPoint> points)
        {
            _points = points;
        }

        public Action? OnChanged;

        protected List<int> EditableIndices { get; } = new();

        public int? SelectedEditableIndex { get; set; }

        public bool IsEditing { get; set; }

        private readonly static SKPaint ControlPointPaint = new()
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
            StrokeWidth = 2,
            Color = SKColors.LightYellow,
        };

        private readonly static SKPaint ControlPointOutlinePaint = new()
        {
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            StrokeWidth = 1,
            Color = SKColors.Black
        };

        private readonly static SKPaint SelectedControlPointPaint = new()
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
            StrokeWidth = 2,
            Color = SKColors.BlueViolet,
        };

        public bool IsDrawing { get; private set; }

        private SKPoint? _lastPoint;



        private void NotifyChanged()
        {
            OnChanged?.Invoke();
        }

        // ----------------------------------
        // Drawing
        // ----------------------------------

        public void BeginDraw(SKPoint start)
        {
            _points.Clear();
            _points.Add(start);

            _lastPoint = start;
            IsDrawing = true;

            NotifyChanged();
        }

        public void ContinueDraw(SKPoint worldPos, bool ctrl, bool shift)
        {
            if (!IsDrawing || _points.Count < 1)
            {
                return;
            }

            var constrained = ApplyConstraints(_points[0], worldPos, ctrl, shift);

            _points.Add(constrained);
            _lastPoint = constrained;

            NotifyChanged();
        }

        public void EndDraw()
        {
            IsDrawing = false;
            _lastPoint = null;

            RebuildEditablePoints();

            NotifyChanged();
        }

        float? _lockedAngle = null;

        private SKPoint ApplyConstraints(SKPoint origin, SKPoint p, bool ctrl, bool shift)
        {
            var dx = p.X - origin.X;
            var dy = p.Y - origin.Y;

            if (!ctrl && !shift)
            {
                _lockedAngle = null;
                return p;
            }

            float angle = MathF.Atan2(dy, dx);
            float length = MathF.Sqrt(dx * dx + dy * dy);

            if (ctrl)
            {
                _lockedAngle = null;
                // Axis lock (0° or 90°)
                if (MathF.Abs(dx) > MathF.Abs(dy))
                {
                    return new SKPoint(origin.X + dx, origin.Y);
                }
                else
                {
                    return new SKPoint(origin.X, origin.Y + dy);
                }
            }

            if (shift)
            {
                // Snap to 5 degree increments

                if (_lockedAngle == null)
                {
                    const float step = MathF.PI / 36f; // 5 degrees
                    _lockedAngle = MathF.Round(angle / step) * step;
                }

                if (_lockedAngle != null)
                {
                    return new SKPoint(
                        origin.X + MathF.Cos(_lockedAngle.Value) * length,
                        origin.Y + MathF.Sin(_lockedAngle.Value) * length);
                }
            }

            return p;
        }

        // ----------------------------
        // Editable points
        // ----------------------------

        public void RebuildEditablePoints(int stride = 10)
        {
            EditableIndices.Clear();

            for (int i = 0; i < _points.Count; i += stride)
            {
                EditableIndices.Add(i);
            }

            if (!EditableIndices.Contains(0))
            {
                EditableIndices.Insert(0, 0);
            }

            int last = _points.Count - 1;

            if (!EditableIndices.Contains(last))
            {
                EditableIndices.Add(last);
            }
        }

        // ----------------------------
        // Hit testing
        // ----------------------------

        public int HitTestEditable(SKPoint worldPos, float radius)
        {
            if (EditableIndices.Count == 0)
            {
                throw new Exception("Editable Polyline Editor. HitTestEditable. EditableIndices count is zero");
            }

            if (_points.Count == 0)
            {
                throw new Exception("Editable Polyline Editor. HitTestEditable, _points count is zero");
            }

            float r2 = radius * radius;

            for (int i = 0; i < EditableIndices.Count; i++)
            {
                var p = _points[EditableIndices[i]];

                if (SKPoint.DistanceSquared(p, worldPos) <= r2)
                    return i;
            }

            return -1;
        }

        // ----------------------------
        // Mouse interaction
        // ----------------------------

        public void OnMouseDown(SKPoint worldPos, float hitRadius)
        {
            int hit = HitTestEditable(worldPos, hitRadius);

            if (hit >= 0)
            {
                _activeIndex = hit;
                _isDragging = true;
            }
        }

        public void OnMouseMove(SKPoint worldPos, float hitRadius)
        {
            if (_isDragging && _activeIndex >= 0)
            {
                DragHandle(worldPos);
                SmoothPoints(1, 0.3f);

                //NotifyChanged();

                return;
            }

            _hoverIndex = HitTestEditable(worldPos, hitRadius);
        }

        public void OnMouseUp()
        {
            RebuildEditablePoints();

            _isDragging = false;
            _activeIndex = -1;
        }


        // ----------------------------
        // Editing (with falloff)
        // ----------------------------

        private void DragHandle(SKPoint newPos)
        {
            int pointIndex = EditableIndices[_activeIndex];
            var oldPos = _points[pointIndex];

            float dx = newPos.X - oldPos.X;
            float dy = newPos.Y - oldPos.Y;

            const int falloffRange = 10;

            for (int i = -falloffRange; i <= falloffRange; i++)
            {
                int idx = pointIndex + i;

                if (idx < 0 || idx >= _points.Count)
                {
                    continue;
                }

                float sigma = falloffRange * 0.5f;
                float t = MathF.Exp(-(i * i) / (2 * sigma * sigma));

                _points[idx] = new SKPoint(
                    (int)(Math.Round(_points[idx].X + dx * t)),
                    (int)(Math.Round(_points[idx].Y + dy * t)));
            }

            NotifyChanged();
        }

        // ----------------------------
        // Insert / Remove
        // ----------------------------

        public void InsertPoint(int index, SKPoint p)
        {
            _points.Insert(index, p);
            RebuildEditablePoints();
            NotifyChanged();
        }

        public void RemoveEditable(int editableIndex)
        {
            if (_points.Count <= 2)
                return;

            int idx = EditableIndices[editableIndex];

            _points.RemoveAt(idx);

            SelectedEditableIndex = null;

            RebuildEditablePoints();
            NotifyChanged();
        }

        // ----------------------------
        // Rendering helpers
        // ----------------------------

        public void SmoothPoints(int iterations = 1, float strength = 0.5f)
        {
            for (int it = 0; it < iterations; it++)
            {
                for (int i = 1; i < _points.Count - 1; i++)
                {
                    var prev = _points[i - 1];
                    var next = _points[i + 1];
                    var current = _points[i];

                    var avg = new SKPoint(
                        (prev.X + next.X) * 0.5f,
                        (prev.Y + next.Y) * 0.5f);

                    _points[i] = new SKPoint(
                        current.X + (avg.X - current.X) * strength,
                        current.Y + (avg.Y - current.Y) * strength);
                }
            }
        }

        public void RenderEditableHandles(SKCanvas canvas, float zoom)
        {
            if (!IsEditing)
            {
                return;
            }

            float r = 4f / zoom;

            for (int i = 0; i < EditableIndices.Count; i++)
            {
                int idx = EditableIndices[i];

                if (idx < 0 || idx >= _points.Count)
                {
                    continue;
                }

                if (i == _hoverIndex)
                {
                    canvas.DrawCircle(_points[idx], r + 1, SelectedControlPointPaint);
                }
                else
                {
                    canvas.DrawCircle(_points[idx], r, ControlPointPaint);
                    canvas.DrawCircle(_points[idx], r + 1, ControlPointOutlinePaint);
                }
            }

            if (SelectedEditableIndex != null)
            {
                var p = _points[EditableIndices[SelectedEditableIndex.Value]];

                using var highlight = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 2f / zoom,
                    Color = SKColors.Yellow,
                    IsAntialias = true
                };

                canvas.DrawCircle(p, r * 1.5f, highlight);
            }
        }
    }

}
