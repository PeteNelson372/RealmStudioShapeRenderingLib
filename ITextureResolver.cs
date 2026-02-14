using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public interface ITextureResolver
    {
        SKShader GetShader(string texturePath);
    }
}
