namespace RealmStudioShapeRenderingLib
{
    public sealed class UniformBandCoastlineStyle : ICoastlineStyle
    {
        private readonly CoastlineSettings _s;

        public string Name => "Uniform Band";

        public UniformBandCoastlineStyle(CoastlineSettings settings)
        {
            _s = settings;
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
                    FalloffPower = 0.01f // almost flat
                }
            ];
        }
    }
}
