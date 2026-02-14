using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public sealed class AssetProvider(ITextureProvider textures) : IAssetProvider
    {
        private readonly ITextureProvider _textures = textures;

        public SKImage? GetImage(string assetId)
        {
            try
            {
                return _textures.GetTexture(assetId);
            }
            catch
            {
                return null;
            }
        }
    }

}
