using SkiaSharp;
using Svg.Skia;

namespace RealmStudioShapeRenderingLib
{
    public class SymbolImageCache
    {
        private readonly Dictionary<string, SymbolImageResource> _cache = [];

        public SymbolImageResource? Get(string path)
        {
            path = Utilities.NormalizePath(path);

            if (_cache.TryGetValue(path, out var resource))
                return resource;

            if (!File.Exists(path))
                return null;

            var ext = Path.GetExtension(path).ToLowerInvariant();

            SymbolImageResource? loaded = ext switch
            {
                ".png" or ".jpg" or ".jpeg" or ".bmp" => LoadBitmap(path),
                ".svg" => LoadSvg(path),
                _ => null
            };

            if (loaded != null)
            {
                _cache[path] = loaded;
            }

            return loaded;
        }

        private static BitmapResource? LoadBitmap(string path)
        {
            var img = SKImage.FromBitmap(SKBitmap.Decode(path));

            return img != null ? new BitmapResource(img) : null;
        }

        private static SvgResource? LoadSvg(string path)
        {
            var svg = new SKSvg();

            if (!File.Exists(path))
            {
                return null;
            }

            svg.Load(path);

            if (svg.Picture == null)
            {
                return null;
            }

            SKImage image = SKImage.FromPicture(svg.Picture, new SKSizeI((int)svg.Picture.CullRect.Width, (int)svg.Picture.CullRect.Height));

            return new SvgResource(image, svg.Picture.CullRect);
        }
    }
}
