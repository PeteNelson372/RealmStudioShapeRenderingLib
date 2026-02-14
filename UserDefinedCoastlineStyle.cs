namespace RealmStudioShapeRenderingLib
{
    public sealed class UserDefinedCoastlineStyle : ICoastlineStyle
    {
        private readonly CoastlineSettings _s;

        public string Name => "User Defined";

        public UserDefinedCoastlineStyle(CoastlineSettings settings)
        {
            _s = settings;
        }

        public IReadOnlyList<CoastlineBand> CreateBands(Landform landform)
        {
            return _s.UserBands != null ? _s.UserBands : Array.Empty<CoastlineBand>();
        }
    }

}
