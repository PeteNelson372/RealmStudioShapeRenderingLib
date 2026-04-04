using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public abstract class SymbolImageResource
    {
        public abstract SKRect Bounds { get; }
    }

    public class BitmapResource(SKImage image) : SymbolImageResource
    {
        public SKImage Image { get; } = image;
        public override SKRect Bounds
        {
            get
            {
                return new(0, 0, Image.Width, Image.Height);
            }
        }
    }

    public class SvgResource(SKImage image, SKRect bounds) : SymbolImageResource
    {
        private SKImage? _cachedImage;
        private float _cachedScale;

        const int MaxDim = 2048;

        public SKImage Image { get; } = image;
        public override SKRect Bounds
        {
            get
            {
                return bounds;
            }
        }

        public SKImage GetImage(float scale)
        {
            if (_cachedImage != null && Math.Abs(_cachedScale - scale) < 0.02f)
            {
                return _cachedImage;
            }

            _cachedImage?.Dispose();

            var rect = bounds;

            int width = Math.Max(1, (int)(rect.Width * scale));
            int height = Math.Max(1, (int)(rect.Height * scale));

            width = Math.Min(width, MaxDim);
            height = Math.Min(height, MaxDim);

            SKImageInfo info = new(width, height, SKColorType.Bgra8888);

            // Create a bitmap to receive scaled pixels
            using var bitmap = new SKBitmap(info);

            var sampling = new SKSamplingOptions(SKFilterMode.Linear);

            // ScalePixels takes SKPixmap and SKSamplingOptions
            if (!Image.ScalePixels(bitmap.PeekPixels(), sampling))
            {
                // fallback: create an empty image if scaling fails
                _cachedImage = SKImage.Create(info);
            }
            else
            {
                _cachedImage = SKImage.FromBitmap(bitmap);
            }

            _cachedScale = scale;
            return _cachedImage;
        }
    }
}
