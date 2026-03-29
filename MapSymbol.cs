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
{    public class MapSymbol : MapComponent2D
    {
        public MapSymbolDefinition SymbolDefinition { get; set; } = new();

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public SKPoint Location { get; set; }
        
        public float Rotation { get; set; }
        
        public float Scale { get; set; } = 1f;

        public bool Flip { get; set; }

        public SKColor[] CustomSymbolColors { get; set; } = new SKColor[3];

        public override SKRect Bounds => throw new NotImplementedException();

        public override IShapeState CaptureState()
        {
            return new MapSymbolState
            {
                Location = Location,
                Rotation = Rotation,
                Scale = Scale,
                Flip = Flip,

                CustomColors = (SKColor[])CustomSymbolColors.Clone(),

                Name = Name,
                Description = Description
            };
        }

        public override bool HitTest(SKPoint worldPos)
        {
            throw new NotImplementedException();
        }

        public override void Render(SKCanvas canvas)
        {
            //var renderer = SymbolRendererFactory.Get(SymbolDefinition.BaseColorType);
        }

        public override void RestoreState(IShapeState state)
        {
            var s = (MapSymbolState)state;

            Location = s.Location;
            Rotation = s.Rotation;
            Scale = s.Scale;
            Flip = s.Flip;

            CustomSymbolColors = (SKColor[])s.CustomColors.Clone();

            Name = s.Name;
            Description = s.Description;
        }
    }

}
