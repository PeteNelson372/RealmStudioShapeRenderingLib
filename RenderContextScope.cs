namespace RealmStudioShapeRenderingLib
{
    public static class RenderContextScope
    {
        [ThreadStatic]
        private static RenderContext? _current;

        public static RenderContext Current =>
            _current ?? throw new InvalidOperationException("No SymbolRenderContext set.");

        public static IDisposable Begin(RenderContext context)
        {
            _current = context;
            return new Scope();
        }

        private class Scope : IDisposable
        {
            public void Dispose()
            {
                _current = null;
            }
        }
    }
}
