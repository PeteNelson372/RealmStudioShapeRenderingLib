namespace RealmStudioShapeRenderingLib
{
    public class FontSelectionModel
    {
        public FontStyleModel FontStyle { get; set; } = new();

        public event Action? FontSelectionChanged;

        public void Update(Func<FontStyleModel, FontStyleModel> update)
        {
            var newValue = update(FontStyle);

            if (!EqualityComparer<FontStyleModel>.Default.Equals(FontStyle, newValue))
            {
                FontStyle = newValue;
                FontSelectionChanged?.Invoke();
            }
        }
    }
}
