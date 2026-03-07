using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public class River : WaterBody
    {
        public float StartWidth { get; set; }
        public float EndWidth { get; set; }
        public SKPath CenterlinePath { get; set; } = new();
    }
}
