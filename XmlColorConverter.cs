using SkiaSharp;

namespace RealmStudioX.WPF.EditorUtilities
{
    public static class XmlColorConverter
    {
        public static string Serialize(SKColor color)
        {
            return $"#{color.Red:X2}{color.Green:X2}{color.Blue:X2}{color.Alpha:X2}";
        }

        public static SKColor Deserialize(string value)
        {
            return SKColor.Parse(value);
        }
    }
}
