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
using RealmStudioX.WPF.EditorUtilities;
using SkiaSharp;
using System.Xml.Serialization;

namespace RealmStudioShapeRenderingLib
{
    public class CoastlineSettings
    {
        // User selection
        [XmlElement]
        public LandformCoastlineStyle CoastlineStyle { get; set; }
            = LandformCoastlineStyle.UniformBlend;

        // Common numeric parameters
        [XmlElement]
        public int EffectDistance { get; set; } = 120;

        [XmlElement]
        public float UniformOutlineOuterRingRatio { get; set; } = 0.15f;

        // Base color
        [XmlIgnore]
        public SKColor CoastlineColor { get; set; } = SKColor.Parse("#BB9CC3B7");

        [XmlElement("CoastlineColor")]
        public string CoastlineColorXml
        {
            get => XmlColorConverter.Serialize(CoastlineColor);
            set => CoastlineColor = XmlColorConverter.Deserialize(value);
        }

        // Alpha falloff
        [XmlElement]
        public byte MaxAlpha { get; set; } = 110;

        [XmlElement]
        public byte MinAlpha { get; set; } = 20;

        [XmlElement]
        public float DepthScale { get; set; } = 0.35f;   // % of min dimension

        [XmlElement]
        public float ExteriorCurvePower { get; set; } = 2.0f;

        // Controls how quickly shading falls off outward
        [XmlElement]
        public float FalloffPower { get; set; } = 1.8f;

        // Textures
        [XmlElement]
        public string? HatchTextureId { get; set; }

        [XmlElement]
        public string? DashTextureId { get; set; }

        [XmlElement]
        public string? CircularTextureId { get; set; }

        [XmlIgnore]
        public SKImage? HatchTexture { get; set; }

        [XmlIgnore]
        public SKImage? DashTexture { get; set; }

        // other parameters
        [XmlElement]
        public int TextureOpacity { get; set; }

        [XmlElement]
        public int TextureScale { get; set; }

        [XmlElement]
        public string? HatchBlendMode { get; set; }

        // Behavior toggles
        [XmlElement]
        public bool PaintGradient { get; set; } = true;

        public CoastlineSettings Clone()
        {
            var clone =  new CoastlineSettings()
            {
                CoastlineStyle = CoastlineStyle,
                EffectDistance = EffectDistance,
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

            return clone;
        }
    }
}
