namespace RealmStudioShapeRenderingLib
{
    public sealed class UniformBlendCoastlineStyle : ICoastlineStyle
    {
        private readonly CoastlineSettings _s;

        public string Name => "Uniform Blend";

        public UniformBlendCoastlineStyle(CoastlineSettings settings)
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
                    FalloffPower = 2.2f
                }
            ];
        }
    }

}
