namespace RealmStudioShapeRenderingLib
{
    public class RenderContext(SymbolImageCache imageCache)
    {
        public SymbolImageCache ImageCache { get; } = imageCache;
        public float Zoom { get; set; } = 1.0f;
    }
}
