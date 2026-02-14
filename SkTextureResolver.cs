using SkiaSharp;
using System.Collections.Generic;

namespace RealmStudioShapeRenderingLib
{
    public class SkTextureResolver : ITextureResolver
    {
        private readonly Dictionary<string, SKShader> _cache = [];

        public SKShader GetShader(string texturePath)
        {
            if (_cache.TryGetValue(texturePath, out var shader))
                return shader;

            using var bitmap = SKBitmap.Decode(texturePath);

            shader = bitmap.ToShader(
                SKShaderTileMode.Repeat,
                SKShaderTileMode.Repeat);

            _cache[texturePath] = shader;
            return shader;
        }
    }
}
