namespace RealmStudioShapeRenderingLib
{
    public interface ICoastlineStyle
    {
        /// <summary>
        /// A human-readable name for debugging, logging, or UI previews.
        /// (The enum remains the authoritative identifier.)
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Generates the coastline bands used by the renderer.
        /// This method must be pure and side-effect free.
        /// </summary>
        IReadOnlyList<CoastlineBand> CreateBands(Landform landform);
    }

}
