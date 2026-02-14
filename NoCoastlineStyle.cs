namespace RealmStudioShapeRenderingLib
{
    public sealed class NoCoastlineStyle : ICoastlineStyle
    {
        public string Name => "None";

        public IReadOnlyList<CoastlineBand> CreateBands(Landform landform)
            => Array.Empty<CoastlineBand>();
    }
}
