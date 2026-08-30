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
        public float MinimumHeight { get; set; }
        public float MaximumHeight { get; set; }
        public string HeightUnit { get; set; } = string.Empty;

        [XmlIgnore]
        public float[,]? HeightMap { get; private set; }

        [XmlIgnore]
        public SKBitmap? HeightMapBitmap { get; private set; }

        [XmlIgnore]
        public HypsometricPalette? HeightMapPalette { get; set; }

        private const int HypsometricLookupSize = 4096;

        private SKColor[]? _hypsometricColorLookup;

        private float _lookupMinimumHeight;
        private float _lookupMaximumHeight;
        private HypsometricPalette? _lookupPalette;

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
                "MinimumHeight",
                MinimumHeight.ToString(CultureInfo.InvariantCulture));

            writer.WriteAttributeString(
                "MaximumHeight",
                MaximumHeight.ToString(CultureInfo.InvariantCulture));

            writer.WriteAttributeString(
                "HeightUnit",
                HeightUnit);

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

                string? minimumHeightString =
                    reader.GetAttribute("MinimumHeight");

                string? maximumHeightString =
                    reader.GetAttribute("MaximumHeight");

                string? heightUnit =
                    reader.GetAttribute("HeightUnit");

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
                 * Minimum and maximum height are metadata. If either value is
                 * invalid, retain a safe default rather than rejecting the
                 * entire map.
                 */
                if (!float.TryParse(
                        minimumHeightString,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out float minimumHeight))
                {
                    RealmStudioXLogger.Error(
                        $"Unable to load height map: invalid MinimumHeight " +
                        $"'{minimumHeightString}'. Using 0.");

                    minimumHeight = 0.0f;
                }

                if (!float.TryParse(
                        maximumHeightString,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out float maximumHeight))
                {
                    RealmStudioXLogger.Error(
                        $"Unable to load height map: invalid MaximumHeight " +
                        $"'{maximumHeightString}'. Using 0.");

                    maximumHeight = 0.0f;
                }

                MinimumHeight = minimumHeight;
                MaximumHeight = maximumHeight;
                HeightUnit = heightUnit ?? string.Empty;

                HeightMapPalette = null;

                /*
                 * An empty MapHeightMap element represents an empty height map.
                 */
                if (reader.IsEmptyElement)
                {
                    HeightMap =
                        new float[width, height];

                    RebuildHeightMapBitmap();

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

                    RebuildHeightMapBitmap();

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

                    RebuildHeightMapBitmap();

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

                    RebuildHeightMapBitmap();

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

                /*
                 * The actual height data has been reconstructed.
                 * Recreate the rendering bitmap from it.
                 */
                RebuildHeightMapBitmap();
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

            HeightMapBitmap?.Dispose();
            HeightMapBitmap = null;
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

            RebuildHeightMapBitmap();
        }

        public void RebuildHeightMapBitmap()
        {
            if (HeightMap == null)
            {
                HeightMapBitmap?.Dispose();
                HeightMapBitmap = null;
                return;
            }

            int width = HeightMap.GetLength(0);
            int height = HeightMap.GetLength(1);

            HeightMapBitmap?.Dispose();

            HeightMapBitmap = new SKBitmap(
                new SKImageInfo(
                    width,
                    height,
                    SKColorType.Rgba8888,
                    SKAlphaType.Premul));

            HeightMapBitmap.Erase(SKColors.Transparent);

            UpdateHeightMapBitmap(
                HeightMapBitmap,
                HeightMap,
                0,
                0,
                width - 1,
                height - 1);
        }

        public unsafe void UpdateHeightMapBitmap(
            SKBitmap bitmap,
            float[,] heightMap,
            int left,
            int top,
            int right,
            int bottom)
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
                || _lookupMinimumHeight != MinimumHeight
                || _lookupMaximumHeight != MaximumHeight)
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

            // Clamp the supplied map rectangle.
            left = Math.Max(0, left);
            top = Math.Max(0, top);

            right = Math.Min(heightMap.GetLength(0) - 1, right);
            bottom = Math.Min(heightMap.GetLength(1) - 1, bottom);

            if (left > right || top > bottom)
                return;

            const float lookupScale =
                (HypsometricLookupSize - 1) / 2.0f;

            byte* pixelBase = (byte*)pixels;

            for (int y = top; y <= bottom; y++)
            {
                // y is in map coordinates, so convert it to
                // the temporary bitmap's local coordinates.
                int pixelY = y - top;

                byte* row =
                    pixelBase + (pixelY * rowBytes);

                for (int x = left; x <= right; x++)
                {
                    float elevation = heightMap[x, y];

                    float normalizedHeight =
                        NormalizeHeight(
                            elevation,
                            MinimumHeight,
                            MaximumHeight);

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

                    // Convert map X coordinate to the
                    // temporary bitmap's local X coordinate.
                    int pixelX = x - left;

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

            _lookupMinimumHeight = MinimumHeight;
            _lookupMaximumHeight = MaximumHeight;
            _lookupPalette = HeightMapPalette;
        }

        public override void Render(SKCanvas canvas, FontManager? fontManager = null, SKPath? clipPath = null)
        {
            if (HeightMapBitmap != null)
            {
                canvas.DrawBitmap(HeightMapBitmap, 0, 0, SKSamplingOptions.Default);
            }
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
