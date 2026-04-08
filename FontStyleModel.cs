namespace RealmStudioShapeRenderingLib
{
    public class FontStyleModel : IEquatable<FontStyleModel>
    {
        public string Family { get; set; } = "Arial";

        public float Size { get; set; } = 24f;

        public bool Bold { get; set; }

        public bool Italic { get; set; }

        public FontDecorations Decorations { get; set; }

        public FontStyleModel Clone()
        {
            return (FontStyleModel)MemberwiseClone();
        }

        public bool Equals(FontStyleModel? other)
        {
            return other != null
                && Family == other.Family
                && Size == other.Size
                && Bold == other.Bold
                && Italic == other.Italic
                && Decorations == other.Decorations;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as FontStyleModel);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Family, Size, Bold, Italic, Decorations);
        }
    }

    [Flags]
    public enum FontDecorations
    {
        None = 0,
        Underline = 1 << 0,
        Strikeout = 1 << 1,
        Superscript = 1 << 2,
        Subscript = 1 << 3,
    }
}
