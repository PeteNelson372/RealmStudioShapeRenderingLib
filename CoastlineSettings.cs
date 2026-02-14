/**************************************************************************************************************************
* Copyright 2026, Peter R. Nelson
*
* This file is part of the RealmStudio application. The RealmStudio application is intended
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
* For questions about the RealmStudio application or about licensing, please email
* support@brookmonte.com
*
***************************************************************************************************************************/
using SkiaSharp;
using System.Threading.Channels;

namespace RealmStudioShapeRenderingLib
{
    public class CoastlineSettings
    {
        public List<CoastlineBand> Bands { get; } = [];


        // User selection
        public LandformCoastlineStyle CoastlineStyle { get; set; }
            = LandformCoastlineStyle.HatchPattern;

        // Common numeric parameters
        public int EffectDistance { get; set; } = 120;
        public int BandCount { get; set; } = 8;

        // Base color
        public SKColor CoastlineColor { get; set; } = new(187, 156, 195, 183);

        // User-Defined Bands
        public List<CoastlineBand> UserBands = [];

        // Alpha falloff
        public byte MaxAlpha { get; set; } = 180;
        public byte MinAlpha { get; set; } = 20;

        // Textures
        public string? HatchTextureId { get; set; }
        public string? DashTextureId { get; set; }
        public string? CircularTextureId { get; set; }

        // other parameters
        public int TextureOpacity { get; set; }
        public int TextureScale { get; set; }
        public string? HatchBlendMode { get; set; }

        // Behavior toggles
        public bool PaintGradient { get; set; } = true;

        public CoastlineSettings Clone()
        {
            var clone =  new CoastlineSettings()
            {
                CoastlineStyle = CoastlineStyle,
                EffectDistance = EffectDistance,
                BandCount = BandCount,
                CoastlineColor = CoastlineColor,
                MaxAlpha = MaxAlpha,
                MinAlpha = MinAlpha,
                HatchTextureId = HatchTextureId,
                DashTextureId = DashTextureId,
                CircularTextureId = CircularTextureId,
                TextureOpacity = TextureOpacity,
                TextureScale = TextureScale,
                HatchBlendMode = HatchBlendMode,
                PaintGradient = PaintGradient,
            };

            foreach (var band in Bands)
                clone.Bands.Add(band.Clone());

            foreach (var band in UserBands)
                clone.UserBands.Add(band.Clone());

            return clone;
        }
    }
}
