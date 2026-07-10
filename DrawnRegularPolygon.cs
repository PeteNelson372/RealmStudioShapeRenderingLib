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
using RealmStudioX.WPF.EditorUtilities;
using SkiaSharp;
using System.Xml.Serialization;

namespace RealmStudioShapeRenderingLib
{
    public class DrawnRegularPolygon : MapComponent2D, IRectangularShape, IAlignable
    {
        private SKPoint _topLeft;
        private SKPoint _bottomRight;
        private int _sides = 5; // Default to pentagon
        private SKColor _polygonColor = SKColors.Black;
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

        private readonly SKPaint _polygonPaint = new()
        {
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Butt
        };

        private readonly SKPaint _fillPaint = new()
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

        [XmlIgnore]
        public SKColor PolygonColor
        {
            get => _polygonColor;
            set
            {
                _polygonColor = value;
                _shaderValuesModified = true;
            }
        }

        [XmlElement("PolygonColor")]
        public string PolygonColorXml
        {
            get => XmlColorConverter.Serialize(PolygonColor);
            set => PolygonColor = XmlColorConverter.Deserialize(value);
        }

        [XmlIgnore]
        public SKColor FillColor
        {
            get => _fillColor;
            set
            {
                _fillColor = value;
                _shaderValuesModified = true;
            }
        }

        [XmlElement("FillColor")]
        public string FillColorXml
        {
            get => XmlColorConverter.Serialize(FillColor);
            set => FillColor = XmlColorConverter.Deserialize(value);
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
        public int Sides
        {
            get => _sides;
            set
            {
                if (value < 3)
                    throw new ArgumentOutOfRangeException(nameof(Sides), "A polygon must have at least 3 sides.");
                _sides = value;
            }
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
            set
            {
                _fillImage = value;
                _shaderValuesModified = true;
            }
        }

        public override void Render(SKCanvas canvas, FontManager? fontManager = null, SKPath? clipPath = null)
        {
            _polygonPaint.Color = PolygonColor;
            _polygonPaint.StrokeWidth = BrushSize;

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

            SKRect rect = new(TopLeft.X, TopLeft.Y, BottomRight.X, BottomRight.Y);
            Bounds = rect;
            Bounds = SKRect.Inflate(Bounds, 2, 2);

            SKPoint centerPoint = rect.Location;

            List<SKPoint> points = Utilities.PolyPoints(TopLeft, Sides, rect.Width, 3.0F * (float)Math.PI / 2.0F);

            SKPath path = Utilities.BuildClosedPath(points);

            Bounds = path.Bounds;
            Bounds = SKRect.Inflate(Bounds, 2, 2);

            using SKAutoCanvasRestore autoRestore = new(canvas, true);
            if (Rotation != 0)
            {
                canvas.RotateDegrees(Rotation, Bounds.MidX, Bounds.MidY);
            }

            canvas.DrawPath(path, _fillPaint);
            canvas.DrawPath(path, _polygonPaint);
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
                BottomRight = BottomRight,
                ComponentColor = PolygonColor,
                FillColor = FillColor,
                TextureOpacity = TextureOpacity,
                TextureScale = TextureScale,
                BrushSize = BrushSize,
                Rotation = Rotation,
                FillType = FillType,
                FillImageId = FillImageId,
                FillImage = FillImage,
            };
        }

        public override void RestoreState(IShapeState state)
        {
            if (state is not DrawnMapComponentState s)
            {
                return;
            }

            TopLeft = s.TopLeft;
            BottomRight = s.BottomRight;
            PolygonColor = s.ComponentColor;
            FillColor = s.FillColor;
            TextureOpacity = s.TextureOpacity;
            TextureScale = s.TextureScale;
            BrushSize = s.BrushSize;
            Rotation = s.Rotation;
            FillType = s.FillType;
            FillImageId = s.FillImageId;
            FillImage = s.FillImage;
        }
    }
}