namespace RealmStudioShapeRenderingLib
{
    public class OceanShorelineSettings
    {
        public float ShoreDepth { get; set; } = 120f; // pixels outward

        public byte MaxAlpha { get; set; } = 120;

        public bool EnableFoam { get; set; } = true;

        public byte FoamAlpha { get; set; } = 160;
    }
}
