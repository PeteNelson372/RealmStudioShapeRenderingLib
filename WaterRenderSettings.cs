using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public class WaterRenderSettings
    {
        public string TextureId { get; set; } = string.Empty;
        public float ShelfDepth { get; set; } = 4f;
        public float DeepBias { get; set; } = 1.0f;
        public SKColor ShorelineColor { get; set; } = SKColor.Parse("#A19076");
        public SKColor DeepWaterColor { get; set; } = new SKColor(120, 180, 220, 255);
        public SKColor ShallowWaterColor { get; set; } = new SKColor(30, 80, 140, 255);
        public float ShorelineWidth { get; set; } = 2f;
        public float ShallowDepth { get; set; } = 30f;
        public bool LinkWaterColors { get; set; } = true;
        public float RiverWidth { get; set; } = 16f;
        public bool RiverSourceFadeIn { get; set; } = true;
        public float BankFadeDepth { get; set; } = 8f;
        public float MeanderStrength { get; set; } = 1.0f;

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
