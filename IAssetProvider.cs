using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public interface IAssetProvider
    {
        /// <summary>
        /// Returns an image asset by logical id, or null if not found.
        /// </summary>
        SKImage? GetImage(string assetId);
    }
}
