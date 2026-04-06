/**************************************************************************************************************************
* Copyright 2024, Peter R. Nelson
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

namespace RealmStudioShapeRenderingLib
{
    public class MapLabel : MapComponent2D
    {
        public string Text { get; set; } = string.Empty;

        public string FontFamily { get; set; } = "Arial";
        public float FontSize { get; set; } = 24f;

        public SKColor FillColor { get; set; } = SKColors.White;

        public bool HasOutline { get; set; }
        public float OutlineWidth { get; set; }
        public SKColor OutlineColor { get; set; }

        public bool HasGlow { get; set; }
        public float GlowStrength { get; set; }
        public SKColor GlowColor { get; set; }

        public float Rotation { get; set; }

        // path/curve data
        public SKPath? CurvePath { get; set; }

        // Optional: arc parameters
        public float ArcRadius { get; set; }
        public float ArcAngle { get; set; }

        public MapLabel() { }


        public override void Render(SKCanvas canvas)
        {

        }

        public override bool HitTest(SKPoint worldPos)
        {
            throw new NotImplementedException();
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
}
