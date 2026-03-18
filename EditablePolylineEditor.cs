using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public class EditablePolylineEditor
    {
        public List<SKPoint> _points;

        public Action? OnChanged;

        protected List<int> EditableIndices { get; } = new();

        public int? SelectedEditableIndex { get; set; }

        public bool IsEditing { get; set; }

        private void NotifyChanged()
        {
            OnChanged?.Invoke();
        }

        public bool IsDrawing { get; private set; }

        private SKPoint? _lastPoint;

        public EditablePolylineEditor(List<SKPoint> points)
        {
            _points = points;
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
            if (!IsDrawing || _lastPoint == null)
                return;

            var constrained = ApplyConstraints(_lastPoint.Value, worldPos, ctrl, shift);

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

        private SKPoint ApplyConstraints(SKPoint origin, SKPoint p, bool ctrl, bool shift)
        {
            var dx = p.X - origin.X;
            var dy = p.Y - origin.Y;

            if (!ctrl && !shift)
                return p;

            float angle = MathF.Atan2(dy, dx);
            float length = MathF.Sqrt(dx * dx + dy * dy);

            if (ctrl)
            {
                // Axis lock (0° or 90°)
                if (MathF.Abs(dx) > MathF.Abs(dy))
                    return new SKPoint(origin.X + dx, origin.Y);
                else
                    return new SKPoint(origin.X, origin.Y + dy);
            }

            if (shift)
            {
                // Snap to 5° increments
                const float step = MathF.PI / 36f; // 5 degrees

                float snapped = MathF.Round(angle / step) * step;

                return new SKPoint(
                    origin.X + MathF.Cos(snapped) * length,
                    origin.Y + MathF.Sin(snapped) * length);
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
                EditableIndices.Insert(0, 0);

            int last = _points.Count - 1;
            if (!EditableIndices.Contains(last))
                EditableIndices.Add(last);
        }

        // ----------------------------
        // Hit testing
        // ----------------------------

        public int HitTestEditable(SKPoint worldPos, float radius)
        {
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
        // Editing (with falloff)
        // ----------------------------

        public void MoveEditable(int editableIndex, SKPoint newPos, int influence = 9)
        {
            int centerIdx = EditableIndices[editableIndex];
            var oldPos = _points[centerIdx];

            var delta = new SKPoint(newPos.X - oldPos.X, newPos.Y - oldPos.Y);

            for (int i = -influence; i <= influence; i++)
            {
                int idx = centerIdx + i;

                if (idx < 0 || idx >= _points.Count)
                    continue;

                float t = 1f - (Math.Abs(i) / (float)(influence + 1));

                t = t * t; // quadratic falloff

                _points[idx] = new SKPoint(
                    _points[idx].X + delta.X * t,
                    _points[idx].Y + delta.Y * t);
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

        public void RenderEditableHandles(SKCanvas canvas, float zoom)
        {
            if (!IsEditing)
                return;

            using var paint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = SKColors.Orange,
                IsAntialias = true
            };

            float r = 4f / zoom;

            foreach (var idx in EditableIndices)
            {
                canvas.DrawCircle(_points[idx], r, paint);
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
