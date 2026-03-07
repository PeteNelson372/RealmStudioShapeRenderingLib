namespace RealmStudioShapeRenderingLib
{
    public class WaterSystem
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public List<WaterBody> WaterBodies { get; set; } = [];
    }
}
