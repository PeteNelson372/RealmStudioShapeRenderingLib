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
#nullable enable
using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public sealed class LandformShadingSettings
    {
        // -------------------------------------------------
        // Base land fill & outline (non-gradient)
        // -------------------------------------------------

        public bool UseTextureBackground { get; set; } = true;

        public SKColor LandformBackgroundColor { get; set; } = new (140, 180, 120);

        public SKColor LandformOutlineColor { get; set; } = new(62, 55, 40);

        public string? LandformTextureId { get; set; }

        public int LandformOutlineWidth { get; set; } = 2;

        // -------------------------------------------------
        // Interior shading (gradient inward from coast)
        // -------------------------------------------------

        public bool EnableInteriorShading { get; set; } = true;

        public float InteriorShadingDepth { get; set; } = 200f;

        public int InteriorShadingSteps { get; set; } = 32;

        /// <summary>
        /// How far inland the shading reaches (world units)
        /// </summary>
        public float LandShadingDepth { get; set; } = 200f;

        /// <summary>
        /// Number of gradient steps (higher = smoother)
        /// </summary>
        public int Steps { get; set; } = 24;

        /// <summary>
        /// Color near the coastline
        /// </summary>
        public SKColor CoastColor { get; set; }
            = new SKColor(80, 110, 70);

        /// <summary>
        /// Color deeper inland
        /// </summary>
        public SKColor InlandColor { get; set; }
            = new SKColor(140, 180, 120);

        /// <summary>
        /// Alpha inland
        /// </summary>
        public byte MaxAlpha { get; set; } = 110;

        /// <summary>
        /// Alpha near the coast (0 = no shading at coast, 255 = full shading at coast)
        /// </summary>
        public byte MinAlpha { get; set; } = 20;

        /// <summary>
        /// Controls how quickly shading falls off inward
        /// </summary>
        public float FalloffPower { get; set; } = 1.8f;

        public bool EnableNoise { get; set; } = true;
        public float NoiseStrength { get; set; } = 0.1f;   // 0–0.5 recommended
        public float NoiseScale { get; set; } = 0.02f;     // smaller = larger features
        public int NoiseSeed { get; set; } = 1337;

        public float DepthScale { get; set; } = 0.35f;   // % of min dimension

        public float InteriorCurvePower { get; set; } = 2.0f;


        public LandformShadingSettings Clone()
        {
            return new LandformShadingSettings()
            {
                UseTextureBackground = UseTextureBackground,
                MaxAlpha = MaxAlpha,
                MinAlpha = MinAlpha,
                FalloffPower = FalloffPower,
                LandformBackgroundColor = LandformBackgroundColor,
                LandformOutlineColor = LandformOutlineColor,
                LandformOutlineWidth = LandformOutlineWidth,
                LandformTextureId = LandformTextureId,
                EnableInteriorShading = EnableInteriorShading,
                LandShadingDepth = LandShadingDepth,
                Steps = Steps,
                CoastColor = CoastColor,
                InlandColor = InlandColor,
            };
        }
    }

}
