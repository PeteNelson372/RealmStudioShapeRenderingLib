using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public sealed class HatchCoastlineStyle : ICoastlineStyle
    {
        private readonly CoastlineSettings _s;
        private readonly SKImage? _texture;

        public string Name => "Hatch Pattern";

        public HatchCoastlineStyle(
            CoastlineSettings settings,
            SKImage? texture)
        {
            _s = settings;
            _texture = texture;
        }

        public IReadOnlyList<CoastlineBand> CreateBands(Landform landform)
        {
            return
            [
                new CoastlineBand
                {
                    StartDistance = 0,
                    EndDistance = _s.EffectDistance,
                    Color = _s.CoastlineColor,
                    Alpha = _s.MaxAlpha / 255f,
                    TextureImage = _texture,
                    FalloffPower = 2.0f
                }
            ];
        }
    }
}
