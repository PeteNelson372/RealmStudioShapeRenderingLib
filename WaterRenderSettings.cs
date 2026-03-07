using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public class WaterRenderSettings
    {
        public string TextureId { get; set; } = string.Empty;

        public float ShelfDepth { get; set; } = 6f;
        public float DeepBias { get; set; } = 1.0f;
        public SKColor ShorelineColor { get; set; } = SKColor.Parse("#A19076");
        public SKColor DeepWaterColor { get; set; } = new SKColor(120, 180, 220, 255);
        public SKColor ShallowWaterColor { get; set; } = new SKColor(30, 80, 140, 255);
        public float ShorelineWidth { get; set; } = 2f;
        public float ShallowDepth { get; set; } = 80f;
        public bool LinkWaterColors { get; set; } = true;
    }
}
