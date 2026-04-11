using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public class MapLabel : MapComponent2D, ITransformable2D
    {
        public string Text { get; set; } = string.Empty;

        public SKPoint Location { get; set; }

        private SKPoint _baselineLocation;

        public float Rotation { get; set; }
        public float Scale { get; set; } = 1f;
        public bool Mirror { get; set; }

        public FontStyleModel FontStyle { get; set; } = new();

        public SKColor FontColor { get; set; } = SKColors.White;

        public bool HasOutline { get; set; }
        public float OutlineWidth { get; set; }
        public SKColor OutlineColor { get; set; }

        public bool HasGlow { get; set; }
        public float GlowStrength { get; set; }
        public SKColor GlowColor { get; set; }

        public SKPath? CurvePath { get; set; }

        public bool BoundsModified { get; set; } = true;
        public bool IsEditing { get; set; }

        // accurate curved bounds
        public SKRect CurveBounds { get; private set; }

        private FontManager? _fontManager;
        private float _startFontSize;

        private SKFont? _renderFont {  get; set; }
        public SKFont? RenderFont => _renderFont;

        // =========================
        // Rendering
        // =========================

        public override void Render(SKCanvas canvas, FontManager? fontManager)
        {
            ArgumentNullException.ThrowIfNull(fontManager);

            if (string.IsNullOrEmpty(Text))
                return;

            _fontManager ??= fontManager;

            using var font = GetFont();
            _renderFont ??= font;

            UpdateBounds(font);

            if (IsEditing)
                return;

            canvas.Save();

            // Rotate around anchor (Location)
            if (Math.Abs(Rotation) > 0.001f)
            {
                canvas.Translate(Location.X, Location.Y);
                canvas.RotateDegrees(Rotation);
                canvas.Translate(-Location.X, -Location.Y);
            }

            using var fillPaint = new SKPaint
            {
                Color = FontColor,
                IsAntialias = true
            };

            if (CurvePath != null)
            {
                using var measure = new SKPathMeasure(CurvePath, false);

                float pathLength = measure.Length;
                float textWidth = font.MeasureText(Text);
                //float hOffset = (pathLength - textWidth) * 0.5f;

                DrawOnPath(canvas, font, fillPaint);

                // -------------------------------------------------
                // Accurate CurveBounds via SKTextBlob
                // -------------------------------------------------
                using var blob = SKTextBlob.CreatePathPositioned(
                    Text,
                    font,
                    CurvePath,
                    SKTextAlign.Center,
                    new SKPoint(0, 0)
                );

                CurveBounds = blob != null ? blob.Bounds : Bounds;
            }
            else
            {
                DrawStraight(canvas, font, fillPaint);
                CurveBounds = Bounds;
            }

            canvas.Restore();
        }

        private void DrawStraight(SKCanvas canvas, SKFont font, SKPaint fillPaint)
        {
            float x = _baselineLocation.X;
            float y = _baselineLocation.Y;

            if (GlowStrength > 0)
            {
                using var glow = new SKPaint
                {
                    Color = GlowColor,
                    IsAntialias = true,
                    MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, GlowStrength)
                };
                canvas.DrawText(Text, x, y, font, glow);
            }

            if (OutlineWidth > 0)
            {
                using var stroke = new SKPaint
                {
                    Color = OutlineColor,
                    IsAntialias = true,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = OutlineWidth,
                    StrokeJoin = SKStrokeJoin.Round
                };
                canvas.DrawText(Text, x, y, font, stroke);
            }

            canvas.DrawText(Text, x, y, font, fillPaint);
        }

        private void DrawOnPath(SKCanvas canvas, SKFont font, SKPaint fillPaint)
        {
            if (GlowStrength > 0)
            {
                using var glow = new SKPaint
                {
                    Color = GlowColor,
                    IsAntialias = true,
                    MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, GlowStrength)
                };
                canvas.DrawTextOnPath(Text, CurvePath, new SKPoint(0, 0),
                    false, SKTextAlign.Center, font, glow);
            }

            if (OutlineWidth > 0)
            {
                using var stroke = new SKPaint
                {
                    Color = OutlineColor,
                    IsAntialias = true,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = OutlineWidth,
                    StrokeJoin = SKStrokeJoin.Round
                };
                canvas.DrawTextOnPath(Text, CurvePath, new SKPoint(0, 0),
                    false, SKTextAlign.Center, font, stroke);
            }

            canvas.DrawTextOnPath(Text, CurvePath, new SKPoint(0, 0),
                false, SKTextAlign.Center, font, fillPaint);
        }

        // =========================
        // Bounds
        // =========================

        private void UpdateBounds(SKFont font)
        {
            if (!BoundsModified) return;

            font.MeasureText(Text, out SKRect bounds);

            float inflateX = MathF.Max(5, bounds.Width * 0.01f);
            float inflateY = MathF.Max(5, bounds.Height * 0.01f);

            bounds.Inflate(inflateX, inflateY);

            LocalBounds = bounds;

            float cx = (bounds.Left + bounds.Right) * 0.5f;
            float cy = (bounds.Top + bounds.Bottom) * 0.5f;

            _baselineLocation = new SKPoint(
                Location.X - cx,
                Location.Y - cy
            );

            var corners = GetTransformedCorners();

            Bounds = SKRect.Create(
                corners.Min(p => p.X),
                corners.Min(p => p.Y),
                corners.Max(p => p.X) - corners.Min(p => p.X),
                corners.Max(p => p.Y) - corners.Min(p => p.Y)
            );

            BoundsModified = false;
        }

        private SKPoint GetCenter() => Location;

        public SKPoint[] GetTransformedCorners()
        {
            var r = LocalBounds;

            float sx = Mirror ? -Scale : Scale;
            float sy = Scale;

            float rad = Rotation * MathF.PI / 180f;
            float cos = MathF.Cos(rad);
            float sin = MathF.Sin(rad);

            SKPoint Transform(float x, float y)
            {
                x *= sx;
                y *= sy;

                float wx = _baselineLocation.X + x;
                float wy = _baselineLocation.Y + y;

                float dx = wx - Location.X;
                float dy = wy - Location.Y;

                float rx = dx * cos - dy * sin;
                float ry = dx * sin + dy * cos;

                return new SKPoint(Location.X + rx, Location.Y + ry);
            }

            return
            [
                Transform(r.Left, r.Top),
                Transform(r.Right, r.Top),
                Transform(r.Right, r.Bottom),
                Transform(r.Left, r.Bottom)
            ];
        }

        public SKRect GetLocalBounds()
        {
            return LocalBounds;
        }

        // =========================
        // Editing
        // =========================

        public int GetCaretIndex(SKPoint worldPoint, SKFont font)
        {
            var local = WorldToLocal(worldPoint);

            if (local.Y < LocalBounds.Top || local.Y > LocalBounds.Bottom)
                return -1;

            float x = local.X - LocalBounds.Left;

            if (x <= 0)
                return 0;

            int length = Text.Length;
            float prevWidth = 0f;

            for (int i = 1; i <= length; i++)
            {
                float currWidth = font.MeasureText(Text[..i]);
                float mid = (prevWidth + currWidth) * 0.5f;

                if (x < mid) return i - 1;
                if (x < currWidth) return i;

                prevWidth = currWidth;
            }

            return length;
        }

        public SKPoint WorldToLocal(SKPoint world)
        {
            float rad = -Rotation * MathF.PI / 180f;
            float cos = MathF.Cos(rad);
            float sin = MathF.Sin(rad);

            var center = Location;

            float dx = world.X - center.X;
            float dy = world.Y - center.Y;

            float rx = dx * cos - dy * sin;
            float ry = dx * sin + dy * cos;

            float wx = center.X + rx;
            float wy = center.Y + ry;

            return new SKPoint(
                wx - _baselineLocation.X,
                wy - _baselineLocation.Y
            );
        }

        // =========================
        // Transform
        // =========================

        public void BeginScale()
        {
            _startFontSize = FontStyle.Size;
        }

        public void ApplyScale(float factor)
        {
            FontStyle.Size = _startFontSize * factor;
            BoundsModified = true;
        }

        // =========================
        // Hit Testing
        // =========================

        public override bool HitTest(SKPoint worldPos)
        {
            if (BoundsModified)
                UpdateBounds(GetFont());

            return CurvePath != null
                ? CurveBounds.Contains(worldPos)
                : Bounds.Contains(worldPos);
        }

        // =========================
        // Font
        // =========================

        public SKFont GetFont()
        {
            var tf = _fontManager!.GetTypeface(FontStyle);
            return new SKFont(tf, FontStyle.Size);
        }

        // =========================
        // State
        // =========================

        public override IShapeState CaptureState()
        {
            return new MapLabelState()
            {
                Text = Text,
                Location = Location,
                Rotation = Rotation,
                Scale = Scale,
                Mirror = Mirror,
                FontStyle = FontStyle.Clone(),
                FontColor = FontColor,
                HasOutline = HasOutline,
                OutlineWidth = OutlineWidth,
                OutlineColor = OutlineColor,
                HasGlow = HasGlow,
                GlowStrength = GlowStrength,
                GlowColor = GlowColor,
                CurvePath = CurvePath,
            };
        }

        public override void RestoreState(IShapeState state)
        {
            if (state is not MapLabelState s)
                return;

            Text = s.Text;
            Location = s.Location;
            Rotation = s.Rotation;
            Scale = s.Scale;
            Mirror = s.Mirror;
            FontStyle = s.FontStyle.Clone();
            FontColor = s.FontColor;
            HasOutline = s.HasOutline;
            OutlineWidth = s.OutlineWidth;
            OutlineColor = s.OutlineColor;
            HasGlow = s.HasGlow;
            GlowStrength = s.GlowStrength;
            GlowColor = s.GlowColor;
            CurvePath = s.CurvePath;

            BoundsModified = true;
        }
    }
}