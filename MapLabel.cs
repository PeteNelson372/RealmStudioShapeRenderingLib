using log4net.Layout;
using RealmStudioX.WPF.EditorUtilities;
using SkiaSharp;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

namespace RealmStudioShapeRenderingLib
{
    public class MapLabel : MapComponent2D, ITransformable2D, IAlignable, IRotatable
    {
        [XmlElement]
        public string Text { get; set; } = string.Empty;

        [XmlElement]
        public SKPoint Location { get; set; }

        [XmlIgnore]
        public SKPoint BaselineLocation { get; set; } = SKPoint.Empty;

        [XmlElement]
        public float Rotation { get; set; }

        [XmlElement]
        public float Scale { get; set; } = 1f;

        [XmlElement]
        public bool Mirror { get; set; }

        [XmlElement]
        public FontStyleModel FontStyle { get; set; } = new();

        [XmlIgnore]
        public SKColor FontColor { get; set; } = SKColors.White;

        [XmlElement("FontColor")]
        public string FontColorXml
        {
            get => XmlColorConverter.Serialize(FontColor);
            set => FontColor = XmlColorConverter.Deserialize(value);
        }

        [XmlElement]
        public bool HasOutline { get; set; }

        [XmlElement]
        public float OutlineWidth { get; set; }

        [XmlIgnore]
        public SKColor OutlineColor { get; set; }

        [XmlElement("OutlineColor")]
        public string OutlineColorXml
        {
            get => XmlColorConverter.Serialize(OutlineColor);
            set => OutlineColor = XmlColorConverter.Deserialize(value);
        }

        [XmlElement]
        public bool HasGlow { get; set; }

        [XmlElement]
        public float GlowStrength { get; set; }

        [XmlIgnore]
        public SKColor GlowColor { get; set; }

        [XmlElement("GlowColor")]
        public string GlowColorXml
        {
            get => XmlColorConverter.Serialize(GlowColor);
            set => GlowColor = XmlColorConverter.Deserialize(value);
        }

        [XmlIgnore]
        public SKPath? CurvePath { get; set; }

        [XmlElement("CurveGeometry")]
        public string GeometryData
        {
            get => CurvePath != null ? CurvePath.ToSvgPathData() : string.Empty;

            set
            {
                CurvePath?.Dispose();

                if (string.IsNullOrWhiteSpace(value))
                {
                    CurvePath = new SKPath();
                    return;
                }

                CurvePath = SKPath.ParseSvgPathData(value);
            }
        }

        [XmlElement]
        public float CurvePathOffset { get; set; }

        [XmlElement]
        [DefaultValue(false)]
        public bool ReverseText { get; set; } = false;

        [XmlIgnore]
        public bool BoundsModified { get; set; } = true;

        [XmlIgnore]
        public bool IsEditing { get; set; }

        [XmlIgnore]
        public bool IsTransformTarget { get; set; } = false;

        [XmlIgnore]
        // accurate curved bounds
        public SKRect CurveBounds { get; set; }

        private FontManager? _fontManager;
        private float _startFontSize;

        private SKFont? _renderFont {  get; set; }

        [XmlElement, AllowNull]
        public SKFont? RenderFont => _renderFont;

        public override void FinalizeShapeGeometry(RealmStudioMap map)
        {
            BoundsModified = true;
        }

        // =========================
        // Rendering
        // =========================

        public override void Render(SKCanvas canvas, FontManager? fontManager, SKPath? clipPath = null)
        {
            ArgumentNullException.ThrowIfNull(fontManager);

            if (string.IsNullOrEmpty(Text))
                return;

            if (IsEditing)
                return;

            _fontManager = fontManager;

            var font = GetFont();
            if (_renderFont == null)
            {
                // keep this font instance for future rendering (do NOT dispose here)
                _renderFont = font;
                UpdateBounds(_renderFont);
            }
            else
            {
                // temporary font used only for bounds update; dispose after use
                using (font)
                {
                    UpdateBounds(font);
                }
            }

            TextRenderer.RenderLabelText(canvas, this);

            if (RenderFont != null)
            {
                UpdateBounds(RenderFont);
            }

            //canvas.DrawRect(Bounds, PaintObjects.DebugPaint);

            //canvas.DrawRect(CurveBounds, PaintObjects.DebugPaint4);
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

            BaselineLocation = new SKPoint(
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

                float wx = BaselineLocation.X + x;
                float wy = BaselineLocation.Y + y;

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
                wx - BaselineLocation.X,
                wy - BaselineLocation.Y
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

        public void MoveTo(SKPoint newLocation)
        {
            SKPoint oldLocation = Location;

            float deltaX = newLocation.X - oldLocation.X;
            float deltaY = newLocation.Y - oldLocation.Y;

            Location = newLocation;

            CurvePath?.Offset(deltaX, deltaY);
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
                CurvePathOffset = CurvePathOffset,
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
            CurvePathOffset= s.CurvePathOffset;
            BoundsModified = true;
        }
    }
}