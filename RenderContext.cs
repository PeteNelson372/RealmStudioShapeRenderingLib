namespace RealmStudioShapeRenderingLib
{
    public class RenderContext(SymbolImageCache imageCache, EditorState state)
    {
        public EditorState State { get; private set; } = state;
        public SymbolImageCache ImageCache { get; } = imageCache;
        public float Zoom { get; set; } = 1.0f;
    }
}
