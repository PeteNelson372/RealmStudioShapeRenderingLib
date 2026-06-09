using RealmStudioX.WPF.EditorUtilities;
using SkiaSharp;
using System.Xml.Serialization;

namespace RealmStudioShapeRenderingLib
{
    public class RegionRenderStyle
    {
        [XmlElement]
        public PathType MapPathType { get; set; } = PathType.SolidLinePath;


        // -------------------------------------------------
        // Core appearance
        // -------------------------------------------------

        [XmlElement]
        public float Width { get; set; } = 6f;

        [XmlIgnore]
        public SKColor Color { get; set; } = new SKColor(75, 49, 26, 255);

        [XmlElement("Color")]
        public string ColorXml
        {
            get => XmlColorConverter.Serialize(Color);
            set => Color = XmlColorConverter.Deserialize(value);
        }

        [XmlElement]
        public int Opacity { get; set; } = 255;

        // -------------------------------------------------
        // Border / outline
        // -------------------------------------------------

        [XmlElement]
        public bool HasBorder { get; set; } = false;

        [XmlElement]
        public float BorderWidth { get; set; } = 2f;

        [XmlIgnore]
        public SKColor BorderColor { get; set; } = new SKColor(75, 49, 26, 255);

        [XmlElement("BorderColor")]
        public string BorderColorXml
        {
            get => XmlColorConverter.Serialize(BorderColor);
            set => BorderColor = XmlColorConverter.Deserialize(value);
        }

        // -------------------------------------------------
        // Dash / pattern
        // -------------------------------------------------

        [XmlElement]
        public float[]? DashPattern { get; set; }

        [XmlElement]
        public float DashPhase { get; set; } = 0f;

        // -------------------------------------------------
        // Gradient
        // -------------------------------------------------

        [XmlElement]
        public bool UseGradient { get; set; } = false;

        [XmlIgnore]
        public SKColor GradientStart { get; set; } = SKColors.White;

        [XmlElement("GradientStart")]
        public string GradientStartXml
        {
            get => XmlColorConverter.Serialize(GradientStart);
            set => GradientStart = XmlColorConverter.Deserialize(value);
        }

        [XmlIgnore]
        public SKColor GradientEnd { get; set; } = SKColors.Black;

        [XmlElement("GradientEnd")]
        public string GradientEndXml
        {
            get => XmlColorConverter.Serialize(GradientEnd);
            set => GradientEnd = XmlColorConverter.Deserialize(value);
        }


        // -------------------------------------------------
        // One-sided rendering (cliffs, borders, etc.)
        // -------------------------------------------------

        [XmlElement]
        public bool OneSided { get; set; } = false;

        [XmlElement]
        public bool FlipSide { get; set; } = false;

        // -------------------------------------------------
        // Stroke behavior
        // -------------------------------------------------

        [XmlElement]
        public SKStrokeCap StrokeCap { get; set; } = SKStrokeCap.Round;

        [XmlElement]
        public SKStrokeJoin StrokeJoin { get; set; } = SKStrokeJoin.Round;

        [XmlElement]
        public int Smoothing { get; set; } = 0;


        // -------------------------------------------------
        // Clone (important for undo/redo safety)
        // -------------------------------------------------

        public RegionRenderStyle Clone()
        {
            return new RegionRenderStyle
            {
                MapPathType = MapPathType,

                Width = Width,
                Color = Color,
                Opacity = Opacity,

                HasBorder = HasBorder,
                BorderWidth = BorderWidth,
                BorderColor = BorderColor,

                DashPattern = DashPattern != null ? (float[])DashPattern.Clone() : null,
                DashPhase = DashPhase,

                UseGradient = UseGradient,
                GradientStart = GradientStart,
                GradientEnd = GradientEnd,

                OneSided = OneSided,
                FlipSide = FlipSide,

                StrokeCap = StrokeCap,
                StrokeJoin = StrokeJoin,
                Smoothing = Smoothing
            };
        }
    }
}

