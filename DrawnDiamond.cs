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
    public sealed class DrawnDiamond : MapComponent2D, IDrawnMapComponent
    {
        private SKPoint _topLeft;
        private SKPoint _bottomRight;
        private SKColor _color = SKColors.Black;
        private SKColor _fillColor = SKColors.Transparent;
        private float _textureOpacity = 1.0f;
        private float _textureScale = 1.0f;
        private int _brushSize = 2;
        private int _rotation;
        private DrawingFillType _fillType = DrawingFillType.None;
        private string _fillImageId = string.Empty;
        private SKImage? _fillImage;
        private SKShader? _fillShader;

        private bool _shaderValuesModified = true;

        private SKPaint _diamondPaint = new()
        {
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Butt
        };

        private SKPaint _fillPaint = new()
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        [XmlElement]
        public SKPoint TopLeft
        {
            get => _topLeft;
            set => _topLeft = value;
        }

        [XmlElement]
        public SKPoint BottomRight
        {
            get => _bottomRight;
            set => _bottomRight = value;
        }

        [XmlElement]
        public SKColor Color
        {
            get => _color;
            set
            {
                _color = value;
                _shaderValuesModified = true;
            }
        }

        [XmlElement]
        public SKColor FillColor
        {
            get => _fillColor;
            set
            {
                _fillColor = value;
                _shaderValuesModified = true;
            }
        }

        [XmlElement]
        public float TextureOpacity
        {
            get => _textureOpacity;
            set
            {
                _textureOpacity = value;
                _shaderValuesModified = true;
            }
        }

        [XmlElement]
        public float TextureScale
        {
            get => _textureScale;
            set
            {
                _textureScale = value;
                _shaderValuesModified = true;
            }
        }

        [XmlElement]
        public int BrushSize
        {
            get => _brushSize;
            set => _brushSize = value;
        }

        [XmlElement]
        public int Rotation
        {
            get => _rotation;
            set => _rotation = value;
        }

        [XmlElement]
        public DrawingFillType FillType
        {
            get => _fillType;
            set => _fillType = value;
        }

        [XmlElement]
        public string FillImageId
        {
            get => _fillImageId;
            set
            {
                _fillImageId = value ?? throw new ArgumentNullException(nameof(value), "FillImageId cannot be null.");
                _shaderValuesModified = true;
            }
        }

        [XmlIgnore]
        public SKImage? FillImage
        {
            get => _fillImage;
            set => _fillImage = value;
        }


        public override void Render(SKCanvas canvas, FontManager? fontManager = null)
        {
            SKRect rect = new(TopLeft.X, TopLeft.Y, BottomRight.X, BottomRight.Y);
            Bounds = rect;

            _diamondPaint.Color = Color;
            _diamondPaint.StrokeWidth = BrushSize;

            if (FillType == DrawingFillType.Texture && FillImage != null)
            {
                if (_shaderValuesModified)
                {
                    _fillShader?.Dispose();

                    using SKBitmap scaledTexture = Utilities.ScaleSKBitmap(SKBitmap.FromImage(FillImage), TextureScale);
                    using SKBitmap textureBitmap = Utilities.SetBitmapOpacity(scaledTexture, TextureOpacity / 255f);

                    _fillShader = SKShader.CreateBitmap(textureBitmap, SKShaderTileMode.Repeat, SKShaderTileMode.Repeat);

                    _shaderValuesModified = false;
                }
                ;

                _fillPaint.Shader = _fillShader;
                _fillPaint.Style = SKPaintStyle.StrokeAndFill;
            }
            else if (FillType == DrawingFillType.Color)
            {
                _fillPaint.Color = FillColor;
                _fillPaint.Style = SKPaintStyle.StrokeAndFill;
            }
            else
            {
                _fillPaint.Style = SKPaintStyle.Stroke;
            }

            using SKAutoCanvasRestore autoRestore = new(canvas, true);

            if (Rotation != 0)
            {
                canvas.RotateDegrees(Rotation, (_topLeft.X + _bottomRight.X) / 2, (_topLeft.Y + _bottomRight.Y) / 2);
            }

            // draw the diamond

            SKPoint p1 = new((TopLeft.X + BottomRight.X) / 2, TopLeft.Y);
            SKPoint p2 = new(BottomRight.X, (TopLeft.Y + BottomRight.Y) / 2);
            SKPoint p3 = new((TopLeft.X + BottomRight.X) / 2, BottomRight.Y);
            SKPoint p4 = new(TopLeft.X, (TopLeft.Y + BottomRight.Y) / 2);

            using SKPath path = new();
            path.MoveTo(p1);
            path.LineTo(p2);
            path.LineTo(p3);
            path.LineTo(p4);
            path.Close();

            if (FillType != DrawingFillType.None)
            {
                // draw the filled diamond first if the fill is enabled
                canvas.DrawPath(path, _fillPaint);
            }

            canvas.DrawPath(path, _diamondPaint);
        }

        public override bool HitTest(SKPoint worldPos)
        {
            return Bounds.Contains(worldPos);
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
