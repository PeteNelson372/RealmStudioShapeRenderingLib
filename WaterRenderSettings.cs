using RealmStudioX.WPF.EditorUtilities;
using SkiaSharp;
using System.Xml.Serialization;

namespace RealmStudioShapeRenderingLib
{
    public class WaterRenderSettings
    {
        [XmlElement]
        public string TextureId { get; set; } = string.Empty;

        [XmlElement]
        public float ShelfDepth { get; set; } = 4f;

        [XmlElement]
        public float DeepBias { get; set; } = 1.0f;

        [XmlIgnore]
        public SKColor ShorelineColor { get; set; } = SKColor.Parse("#A19076");

        [XmlElement("ShorelineColor")]
        public string ShorelineColorXml
        {
            get => XmlColorConverter.Serialize(ShorelineColor);
            set => ShorelineColor = XmlColorConverter.Deserialize(value);
        }

        [XmlIgnore]
        public SKColor DeepWaterColor { get; set; } = new SKColor(120, 180, 220, 255);

        [XmlElement("DeepWaterColor")]
        public string DeepWaterColorXml
        {
            get => XmlColorConverter.Serialize(DeepWaterColor);
            set => DeepWaterColor = XmlColorConverter.Deserialize(value);
        }

        [XmlIgnore]
        public SKColor ShallowWaterColor { get; set; } = new SKColor(30, 80, 140, 255);

        [XmlElement("ShallowWaterColor")]
        public string ShallowWaterColorXml
        {
            get => XmlColorConverter.Serialize(ShallowWaterColor);
            set => ShallowWaterColor = XmlColorConverter.Deserialize(value);
        }

        [XmlElement]
        public float ShorelineWidth { get; set; } = 2f;

        [XmlElement]
        public float ShallowDepth { get; set; } = 30f;

        [XmlElement]
        public bool LinkWaterColors { get; set; } = true;

        [XmlElement]
        public float RiverWidth { get; set; } = 16f;

        [XmlElement]
        public bool RiverSourceFadeIn { get; set; } = true;

        [XmlElement]
        public float BankFadeDepth { get; set; } = 8f;

        [XmlElement]
        public float MeanderStrength { get; set; } = 1.0f;

        [XmlIgnore]
        public SKColor[]? DepthColorLUT;

        public static WaterRenderSettings Clone(WaterRenderSettings other)
        {
            WaterRenderSettings clone = new()
            {
                TextureId = other.TextureId,
                ShelfDepth = other.ShelfDepth,
                DeepBias = other.DeepBias,
                ShorelineColor = other.ShorelineColor,
                DeepWaterColor = other.DeepWaterColor,
                ShallowWaterColor = other.ShallowWaterColor,
                ShorelineWidth = other.ShorelineWidth,
                ShallowDepth = other.ShallowDepth,
                LinkWaterColors = other.LinkWaterColors,
                RiverWidth = other.RiverWidth,
                RiverSourceFadeIn = other.RiverSourceFadeIn,
                BankFadeDepth = other.BankFadeDepth,
                MeanderStrength = other.MeanderStrength,
                DepthColorLUT = other.DepthColorLUT == null ? null : (SKColor[])other.DepthColorLUT.Clone(),
            };

            return clone;
        }
    }
}
