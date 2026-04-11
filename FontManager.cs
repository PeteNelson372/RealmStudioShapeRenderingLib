using SkiaSharp;
using System.Reflection;

namespace RealmStudioShapeRenderingLib
{
    public class FontManager
    {
        private readonly Dictionary<FontKey, SKTypeface> _typefaceCache = [];
        private readonly Dictionary<string, SKTypeface> _bundledFonts = [];
        private readonly Dictionary<string, bool> _fontValidityCache = [];

        private List<string>? _availableFonts;

        private readonly object _lock = new();
        private bool _initialized;

        private Assembly? _resourceAssmbly;

        // ---------------------------------------------------------
        // INITIALIZATION
        // ---------------------------------------------------------

        public Task InitializeAsync(Assembly resourceAssembly)
        {
            lock (_lock)
            {
                if (_initialized)
                    return Task.CompletedTask;

                _resourceAssmbly = resourceAssembly;

                _ = GetAvailableFonts();

                _initialized = true;
            }

            return Task.CompletedTask;
        }

        // ---------------------------------------------------------
        // FONT ENUMERATION
        // ---------------------------------------------------------

        public IReadOnlyList<string> GetAvailableFonts()
        {
            if (_availableFonts != null)
                return _availableFonts;

            var fontNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1. System fonts
            foreach (var family in SKFontManager.Default.FontFamilies)
            {
                fontNames.Add(family);
            }

            // 2. Bundled fonts
            foreach (var family in GetBundledFontFamilies())
            {
                fontNames.Add(family);
            }

            // 3. Filter fonts
            var filtered = new List<string>();

            foreach (var family in fontNames)
            {
                var tf = GetTypefaceSafe(family);
                if (tf == null)
                    continue;

                if (!IsTextFontCached(family, tf))
                    continue;

                filtered.Add(family);
            }

            // 4. Sort
            filtered.Sort(StringComparer.OrdinalIgnoreCase);

            _availableFonts = filtered;

            return _availableFonts;
        }

        // ---------------------------------------------------------
        // TYPEFACE ACCESS
        // ---------------------------------------------------------

        public SKTypeface? GetTypeface(FontStyleModel style)
        {
            var key = new FontKey(style.Family, style.Bold, style.Italic);

            if (_typefaceCache.TryGetValue(key, out var cached))
                return cached;

            // Bundled font fallback
            if (_bundledFonts.TryGetValue(style.Family, out var bundled))
            {
                _typefaceCache[key] = bundled;
                return bundled;
            }

            var weight = style.Bold
                ? SKFontStyleWeight.Bold
                : SKFontStyleWeight.Normal;

            var slant = style.Italic
                ? SKFontStyleSlant.Italic
                : SKFontStyleSlant.Upright;

            var tf = SKTypeface.FromFamilyName(
                style.Family,
                new SKFontStyle(weight, SKFontStyleWidth.Normal, slant));

            if (tf != null)
                _typefaceCache[key] = tf;

            return tf;
        }

        private SKTypeface? GetTypefaceSafe(string family)
        {
            try
            {
                // Default probe style (Regular)
                var style = new FontStyleModel
                {
                    Family = family,
                    Bold = false,
                    Italic = false
                };

                return GetTypeface(style);
            }
            catch
            {
                return null;
            }
        }

        // ---------------------------------------------------------
        // BUNDLED FONTS
        // ---------------------------------------------------------

        public IReadOnlyCollection<string> GetBundledFontFamilies()
        {
            LoadBundledFontsIfNeeded();
            return _bundledFonts.Keys;
        }

        private void LoadBundledFontsIfNeeded()
        {
            ArgumentNullException.ThrowIfNull(_resourceAssmbly, nameof(_resourceAssmbly));

            if (_bundledFonts.Count > 0)
                return;

            var assembly = _resourceAssmbly;

            var resourceNames = assembly.GetManifestResourceNames()
                .Where(n => n.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) ||
                            n.EndsWith(".otf", StringComparison.OrdinalIgnoreCase));

            foreach (var resourceName in resourceNames)
            {
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                    continue;

                var tf = SKTypeface.FromStream(stream);

                if (tf == null || string.IsNullOrEmpty(tf.FamilyName))
                    continue;

                // -------------------------------------------------
                // Store in bundled fonts (family → base typeface)
                // -------------------------------------------------
                _bundledFonts[tf.FamilyName] = tf;

                // -------------------------------------------------
                // Seed cache with default style (not bold/italic)
                // -------------------------------------------------
                var key = new FontKey(tf.FamilyName, bold: false, italic: false);

                _typefaceCache[key] = tf;
            }
        }

        // ---------------------------------------------------------
        // FILTERING (UPDATED FOR SKIA 3.x)
        // ---------------------------------------------------------

        private bool IsTextFontCached(string family, SKTypeface tf)
        {
            if (_fontValidityCache.TryGetValue(family, out var result))
                return result;

            result = IsTextFont(tf);
            _fontValidityCache[family] = result;

            return result;
        }

        private bool IsTextFont(SKTypeface tf)
        {
            return HasBasicLatinGlyphs(tf) &&
                   HasGoodCoverage(tf) &&
                   HasReasonableMetrics(tf);
        }

        private bool HasBasicLatinGlyphs(SKTypeface tf)
        {
            using var font = new SKFont(tf, 12);

            const string test = "AaBbCc";

            Span<ushort> glyphs = stackalloc ushort[test.Length];
            font.GetGlyphs(test.AsSpan(), glyphs);

            foreach (var g in glyphs)
            {
                if (g == 0)
                    return false;
            }

            return true;
        }

        private bool HasGoodCoverage(SKTypeface tf)
        {
            using var font = new SKFont(tf, 12);

            const string test = "The quick brown fox 123";

            Span<ushort> glyphs = stackalloc ushort[test.Length];
            font.GetGlyphs(test.AsSpan(), glyphs);

            int missing = 0;

            foreach (var g in glyphs)
            {
                if (g == 0)
                    missing++;
            }

            return missing < test.Length * 0.2;
        }

        private static bool HasReasonableMetrics(SKTypeface tf)
        {
            using var font = new SKFont(tf, 24);

            float width = font.MeasureText("Hello World");

            return width > 0 && width < 1000;
        }

        private readonly struct FontKey : IEquatable<FontKey>
        {
            public readonly string Family;
            public readonly bool Bold;
            public readonly bool Italic;

            public FontKey(string family, bool bold, bool italic)
            {
                Family = family;
                Bold = bold;
                Italic = italic;
            }

            public bool Equals(FontKey other) =>
                Family == other.Family &&
                Bold == other.Bold &&
                Italic == other.Italic;

            public override int GetHashCode() =>
                HashCode.Combine(Family, Bold, Italic);

            public override bool Equals(object? obj)
            {
                return obj is FontKey key && Equals(key);
            }
        }
    }
}