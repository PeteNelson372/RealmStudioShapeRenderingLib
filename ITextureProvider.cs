using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public interface ITextureProvider
    {
        SKImage GetTexture(string textureId);
    }
}
