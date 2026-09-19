/**************************************************************************************************************************
* Copyright 2026, Peter R. Nelson
*
* This file is part of the RealmStudioX application. The RealmStudioX application is intended
* for creating fantasy maps for gaming and world building.
*
* RealmStudio is free software: you can redistribute it and/or modify it under the terms
* of the GNU General Public License as published by the Free Software Foundation,
* either version 3 of the License, or (at your option) any later version.
*
* This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
* without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
* See the GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License along with this program.
* The text of the GNU General Public License (GPL) is found in the LICENSE.txt file.
* If the LICENSE.txt file is not present or the text of the GNU GPL is not present in the LICENSE.txt file,
* see https://www.gnu.org/licenses/.
*
* For questions about the RealmStudioX application or about licensing, please email
* support@brookmonte.com
*
***************************************************************************************************************************/
using RealmStudioShapeRenderingLib.Logging;
using RealmStudioX.WPF.EditorUtilities;
using SkiaSharp;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace RealmStudioShapeRenderingLib
{
    public class MapHeightMap : MapComponent2D, IXmlSerializable
    {
        public float MinimumElevation { get; set; }
        public float MaximumElevation { get; set; }
        public string ElevationUnit { get; set; } = string.Empty;

        [XmlIgnore]
        public float[,]? HeightMap { get; private set; }

        [XmlIgnore]
        public HypsometricPalette? HeightMapPalette { get; set; }

        private const int HypsometricLookupSize = 4096;

        private SKColor[]? _hypsometricColorLookup;

        private float _lookupMinimumElevation;
        private float _lookupMaximumElevation;
        private HypsometricPalette? _lookupPalette;

        private Dictionary<int, SKPath>? _contourPaths;

        private float _contourInterval;
        private int _majorContourInterval;

        public XmlSchema? GetSchema()
        {
            return null;
        }

        public void WriteXml(XmlWriter writer)
        {
            if (HeightMap == null)
            {
                return;
            }

            int width = HeightMap.GetLength(0);
            int height = HeightMap.GetLength(1);

            writer.WriteAttributeString(
                "Width",
                width.ToString(CultureInfo.InvariantCulture));

            writer.WriteAttributeString(
                "Height",
                height.ToString(CultureInfo.InvariantCulture));

            writer.WriteAttributeString(
                "MinimumElevation",
                MinimumElevation.ToString(CultureInfo.InvariantCulture));

            writer.WriteAttributeString(
                "MaximumElevation",
                MaximumElevation.ToString(CultureInfo.InvariantCulture));

            writer.WriteAttributeString(
                "ElevationUnit",
                ElevationUnit);

            // Serialize the hypsometric palette used by this height map.
            if (HeightMapPalette != null)
            {
                writer.WriteStartElement("HypsometricPalette");

                writer.WriteAttributeString(
                    "Id",
                    HeightMapPalette.Id);

                writer.WriteAttributeString(
                    "Name",
                    HeightMapPalette.Name);

                writer.WriteAttributeString(
                    "IsLocked",
                    HeightMapPalette.IsLocked.ToString(
                        CultureInfo.InvariantCulture));

                foreach (HypsometricTint tint in HeightMapPalette.Tints)
                {
                    writer.WriteStartElement("Tint");

                    writer.WriteAttributeString(
                        "Id",
                        tint.Id);

                    writer.WriteAttributeString(
                        "NormalizedHeight",
                        tint.NormalizedHeight.ToString(
                            CultureInfo.InvariantCulture));

                    writer.WriteAttributeString(
                        "Color",
                        tint.ColorXml);

                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }

            /*
             * Serialize the actual floating-point elevation values.
             *
             * Each float is represented by its IEEE-754 32-bit value.
             * Four bytes are stored for every height value.
             */
            int valueCount = checked(width * height);
            int byteCount = checked(valueCount * sizeof(float));

            byte[] data = new byte[byteCount];

            int byteIndex = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int bits = BitConverter.SingleToInt32Bits(
                        HeightMap[x, y]);

                    data[byteIndex++] = (byte)bits;
                    data[byteIndex++] = (byte)(bits >> 8);
                    data[byteIndex++] = (byte)(bits >> 16);
                    data[byteIndex++] = (byte)(bits >> 24);
                }
            }

            writer.WriteStartElement("Data");

            writer.WriteString(
                Convert.ToBase64String(data));

            writer.WriteEndElement();
        }

        public void ReadXml(XmlReader reader)
        {
            try
            {
                string? widthString =
                    reader.GetAttribute("Width");

                string? heightString =
                    reader.GetAttribute("Height");

                string? minimumElevationString =
                    reader.GetAttribute("MinimumHeight");

                string? maximumElevationString =
                    reader.GetAttribute("MaximumHeight");

                minimumElevationString =
                    reader.GetAttribute("MinimumElevation");

                maximumElevationString =
                    reader.GetAttribute("MaximumElevation");

                string? elevationUnit =
                    reader.GetAttribute("HeightUnit");

                elevationUnit =
                    reader.GetAttribute("ElevationUnit");

                if (!int.TryParse(
                        widthString,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out int width) ||
                    !int.TryParse(
                        heightString,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out int height))
                {
                    RealmStudioXLogger.Error(
                        $"Unable to load height map: invalid or missing dimensions. " +
                        $"Width='{widthString}', Height='{heightString}'.");

                    ClearHeightMap();
                    reader.Skip();

                    return;
                }

                if (width <= 0 || height <= 0)
                {
                    RealmStudioXLogger.Error(
                        $"Unable to load height map: invalid dimensions " +
                        $"{width} x {height}.");

                    ClearHeightMap();
                    reader.Skip();

                    return;
                }

                long valueCount64 =
                    (long)width * height;

                if (valueCount64 > int.MaxValue)
                {
                    RealmStudioXLogger.Error(
                        $"Unable to load height map: dimensions " +
                        $"{width} x {height} are too large.");

                    ClearHeightMap();
                    reader.Skip();

                    return;
                }

                int valueCount = (int)valueCount64;

                int expectedByteCount;

                try
                {
                    expectedByteCount =
                        checked(valueCount * sizeof(float));
                }
                catch (OverflowException)
                {
                    RealmStudioXLogger.Error(
                        $"Unable to load height map: dimensions " +
                        $"{width} x {height} result in an " +
                        $"invalid data size.");

                    ClearHeightMap();
                    reader.Skip();

                    return;
                }

                /*
                 * Minimum and maximum elevation are metadata. If either value is
                 * invalid, retain a safe default rather than rejecting the
                 * entire map.
                 */
                if (!float.TryParse(
                        minimumElevationString,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out float minimumElevation))
                {
                    RealmStudioXLogger.Error(
                        $"Unable to load height map: invalid MinimumHeight " +
                        $"'{minimumElevationString}'. Using 0.");

                    minimumElevation = 0.0f;
                }

                if (!float.TryParse(
                        maximumElevationString,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out float maximumElevation))
                {
                    RealmStudioXLogger.Error(
                        $"Unable to load height map: invalid MaximumHeight " +
                        $"'{maximumElevationString}'. Using 0.");

                    maximumElevation = 0.0f;
                }

                MinimumElevation = minimumElevation;
                MaximumElevation = maximumElevation;
                ElevationUnit = elevationUnit ?? string.Empty;

                HeightMapPalette = null;

                /*
                 * An empty MapHeightMap element represents an empty height map.
                 */
                if (reader.IsEmptyElement)
                {
                    HeightMap =
                        new float[width, height];

                    return;
                }

                /*
                 * Enter the MapHeightMap element.
                 */
                reader.ReadStartElement("MapHeightMap");

                string? base64Data = null;

                while (reader.NodeType != XmlNodeType.EndElement ||
                       reader.LocalName != "MapHeightMap")
                {
                    if (reader.NodeType == XmlNodeType.Whitespace ||
                        reader.NodeType == XmlNodeType.SignificantWhitespace)
                    {
                        reader.Read();
                        continue;
                    }

                    if (reader.NodeType != XmlNodeType.Element)
                    {
                        reader.Read();
                        continue;
                    }

                    switch (reader.LocalName)
                    {
                        case "HypsometricPalette":

                            HeightMapPalette =
                                ReadHypsometricPalette(reader);

                            break;

                        case "Data":

                            try
                            {
                                base64Data =
                                    reader.ReadElementContentAsString();
                            }
                            catch (XmlException ex)
                            {
                                RealmStudioXLogger.Exception(
                                    "Unable to load height map: invalid " +
                                    "height map data XML.",
                                    ex);

                                base64Data = null;

                                SkipCurrentElementSafely(reader);
                            }

                            break;

                        default:

                            RealmStudioXLogger.Error(
                                $"Ignoring unknown element " +
                                $"'{reader.LocalName}' in MapHeightMap.");

                            reader.Skip();

                            break;
                    }
                }

                reader.ReadEndElement();

                /*
                 * No data means an empty height map.
                 */
                if (string.IsNullOrWhiteSpace(base64Data))
                {
                    HeightMap =
                        new float[width, height];

                    return;
                }

                byte[] data;

                try
                {
                    data =
                        Convert.FromBase64String(base64Data);
                }
                catch (FormatException ex)
                {
                    RealmStudioXLogger.Exception(
                        "Unable to load height map: height map data " +
                        "is not valid Base64 data.",
                        ex);

                    HeightMap =
                        new float[width, height];

                    return;
                }

                if (data.Length != expectedByteCount)
                {
                    RealmStudioXLogger.Error(
                        $"Unable to load height map: incorrect data size. " +
                        $"Expected {expectedByteCount} bytes for a " +
                        $"{width} x {height} height map, but found " +
                        $"{data.Length} bytes.");

                    HeightMap =
                        new float[width, height];

                    return;
                }

                /*
                 * Reconstruct the runtime float[,] array.
                 */
                HeightMap =
                    new float[width, height];

                int byteIndex = 0;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int bits =
                            data[byteIndex]
                            | (data[byteIndex + 1] << 8)
                            | (data[byteIndex + 2] << 16)
                            | (data[byteIndex + 3] << 24);

                        HeightMap[x, y] =
                            BitConverter.Int32BitsToSingle(bits);

                        byteIndex += sizeof(float);
                    }
                }
            }
            catch (Exception ex)
            {
                /*
                 * A damaged height map should never prevent the remainder
                 * of the RealmStudioX map from loading.
                 */
                RealmStudioXLogger.Exception(
                    "Unexpected error while loading height map. " +
                    "The height map will be discarded.",
                    ex);

                ClearHeightMap();

                SkipCurrentElementSafely(reader);
            }
        }

        private static HypsometricPalette? ReadHypsometricPalette(
            XmlReader reader)
        {
            try
            {
                HypsometricPalette palette = new();

                string? id = reader.GetAttribute("Id");
                string? name = reader.GetAttribute("Name");
                string? isLocked = reader.GetAttribute("IsLocked");

                if (!string.IsNullOrWhiteSpace(id))
                    palette.Id = id;

                if (!string.IsNullOrWhiteSpace(name))
                    palette.Name = name;

                if (bool.TryParse(isLocked, out bool locked))
                    palette.IsLocked = locked;

                if (reader.IsEmptyElement)
                {
                    reader.Read();
                    return palette;
                }

                reader.ReadStartElement("HypsometricPalette");

                while (reader.NodeType != XmlNodeType.EndElement ||
                       reader.LocalName != "HypsometricPalette")
                {
                    if (reader.NodeType == XmlNodeType.Whitespace ||
                        reader.NodeType == XmlNodeType.SignificantWhitespace)
                    {
                        reader.Read();
                        continue;
                    }

                    if (reader.NodeType != XmlNodeType.Element)
                    {
                        reader.Read();
                        continue;
                    }

                    if (reader.LocalName != "Tint")
                    {
                        reader.Skip();
                        continue;
                    }

                    HypsometricTint? tint =
                        ReadHypsometricTint(reader);

                    if (tint != null)
                        palette.Tints.Add(tint);
                }

                reader.ReadEndElement();

                palette.SortTints();

                return palette;
            }
            catch (Exception ex)
            {
                RealmStudioXLogger.Exception(
                    "Unable to load hypsometric palette. " +
                    "The palette will be ignored.",
                    ex);

                SkipCurrentElementSafely(reader);

                return null;
            }
        }

        private static HypsometricTint? ReadHypsometricTint(
            XmlReader reader)
        {
            try
            {
                string? id = reader.GetAttribute("Id");
                string? heightString =
                    reader.GetAttribute("NormalizedHeight");
                string? colorString =
                    reader.GetAttribute("Color");

                if (!float.TryParse(
                        heightString,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out float normalizedHeight))
                {
                    RealmStudioXLogger.Error(
                        $"Ignoring hypsometric tint with invalid " +
                        $"NormalizedHeight '{heightString}'.");

                    reader.Skip();
                    return null;
                }

                normalizedHeight =
                    Math.Clamp(normalizedHeight, -1.0f, 1.0f);

                if (string.IsNullOrWhiteSpace(colorString))
                {
                    RealmStudioXLogger.Error(
                        "Ignoring hypsometric tint with missing color.");

                    reader.Skip();
                    return null;
                }

                SKColor color;

                try
                {
                    color =
                        XmlColorConverter.Deserialize(colorString);
                }
                catch (Exception ex)
                {
                    RealmStudioXLogger.Exception(
                        $"Ignoring hypsometric tint with invalid color " +
                        $"'{colorString}'.",
                        ex);

                    reader.Skip();
                    return null;
                }

                HypsometricTint tint = new()
                {
                    NormalizedHeight = normalizedHeight,
                    Color = color
                };

                if (!string.IsNullOrWhiteSpace(id))
                    tint.Id = id;

                reader.Skip();

                return tint;
            }
            catch (Exception ex)
            {
                RealmStudioXLogger.Exception(
                    "Unable to load hypsometric tint. " +
                    "The tint will be ignored.",
                    ex);

                SkipCurrentElementSafely(reader);

                return null;
            }
        }

        private void ClearHeightMap()
        {
            HeightMap = null;
        }

        private static void SkipCurrentElementSafely(XmlReader reader)
        {
            try
            {
                if (reader.ReadState != ReadState.Interactive)
                    return;

                if (reader.NodeType == XmlNodeType.Element)
                {
                    reader.Skip();
                }
                else
                {
                    // We're somewhere inside the element. Advance until we
                    // reach something XmlSerializer can continue from.
                    while (reader.ReadState == ReadState.Interactive &&
                           reader.NodeType != XmlNodeType.EndElement)
                    {
                        if (!reader.Read())
                            break;
                    }

                    if (reader.NodeType == XmlNodeType.EndElement)
                    {
                        reader.Read();
                    }
                }
            }
            catch (XmlException)
            {
                // Nothing more can safely be done here. The original error
                // has already been logged by ReadXml().
            }
        }

        public void Initialize(int width, int height)
        {
            HeightMap = new float[width, height];
        }

        public unsafe void UpdateHeightMapBitmap(
    SKBitmap bitmap,
    float[,] heightMap,
    int left,
    int top,
    int right,
    int bottom,
    SKColor[] colorLookup)
        {
            if (colorLookup == null || colorLookup.Length == 0)
                return;

            using SKPixmap? pixmap = bitmap.PeekPixels();

            if (pixmap == null)
                return;

            IntPtr pixels = pixmap.GetPixels();

            if (pixels == IntPtr.Zero)
                return;

            int rowBytes = pixmap.RowBytes;

            left = Math.Max(0, left);
            top = Math.Max(0, top);

            right = Math.Min(
                heightMap.GetLength(0) - 1,
                right);

            bottom = Math.Min(
                heightMap.GetLength(1) - 1,
                bottom);

            if (left > right || top > bottom)
                return;

            const float lookupScale =
                (HypsometricLookupSize - 1) / 2.0f;

            byte* pixelBase = (byte*)pixels;

            for (int y = top; y <= bottom; y++)
            {
                int pixelY = y - top;

                byte* row =
                    pixelBase + (pixelY * rowBytes);

                for (int x = left; x <= right; x++)
                {
                    float elevation = heightMap[x, y];

                    float normalizedHeight =
                        NormalizeHeight(
                            elevation,
                            MinimumElevation,
                            MaximumElevation);

                    int lookupIndex =
                        (int)Math.Round(
                            (normalizedHeight + 1.0f) *
                            lookupScale);

                    lookupIndex = Math.Clamp(
                        lookupIndex,
                        0,
                        colorLookup.Length - 1);

                    SKColor color =
                        colorLookup[lookupIndex];

                    int pixelX = x - left;
                    int offset = pixelX * 4;

                    row[offset] = color.Red;
                    row[offset + 1] = color.Green;
                    row[offset + 2] = color.Blue;
                    row[offset + 3] = color.Alpha;
                }
            }
        }

        public unsafe void UpdateHeightMapBitmap(
            SKBitmap bitmap,
            float[,] heightMap,
            int left,
            int top,
            int right,
            int bottom,
            int destinationLeft = 0,
            int destinationTop = 0)
        {
            if (HeightMapPalette == null
                || HeightMapPalette.Tints.Count == 0)
            {
                return;
            }

            if (_hypsometricColorLookup == null
                || !ReferenceEquals(
                    _lookupPalette,
                    HeightMapPalette)
                || _lookupMinimumElevation != MinimumElevation
                || _lookupMaximumElevation != MaximumElevation)
            {
                RebuildHypsometricColorLookup();
            }

            if (_hypsometricColorLookup == null)
                return;

            using SKPixmap? pixmap = bitmap.PeekPixels();

            if (pixmap == null)
                return;

            IntPtr pixels = pixmap.GetPixels();

            if (pixels == IntPtr.Zero)
                return;

            int rowBytes = pixmap.RowBytes;

            // Clamp the supplied map rectangle to the height map.
            left = Math.Max(0, left);
            top = Math.Max(0, top);

            right = Math.Min(
                heightMap.GetLength(0) - 1,
                right);

            bottom = Math.Min(
                heightMap.GetLength(1) - 1,
                bottom);

            if (left > right || top > bottom)
                return;

            const float lookupScale =
                (HypsometricLookupSize - 1) / 2.0f;

            byte* pixelBase = (byte*)pixels;

            for (int y = top; y <= bottom; y++)
            {
                // Convert the map Y coordinate to the destination
                // bitmap's local Y coordinate.
                int pixelY =
                    destinationTop + (y - top);

                byte* row =
                    pixelBase + (pixelY * rowBytes);

                for (int x = left; x <= right; x++)
                {
                    float elevation =
                        heightMap[x, y];

                    float normalizedHeight =
                        NormalizeHeight(
                            elevation,
                            MinimumElevation,
                            MaximumElevation);

                    int lookupIndex =
                        (int)Math.Round(
                            (normalizedHeight + 1.0f) *
                            lookupScale);

                    lookupIndex = Math.Clamp(
                        lookupIndex,
                        0,
                        HypsometricLookupSize - 1);

                    SKColor color =
                        _hypsometricColorLookup[lookupIndex];

                    // Convert the map X coordinate to the
                    // destination bitmap's local X coordinate.
                    int pixelX =
                        destinationLeft + (x - left);

                    int offset = pixelX * 4;

                    row[offset] = color.Red;
                    row[offset + 1] = color.Green;
                    row[offset + 2] = color.Blue;
                    row[offset + 3] = color.Alpha;
                }
            }
        }

        public static float NormalizeHeight(
            float elevation,
            float minimumHeight,
            float maximumHeight)
        {
            if (elevation < 0.0f)
            {
                if (minimumHeight >= 0.0f)
                    return 0.0f;

                return Math.Clamp(
                    elevation / Math.Abs(minimumHeight),
                    -1.0f,
                    0.0f);
            }

            if (maximumHeight <= 0.0f)
                return 0.0f;

            return Math.Clamp(
                elevation / maximumHeight,
                0.0f,
                1.0f);
        }

        public static SKColor GetHypsometricColor(
            float normalizedHeight,
            HypsometricPalette palette)
        {
            if (palette.Tints.Count == 0)
                return SKColors.Transparent;

            if (palette.Tints.Count == 1)
                return palette.Tints[0].Color;

            if (normalizedHeight <=
                palette.Tints[0].NormalizedHeight)
            {
                return palette.Tints[0].Color;
            }

            if (normalizedHeight >=
                palette.Tints[^1].NormalizedHeight)
            {
                return palette.Tints[^1].Color;
            }

            for (int i = 0; i < palette.Tints.Count - 1; i++)
            {
                HypsometricTint lower = palette.Tints[i];
                HypsometricTint upper = palette.Tints[i + 1];

                if (normalizedHeight >= lower.NormalizedHeight
                    && normalizedHeight <= upper.NormalizedHeight)
                {
                    float range =
                        upper.NormalizedHeight -
                        lower.NormalizedHeight;

                    if (range <= 0.0f)
                        return lower.Color;

                    float t =
                        (normalizedHeight -
                         lower.NormalizedHeight) /
                        range;

                    return InterpolateColor(
                        lower.Color,
                        upper.Color,
                        t);
                }
            }

            return palette.Tints[^1].Color;
        }

        private static SKColor InterpolateColor(
            SKColor first,
            SKColor second,
            float amount)
        {
            amount = Math.Clamp(amount, 0.0f, 1.0f);

            byte r = (byte)Math.Round(
                first.Red +
                ((second.Red - first.Red) * amount));

            byte g = (byte)Math.Round(
                first.Green +
                ((second.Green - first.Green) * amount));

            byte b = (byte)Math.Round(
                first.Blue +
                ((second.Blue - first.Blue) * amount));

            byte a = (byte)Math.Round(
                first.Alpha +
                ((second.Alpha - first.Alpha) * amount));

            return new SKColor(r, g, b, a);
        }

        public void RebuildHypsometricColorLookup()
        {
            if (HeightMapPalette == null || HeightMapPalette.Tints.Count == 0)
            {
                _hypsometricColorLookup = null;
                _lookupPalette = null;
                return;
            }

            HeightMapPalette.SortTints();

            SKColor[] lookup =
                new SKColor[HypsometricLookupSize];

            for (int i = 0; i < HypsometricLookupSize; i++)
            {
                float normalizedHeight =
                    -1.0f +
                    (2.0f * i / (HypsometricLookupSize - 1));

                lookup[i] = GetHypsometricColor(
                    normalizedHeight,
                    HeightMapPalette);
            }

            _hypsometricColorLookup = lookup;

            _lookupMinimumElevation = MinimumElevation;
            _lookupMaximumElevation = MaximumElevation;
            _lookupPalette = HeightMapPalette;
        }

        internal void EnsureHypsometricColorLookup()
        {
            if (HeightMapPalette == null ||
                HeightMapPalette.Tints.Count == 0)
            {
                return;
            }

            if (_hypsometricColorLookup == null ||
                !ReferenceEquals(
                    _lookupPalette,
                    HeightMapPalette) ||
                _lookupMinimumElevation != MinimumElevation ||
                _lookupMaximumElevation != MaximumElevation)
            {
                RebuildHypsometricColorLookup();
            }
        }

        internal SKColor GetHeightMapColorFast(float elevation)
        {
            if (_hypsometricColorLookup == null)
                return SKColors.Transparent;

            float normalizedHeight =
                NormalizeHeight(
                    elevation,
                    MinimumElevation,
                    MaximumElevation);

            const float lookupScale =
                (HypsometricLookupSize - 1) / 2.0f;

            int lookupIndex =
                (int)Math.Round(
                    (normalizedHeight + 1.0f) *
                    lookupScale);

            lookupIndex =
                Math.Clamp(
                    lookupIndex,
                    0,
                    HypsometricLookupSize - 1);

            return _hypsometricColorLookup[lookupIndex];
        }

        public SKColor[] GetHypsometricColorLookupSnapshot()
        {
            if (_hypsometricColorLookup == null
                || !ReferenceEquals(_lookupPalette, HeightMapPalette)
                || _lookupMinimumElevation != MinimumElevation
                || _lookupMaximumElevation != MaximumElevation)
            {
                RebuildHypsometricColorLookup();
            }

            if (_hypsometricColorLookup == null)
                return [];

            return (SKColor[])_hypsometricColorLookup.Clone();
        }

        public override void Render(SKCanvas canvas, FontManager? fontManager = null, SKPath? clipPath = null)
        {
            // no op
        }

        public void RenderContours(
            SKCanvas canvas,
            float contourInterval,
            int majorContourInterval,
            SKPaint contourPaint,
            SKPaint majorContourPaint)
        {
            if (HeightMap == null)
                return;

            if (contourInterval <= 0.0f)
                return;

            if (majorContourInterval < 1)
                majorContourInterval = 1;

            if (_contourPaths == null ||
                _contourInterval != contourInterval ||
                _majorContourInterval != majorContourInterval)
            {
                RebuildContours(
                    contourInterval,
                    majorContourInterval);
            }

            if (_contourPaths == null)
                return;

            foreach (var pair in _contourPaths)
            {
                int contourIndex = pair.Key;

                SKPaint paint =
                    contourIndex % majorContourInterval == 0
                        ? majorContourPaint
                        : contourPaint;

                canvas.DrawPath(
                    pair.Value,
                    paint);
            }
        }

        private void RebuildContours(
            float contourInterval,
            int majorContourInterval)
        {
            if (HeightMap == null)
                return;

            int width = HeightMap.GetLength(0);
            int height = HeightMap.GetLength(1);

            if (width < 2 || height < 2)
                return;

            InvalidateContours();

            _contourInterval = contourInterval;
            _majorContourInterval = majorContourInterval;

            _contourPaths =
                new Dictionary<int, SKPath>();

            float minimumHeight = MinimumElevation;
            float maximumHeight = MaximumElevation;

            int firstContourIndex =
                (int)MathF.Ceiling(
                    minimumHeight / contourInterval);

            int lastContourIndex =
                (int)MathF.Floor(
                    maximumHeight / contourInterval);

            if (firstContourIndex > lastContourIndex)
                return;

            var builders =
                new Dictionary<int, SKPathBuilder>();

            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width - 1; x++)
                {
                    float h00 = HeightMap[x, y];
                    float h10 = HeightMap[x + 1, y];
                    float h11 = HeightMap[x + 1, y + 1];
                    float h01 = HeightMap[x, y + 1];

                    float minimumCell =
                        MathF.Min(
                            MathF.Min(h00, h10),
                            MathF.Min(h11, h01));

                    float maximumCell =
                        MathF.Max(
                            MathF.Max(h00, h10),
                            MathF.Max(h11, h01));

                    int cellFirstContour =
                        Math.Max(
                            firstContourIndex,
                            (int)MathF.Ceiling(
                                minimumCell / contourInterval));

                    int cellLastContour =
                        Math.Min(
                            lastContourIndex,
                            (int)MathF.Floor(
                                maximumCell / contourInterval));

                    if (cellFirstContour > cellLastContour)
                        continue;

                    for (int contourIndex = cellFirstContour;
                         contourIndex <= cellLastContour;
                         contourIndex++)
                    {
                        float contourHeight =
                            contourIndex * contourInterval;

                        int caseIndex =
                            GetMarchingSquaresCase(
                                h00,
                                h10,
                                h11,
                                h01,
                                contourHeight);

                        if (caseIndex == 0 ||
                            caseIndex == 15)
                        {
                            continue;
                        }

                        if (!builders.TryGetValue(
                                contourIndex,
                                out SKPathBuilder? builder))
                        {
                            builder = new SKPathBuilder();

                            builders.Add(
                                contourIndex,
                                builder);
                        }

                        AddContourSegments(
                            builder,
                            x,
                            y,
                            h00,
                            h10,
                            h11,
                            h01,
                            contourHeight,
                            caseIndex);
                    }
                }
            }

            foreach (var pair in builders)
            {
                SKPathBuilder builder = pair.Value;

                SKPath path =
                    builder.Detach();

                builder.Dispose();

                _contourPaths.Add(
                    pair.Key,
                    path);
            }
        }

        public void InvalidateContours()
        {
            if (_contourPaths != null)
            {
                foreach (SKPath path in _contourPaths.Values)
                {
                    path.Dispose();
                }

                _contourPaths.Clear();
                _contourPaths = null;
            }
        }

        private static int GetMarchingSquaresCase(
            float h00,
            float h10,
            float h11,
            float h01,
            float contourHeight)
        {
            int caseIndex = 0;

            if (h00 >= contourHeight)
                caseIndex |= 1;

            if (h10 >= contourHeight)
                caseIndex |= 2;

            if (h11 >= contourHeight)
                caseIndex |= 4;

            if (h01 >= contourHeight)
                caseIndex |= 8;

            return caseIndex;
        }

        private static void AddContourSegments(
            SKPathBuilder builder,
            int x,
            int y,
            float h00,
            float h10,
            float h11,
            float h01,
            float contourHeight,
            int caseIndex)
        {
            switch (caseIndex)
            {
                case 1:
                case 14:
                    {
                        SKPoint left =
                            InterpolatePoint(
                                x,
                                y,
                                x,
                                y + 1,
                                h00,
                                h01,
                                contourHeight);

                        SKPoint top =
                            InterpolatePoint(
                                x,
                                y,
                                x + 1,
                                y,
                                h00,
                                h10,
                                contourHeight);

                        AddSegment(
                            builder,
                            left,
                            top);

                        break;
                    }

                case 2:
                case 13:
                    {
                        SKPoint top =
                            InterpolatePoint(
                                x,
                                y,
                                x + 1,
                                y,
                                h00,
                                h10,
                                contourHeight);

                        SKPoint right =
                            InterpolatePoint(
                                x + 1,
                                y,
                                x + 1,
                                y + 1,
                                h10,
                                h11,
                                contourHeight);

                        AddSegment(
                            builder,
                            top,
                            right);

                        break;
                    }

                case 3:
                case 12:
                    {
                        SKPoint left =
                            InterpolatePoint(
                                x,
                                y,
                                x,
                                y + 1,
                                h00,
                                h01,
                                contourHeight);

                        SKPoint right =
                            InterpolatePoint(
                                x + 1,
                                y,
                                x + 1,
                                y + 1,
                                h10,
                                h11,
                                contourHeight);

                        AddSegment(
                            builder,
                            left,
                            right);

                        break;
                    }

                case 4:
                case 11:
                    {
                        SKPoint right =
                            InterpolatePoint(
                                x + 1,
                                y,
                                x + 1,
                                y + 1,
                                h10,
                                h11,
                                contourHeight);

                        SKPoint bottom =
                            InterpolatePoint(
                                x,
                                y + 1,
                                x + 1,
                                y + 1,
                                h01,
                                h11,
                                contourHeight);

                        AddSegment(
                            builder,
                            right,
                            bottom);

                        break;
                    }

                case 5:
                    {
                        SKPoint top =
                            InterpolatePoint(
                                x,
                                y,
                                x + 1,
                                y,
                                h00,
                                h10,
                                contourHeight);

                        SKPoint right =
                            InterpolatePoint(
                                x + 1,
                                y,
                                x + 1,
                                y + 1,
                                h10,
                                h11,
                                contourHeight);

                        SKPoint bottom =
                            InterpolatePoint(
                                x,
                                y + 1,
                                x + 1,
                                y + 1,
                                h01,
                                h11,
                                contourHeight);

                        SKPoint left =
                            InterpolatePoint(
                                x,
                                y,
                                x,
                                y + 1,
                                h00,
                                h01,
                                contourHeight);

                        AddSegment(
                            builder,
                            top,
                            right);

                        AddSegment(
                            builder,
                            bottom,
                            left);

                        break;
                    }

                case 6:
                case 9:
                    {
                        SKPoint top =
                            InterpolatePoint(
                                x,
                                y,
                                x + 1,
                                y,
                                h00,
                                h10,
                                contourHeight);

                        SKPoint bottom =
                            InterpolatePoint(
                                x,
                                y + 1,
                                x + 1,
                                y + 1,
                                h01,
                                h11,
                                contourHeight);

                        AddSegment(
                            builder,
                            top,
                            bottom);

                        break;
                    }

                case 7:
                case 8:
                    {
                        SKPoint left =
                            InterpolatePoint(
                                x,
                                y,
                                x,
                                y + 1,
                                h00,
                                h01,
                                contourHeight);

                        SKPoint bottom =
                            InterpolatePoint(
                                x,
                                y + 1,
                                x + 1,
                                y + 1,
                                h01,
                                h11,
                                contourHeight);

                        AddSegment(
                            builder,
                            left,
                            bottom);

                        break;
                    }

                case 10:
                    {
                        SKPoint top =
                            InterpolatePoint(
                                x,
                                y,
                                x + 1,
                                y,
                                h00,
                                h10,
                                contourHeight);

                        SKPoint left =
                            InterpolatePoint(
                                x,
                                y,
                                x,
                                y + 1,
                                h00,
                                h01,
                                contourHeight);

                        SKPoint right =
                            InterpolatePoint(
                                x + 1,
                                y,
                                x + 1,
                                y + 1,
                                h10,
                                h11,
                                contourHeight);

                        SKPoint bottom =
                            InterpolatePoint(
                                x,
                                y + 1,
                                x + 1,
                                y + 1,
                                h01,
                                h11,
                                contourHeight);

                        AddSegment(
                            builder,
                            top,
                            left);

                        AddSegment(
                            builder,
                            right,
                            bottom);

                        break;
                    }
            }
        }

        private static void AddSegment(
            SKPathBuilder builder,
            SKPoint first,
            SKPoint second)
        {
            builder.MoveTo(first);
            builder.LineTo(second);
        }

        private readonly record struct ContourPointKey(int X, int Y);

        private static ContourPointKey GetContourPointKey(SKPoint point)
        {
            const float scale = 100000.0f;

            return new ContourPointKey(
                (int)MathF.Round(
                    point.X * scale),

                (int)MathF.Round(
                    point.Y * scale));
        }

        private sealed class ContourPath
        {
            public List<SKPoint> Points { get; } = [];

            public bool Closed { get; set; }

            public ContourPointKey Start =>
                GetContourPointKey(
                    Points[0]);

            public ContourPointKey End =>
                GetContourPointKey(
                    Points[^1]);
        }

        private sealed class ContourCollection
        {
            public Dictionary<
                ContourPointKey,
                ContourPath> PathsByEndpoint
            { get; } = [];

            public List<ContourPath> Paths { get; } = [];
        }

        private static SKPoint InterpolatePoint(
            float x1,
            float y1,
            float x2,
            float y2,
            float height1,
            float height2,
            float contourHeight)
        {
            float difference =
                height2 - height1;

            if (MathF.Abs(difference) < 0.000001f)
            {
                return new SKPoint(
                    (x1 + x2) * 0.5f,
                    (y1 + y2) * 0.5f);
            }

            float t =
                (contourHeight - height1) /
                difference;

            t = Math.Clamp(
                t,
                0.0f,
                1.0f);

            return new SKPoint(
                x1 + ((x2 - x1) * t),
                y1 + ((y2 - y1) * t));
        }

        private static void ExtendContourPath(
            ContourCollection collection,
            ContourPath path,
            ContourPointKey existingEndpoint,
            SKPoint newPoint)
        {
            ContourPointKey oldStart =
                path.Start;

            ContourPointKey oldEnd =
                path.End;

            collection.PathsByEndpoint.Remove(
                oldStart);

            collection.PathsByEndpoint.Remove(
                oldEnd);

            if (oldStart == existingEndpoint)
            {
                path.Points.Insert(
                    0,
                    newPoint);
            }
            else
            {
                path.Points.Add(
                    newPoint);
            }

            collection.PathsByEndpoint[
                path.Start] =
                path;

            collection.PathsByEndpoint[
                path.End] =
                path;
        }

        private static void DrawContourSegment(
            SKCanvas canvas,
            SKPaint paint,
            SKPoint first,
            SKPoint second)
        {
            canvas.DrawLine(
                first,
                second,
                paint);
        }

        private static bool IsMajorContour(
            float contourHeight,
            float contourInterval,
            int majorContourInterval)
        {
            if (majorContourInterval <= 1)
                return true;

            float majorInterval =
                contourInterval * majorContourInterval;

            if (majorInterval <= 0.0f)
                return false;

            float remainder =
                MathF.Abs(
                    contourHeight % majorInterval);

            const float tolerance = 0.0001f;

            return remainder < tolerance ||
                   MathF.Abs(remainder - majorInterval) < tolerance;
        }

        public override bool HitTest(SKPoint worldPos)
        {
            return false;
        }

        public override IShapeState CaptureState()
        {
            throw new NotImplementedException();
        }

        public override void RestoreState(IShapeState state)
        {
            throw new NotImplementedException();
        }
    }


    public class HeightMapValue
    {
        [XmlAttribute]
        public int X { get; set; }

        [XmlAttribute]
        public int Y { get; set; }

        [XmlText]
        public float Value { get; set; }
    }
}
