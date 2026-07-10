/**************************************************************************************************************************
* Copyright 2025, Peter R. Nelson
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
using System.Xml.Serialization;

namespace RealmStudioShapeRenderingLib
{
    public sealed class DrawnStamp : MapComponent2D, IPositionImageShape, IAlignable
    {
        private SKPoint _topLeft;
        private int _rotation;
        private float _opacity = 1.0f;
        private float _scale = 1.0f;
        private string _stampPath = string.Empty;
        private SKImage? _stampImage;

        [XmlElement]
        public SKPoint TopLeft
        {
            get => _topLeft;
            set => _topLeft = value;
        }

        [XmlElement]
        public int Rotation
        {
            get => _rotation;
            set => _rotation = value;
        }

        [XmlElement]
        public float Opacity
        {
            get => _opacity;
            set
            {
                if (value < 0 || value > 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Opacity must be between 0 and 1.");
                }
                _opacity = value;
            }
        }

        [XmlElement]
        public float Scale
        {
            get => _scale;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Scale must be greater than 0.");
                }
                _scale = value;
            }
        }

        [XmlElement]
        public string StampPath
        {
            get => _stampPath;
            set => _stampPath = value ?? throw new ArgumentNullException(nameof(value), "Stamp path cannot be null.");
        }

        [XmlIgnore]
        public SKImage? StampImage
        {
            get => _stampImage;
            set
            {
                _stampImage = value ?? throw new ArgumentNullException(nameof(value), "Stamp image cannot be null.");
            }
        }

        public override void Render(SKCanvas canvas, FontManager? fontManager = null, SKPath? clipPath = null)
        {
            if (StampImage != null)
            {
                using SKAutoCanvasRestore autoRestore = new(canvas, true);

                if (Rotation != 0)
                {
                    canvas.RotateDegrees(Rotation, (_topLeft.X + StampImage.Width) / 2, (_topLeft.Y + StampImage.Height) / 2);
                }

                canvas.DrawImage(StampImage,
                    new SKPoint(TopLeft.X - (StampImage.Width / 2), TopLeft.Y - (StampImage.Height / 2)), null);

                // TODO: not accurate when the stamp image is rotated
                Bounds = new SKRect(TopLeft.X, TopLeft.Y, TopLeft.X + StampImage.Width, TopLeft.Y + StampImage.Height);
            }
        }

        public override bool HitTest(SKPoint worldPos)
        {
            return Bounds.Contains(worldPos);
        }

        public override IShapeState CaptureState()
        {
            return new DrawnMapComponentState
            {
                TopLeft = TopLeft,
                TextureOpacity = Opacity,
                TextureScale = Scale,
                Rotation = Rotation,
                StampPath = StampPath,
                StampImage = StampImage,
            };
        }

        public override void RestoreState(IShapeState state)
        {
            if (state is not DrawnMapComponentState s)
            {
                return;
            }

            TopLeft = s.TopLeft;
            Opacity = s.TextureOpacity;
            Scale = s.TextureScale;
            Rotation = s.Rotation;
            StampPath = s.StampPath;
            StampImage = s.StampImage;
        }
    }
}
