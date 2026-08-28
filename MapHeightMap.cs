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
        public MapDistanceUnit HeightUnit { get; set; }

        [XmlIgnore]
        public float[,]? HeightMap { get; private set; }

        [XmlIgnore]
        public SKBitmap? HeightMapBitmap { get; private set; }

        [XmlIgnore]
        public HypsometricPalette? HeightMapPalette { get; set; }

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

            // Convert the height map to one byte per pixel.
            byte[] data = new byte[width * height];

            int index = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float value = HeightMap[x, y];

                    // Height values are ultimately grayscale values.
                    value = Math.Clamp(value, 0.0f, 255.0f);

                    data[index++] = (byte)MathF.Round(value);
                }
            }

            string base64 = Convert.ToBase64String(data);

            writer.WriteString(base64);
        }

        public void ReadXml(XmlReader reader)
        {
            int width = 0;
            int height = 0;

            try
            {
                // Read dimensions from the MapHeightMap element.
                string? widthString = reader.GetAttribute("Width");
                string? heightString = reader.GetAttribute("Height");

                if (!int.TryParse(
                        widthString,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out width) ||
                    !int.TryParse(
                        heightString,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out height))
                {
                    RealmStudioXLogger.Error(
                        $"Unable to load height map: invalid or missing dimensions. " +
                        $"Width='{widthString}', Height='{heightString}'.");

                    ClearHeightMap();

                    // Skip the entire MapHeightMap element, including any old
                    // or malformed content it may contain.
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

                // Protect against overflow before allocating anything.
                long expectedLength64 = (long)width * height;

                if (expectedLength64 > int.MaxValue)
                {
                    RealmStudioXLogger.Error(
                        $"Unable to load height map: dimensions " +
                        $"{width} x {height} are too large.");

                    ClearHeightMap();
                    reader.Skip();

                    return;
                }

                int expectedLength = (int)expectedLength64;

                /*
                 * ReadElementContentAsString() consumes:
                 *
                 *     <MapHeightMap ...>
                 *         Base64 data
                 *     </MapHeightMap>
                 *
                 * and leaves the reader positioned on the next element.
                 */
                string base64;

                try
                {
                    base64 = reader.ReadElementContentAsString();
                }
                catch (XmlException ex)
                {
                    RealmStudioXLogger.Exception(
                        "Unable to load height map: invalid height map XML.",
                        ex);

                    ClearHeightMap();

                    // ReadElementContentAsString may have failed somewhere
                    // inside the element. Try to get past the bad element.
                    SkipCurrentElementSafely(reader);

                    return;
                }

                // An empty element is valid. It simply represents an empty
                // height map (all pixels at sea level = 0).
                if (string.IsNullOrWhiteSpace(base64))
                {
                    HeightMap = new float[width, height];

                    RebuildHeightMapBitmap();

                    return;
                }

                byte[] data;

                try
                {
                    data = Convert.FromBase64String(base64);
                }
                catch (FormatException ex)
                {
                    RealmStudioXLogger.Exception(
                        "Unable to load height map: height map data is not " +
                        "valid Base64 data.",
                        ex);

                    // We know the dimensions are valid, so recover with an
                    // empty height map rather than losing the MapHeightMap.
                    HeightMap = new float[width, height];

                    RebuildHeightMapBitmap();

                    return;
                }

                if (data.Length != expectedLength)
                {
                    RealmStudioXLogger.Error(
                        $"Unable to load height map: incorrect data size. " +
                        $"Expected {expectedLength} bytes for a " +
                        $"{width} x {height} height map, but found " +
                        $"{data.Length} bytes.");

                    HeightMap = new float[width, height];

                    RebuildHeightMapBitmap();

                    return;
                }

                // Everything is valid. Reconstruct the runtime height array.
                HeightMap = new float[width, height];

                int index = 0;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        HeightMap[x, y] = data[index++];
                    }
                }

                // HeightMap is now valid, so recreate its rendering bitmap.
                RebuildHeightMapBitmap();
            }
            catch (Exception ex)
            {
                /*
                 * A damaged height map should never prevent the rest of the
                 * RealmStudioX map from loading.
                 */
                RealmStudioXLogger.Exception(
                    "Unexpected error while loading height map. " +
                    "The height map will be discarded.",
                    ex);

                ClearHeightMap();

                // If we're still somewhere inside the MapHeightMap XML,
                // make a best effort to advance beyond it.
                SkipCurrentElementSafely(reader);
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

        public void UpdateHeightMapBitmap(
            SKBitmap bitmap,
            float[,] heightMap,
            int left,
            int top,
            int right,
            int bottom)
        {
            if (HeightMapPalette == null)
            {
                return;
            }

            using SKPixmap? pixmap = bitmap.PeekPixels();

            if (pixmap != null)
            {
                IntPtr pixels = pixmap.GetPixels();
                int rowBytes = pixmap.RowBytes;

                for (int y = top; y <= bottom; y++)
                {
                    IntPtr row = pixels + ((y - top) * rowBytes);

                    for (int x = left; x <= right; x++)
                    {
                        float elevation = heightMap[x, y];

                        float normalizedHeight =
                            NormalizeHeight(
                                elevation,
                                MinimumHeight,
                                MaximumHeight);

                        SKColor color =
                            GetHypsometricColor(
                                normalizedHeight,
                                HeightMapPalette);

                        int pixelX = x - left;

                        int offset = pixelX * 4;

                        System.Runtime.InteropServices.Marshal.WriteByte(
                            row + offset,
                            color.Red);

                        System.Runtime.InteropServices.Marshal.WriteByte(
                            row + offset + 1,
                            color.Green);

                        System.Runtime.InteropServices.Marshal.WriteByte(
                            row + offset + 2,
                            color.Blue);

                        System.Runtime.InteropServices.Marshal.WriteByte(
                            row + offset + 3,
                            color.Alpha);
                    }
                }
            }
        }

        public static float NormalizeHeight(
            float elevation,
            float minimumHeight,
            float maximumHeight)
        {
            if (elevation < 0)
            {
                if (minimumHeight >= 0)
                    return 0;

                return Math.Clamp(
                    elevation / Math.Abs(minimumHeight),
                    -1.0f,
                    0.0f);
            }

            if (maximumHeight <= 0)
                return 0;

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

            palette.SortTints();

            if (normalizedHeight <= palette.Tints[0].NormalizedHeight)
                return palette.Tints[0].Color;

            if (normalizedHeight >= palette.Tints[^1].NormalizedHeight)
                return palette.Tints[^1].Color;

            for (int i = 0; i < palette.Tints.Count - 1; i++)
            {
                HypsometricTint lower = palette.Tints[i];
                HypsometricTint upper = palette.Tints[i + 1];

                if (normalizedHeight >= lower.NormalizedHeight &&
                    normalizedHeight <= upper.NormalizedHeight)
                {
                    float range =
                        upper.NormalizedHeight -
                        lower.NormalizedHeight;

                    if (range <= 0)
                        return lower.Color;

                    float t =
                        (normalizedHeight - lower.NormalizedHeight) /
                        range;

                    return Utilities.LerpColor(
                        lower.Color,
                        upper.Color,
                        t);
                }
            }

            return palette.Tints[^1].Color;
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
