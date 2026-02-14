using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public sealed class CoastlineBand
    {
        public float StartDistance { get; set; }   // from perimeter
        public float EndDistance { get; set; }

        public SKColor Color { get; set; } = SKColors.Transparent;
        public SKImage? TextureImage { get; set; }

        public float Alpha { get; set; } = 1f;
        public float FalloffPower { get; set; } = 1.5f;

        public CoastlineBand Clone()
        {
            return new CoastlineBand()
            {
                StartDistance = StartDistance,
                EndDistance = EndDistance,
                Color = Color,
                TextureImage = TextureImage,
                Alpha = Alpha,
                FalloffPower = FalloffPower,
            };
        }
    }
}
