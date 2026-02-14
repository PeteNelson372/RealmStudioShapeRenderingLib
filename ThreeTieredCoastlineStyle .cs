namespace RealmStudioShapeRenderingLib
{
    public sealed class ThreeTieredCoastlineStyle : ICoastlineStyle
    {
        private readonly CoastlineSettings _s;

        public string Name => "Three Tiered";

        public ThreeTieredCoastlineStyle(CoastlineSettings settings)
        {
            _s = settings;
        }

        public IReadOnlyList<CoastlineBand> CreateBands(Landform landform)
        {
            float step = _s.EffectDistance / 3f;

            return
            [
                CreateBand(0, step, 1f),
                CreateBand(step, step * 2, 0.6f),
                CreateBand(step * 2, step * 3, 0.3f)
            ];
        }

        private CoastlineBand CreateBand(
            float start,
            float end,
            float alphaScale)
        {
            return new CoastlineBand
            {
                StartDistance = start,
                EndDistance = end,
                Color = _s.CoastlineColor,
                Alpha = (_s.MaxAlpha / 255f) * alphaScale,
                FalloffPower = 1.2f
            };
        }
    }

}
