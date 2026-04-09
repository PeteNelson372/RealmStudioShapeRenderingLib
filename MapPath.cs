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
    public class MapPath : Shape2D
    {
        public string MapPathName { get; set; } = "";
        public string MapPathDescription { get; set; } = string.Empty;
        public List<SKPoint> ControlPoints { get; } = [];
        public float VariationSeed { get; set; }
        public EditablePolylineEditor Editor { get; }
        public PathRenderStyle RenderStyle { get; set; } = new();

        public bool DrawOverSymbols { get; set; } = false;

        public MapPath()
        {
            VariationSeed = Random.Shared.NextSingle() * 1000f;
            Editor = new EditablePolylineEditor(ControlPoints)
            {
                OnChanged = () =>
                {
                    SetGeometry(Utilities.BuildPath(ControlPoints));
                }
            };
        }

        public override void Render(SKCanvas canvas, FontManager? _)
        {
            PathRenderer.Render(canvas, ControlPoints, RenderStyle);
        }

        public void ResolveAssets(IAssetProvider assets)
        {
            // Resolve path texture
            if (RenderStyle.Texture == null && !string.IsNullOrEmpty(RenderStyle.TextureId))
            {
                SKImage? textureImage = assets.GetImage(RenderStyle.TextureId);

                if (textureImage != null)
                {
                    SKBitmap textureBitmap = SKBitmap.FromImage(textureImage);

                    SKBitmap resizedBitmap = Utilities.ScaleSKBitmap(textureBitmap, RenderStyle.TextureScale);

                    SKBitmap opacitySetBitmap = Utilities.SetBitmapOpacity(resizedBitmap, RenderStyle.TextureOpacity);
                   
                    RenderStyle.Texture = opacitySetBitmap;
                }
            }
        }
    }
}
