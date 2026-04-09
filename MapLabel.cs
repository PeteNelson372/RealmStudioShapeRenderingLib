using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public class MapLabel : MapComponent2D, ITransformable2D
    {
        public string Text { get; set; } = string.Empty;

        public SKPoint Location { get; set; }
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

        public bool IsEditing { get; set; } = false;

        private FontManager? _fontManager = null;
        private float _startFontSize;

        // =========================
        // Rendering
        // =========================

        public override void Render(SKCanvas canvas, FontManager? fontManager)
        {
            ArgumentNullException.ThrowIfNull(nameof(fontManager));

            _fontManager ??= fontManager;

            if (IsEditing)
            {
                return;
            }

            if (string.IsNullOrEmpty(Text))
            {
                return;
            }

            using var font = GetFont();

            UpdateBounds(font);

            using var fillPaint = new SKPaint
            {
                Color = FontColor,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            float x = Location.X;
            float y = Location.Y;

            var center = GetCenter();

            canvas.Save();

            // Rotate around CENTER (FIXED)
            if (Math.Abs(Rotation) > 0.001f)
            {
                canvas.Translate(center.X, center.Y);
                canvas.RotateDegrees(Rotation);
                canvas.Translate(-center.X, -center.Y);
            }

            // Glow
            if (GlowStrength > 0)
            {
                using var glowPaint = new SKPaint
                {
                    Color = GlowColor,
                    IsAntialias = true,
                    Style = SKPaintStyle.Fill,
                    MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, GlowStrength)
                };

                canvas.DrawText(Text, x, y, font, glowPaint);
            }

            // Outline
            if (OutlineWidth > 0)
            {
                using var strokePaint = new SKPaint
                {
                    Color = OutlineColor,
                    IsAntialias = true,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = OutlineWidth,
                    StrokeJoin = SKStrokeJoin.Round
                };

                canvas.DrawText(Text, x, y, font, strokePaint);
            }

            // Fill
            canvas.DrawText(Text, x, y, font, fillPaint);

            // Debug bounds
            //canvas.DrawRect(Bounds, PaintObjects.DebugPaint2);

            //var cx = Bounds.MidX;
            //var cy = Bounds.MidY;
            //canvas.DrawCircle(cx, cy, 3, PaintObjects.DebugPaint3);

            canvas.Restore();
        }

        // =========================
        // Label Editing
        // =========================
        public int GetCaretIndex(SKPoint worldPoint, SKFont font)
        {
            var local = WorldToLocal(worldPoint);

            if (local.Y < LocalBounds.Top || local.Y > LocalBounds.Bottom)
            {
                return -1;
            }

            // Convert to text-relative X (0 = left edge of text)
            float x = local.X - LocalBounds.Left;

            // Clamp far left
            if (x <= 0)
            {
                return 0;
            }

            int length = Text.Length;

            // Walk through character positions
            float prevWidth = 0f;

            for (int i = 1; i <= length; i++)
            {
                float currWidth = font.MeasureText(Text[..i]);

                // Midpoint between previous and current character edge
                float mid = (prevWidth + currWidth) * 0.5f;

                if (x < mid)
                {
                    return i - 1;
                }

                if (x < currWidth)
                {
                    return i;
                }

                prevWidth = currWidth;
            }

            // Clamp far right
            return length;
        }


        // =========================
        // Bounds + Center
        // =========================

        private SKPoint GetCenter()
        {
            return new SKPoint(
                Location.X + (LocalBounds.Left + LocalBounds.Right) * 0.5f,
                Location.Y + (LocalBounds.Top + LocalBounds.Bottom) * 0.5f
            );
        }

        private void UpdateBounds(SKFont font)
        {
            if (!BoundsModified) return;

            font.MeasureText(Text, out SKRect bounds);

            float horzInflate = MathF.Max(5, bounds.Width * 0.01f);
            float vertInflate = MathF.Max(5, bounds.Height * 0.01f);

            bounds.Inflate(new SKSize(horzInflate, vertInflate));

            LocalBounds = bounds;

            // Compute transformed corners
            var corners = GetTransformedCorners();

            float minX = corners.Min(p => p.X);
            float minY = corners.Min(p => p.Y);
            float maxX = corners.Max(p => p.X);
            float maxY = corners.Max(p => p.Y);

            SKRect boundsRect = new(minX, minY, maxX, maxY);

            Bounds = boundsRect;

            BoundsModified = false;
        }

        public SKRect GetLocalBounds()
        {
            return LocalBounds;
        }

        public SKPoint WorldToLocal(SKPoint world)
        {
            float rad = -Rotation * MathF.PI / 180f;
            float cos = MathF.Cos(rad);
            float sin = MathF.Sin(rad);

            var center = new SKPoint(
                Location.X + (LocalBounds.Left + LocalBounds.Right) * 0.5f,
                Location.Y + (LocalBounds.Top + LocalBounds.Bottom) * 0.5f
            );

            // translate to origin (center)
            float dx = world.X - center.X;
            float dy = world.Y - center.Y;

            // inverse rotate
            float rx = dx * cos - dy * sin;
            float ry = dx * sin + dy * cos;

            // translate back
            float wx = center.X + rx;
            float wy = center.Y + ry;

            // convert to local (baseline-relative)
            return new SKPoint(
                wx - Location.X,
                wy - Location.Y
            );
        }

        // =========================
        // Geometry
        // =========================

        public SKPoint[] GetTransformedCorners()
        {
            var r = LocalBounds;

            float sx = Mirror ? -Scale : Scale;
            float sy = Scale;

            float rad = Rotation * MathF.PI / 180f;
            float cos = MathF.Cos(rad);
            float sin = MathF.Sin(rad);

            var center = GetCenter();

            SKPoint Transform(float x, float y)
            {
                // scale in local space
                x *= sx;
                y *= sy;

                // move to world
                float wx = Location.X + x;
                float wy = Location.Y + y;

                // rotate around center
                float dx = wx - center.X;
                float dy = wy - center.Y;

                float rx = dx * cos - dy * sin;
                float ry = dx * sin + dy * cos;

                return new SKPoint(center.X + rx, center.Y + ry);
            }

            return
            [
                Transform(r.Left,  r.Top),
                Transform(r.Right, r.Top),
                Transform(r.Right, r.Bottom),
                Transform(r.Left,  r.Bottom)
            ];
        }

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
            {
                var font = GetFont();
                UpdateBounds(font);
            }

            return Bounds.Contains(worldPos);
        }

        // =========================
        // Font
        // =========================

        private SKFont GetFont()
        {
            ArgumentNullException.ThrowIfNull(nameof(_fontManager));

            var typeface = _fontManager!.GetTypeface(FontStyle);
            return new SKFont(typeface, FontStyle.Size);
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