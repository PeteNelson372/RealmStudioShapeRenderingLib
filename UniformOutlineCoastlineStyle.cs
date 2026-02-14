namespace RealmStudioShapeRenderingLib
{
    public sealed class UniformOutlineCoastlineStyle : ICoastlineStyle
    {
        private readonly CoastlineSettings _s;

        public string Name => "Uniform Outline";

        public UniformOutlineCoastlineStyle(CoastlineSettings settings)
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
                    EndDistance = 6f,
                    Color = _s.CoastlineColor,
                    Alpha = 1f,
                    FalloffPower = 0.01f
                }
            ];
        }
    }

}
