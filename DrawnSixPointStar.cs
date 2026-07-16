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
    public sealed class DrawnSixPointStar : MapComponent2D, ICenterRadiusShape, IAlignable, IRotatable
    {
        private SKPoint _center;
        private float _radius;
        private SKColor _starColor = SKColors.Black;
        private SKColor _fillColor = SKColors.Transparent;
        private float _textureOpacity = 1.0f;
        private float _textureScale = 1.0f;
        private int _brushSize = 2;
        private float _rotation;
        private DrawingFillType _fillType = DrawingFillType.None;
        private string _fillImageId = string.Empty;
        private SKImage? _fillImage;
        private SKShader? _fillShader;

        private bool _shaderValuesModified = true;

        private readonly SKPaint _starPaint = new()
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
        public SKPoint Center
        {
            get => _center;
            set => _center = value;
        }

        [XmlElement]
        public float Radius
        {
            get => _radius;
            set => _radius = value;
        }

        [XmlIgnore]
        public SKColor StarColor
        {
            get => _starColor;
            set
            {
                _starColor = value;
                _shaderValuesModified = true;
            }
        }

        [XmlElement("StarColor")]
        public string StarColorXml
        {
            get => XmlColorConverter.Serialize(StarColor);
            set => StarColor = XmlColorConverter.Deserialize(value);
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
        public int BrushSize
        {
            get => _brushSize;
            set => _brushSize = value;
        }

        [XmlElement]
        public float Rotation
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
            _starPaint.Color = StarColor;
            _starPaint.StrokeWidth = BrushSize;

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

            List<SKPoint> points = [];

            for (int degrees = 0; degrees < 360; degrees += 30)
            {
                float radians = (float)(degrees * Math.PI / 180.0);

                if (degrees % 60 != 0)
                {
                    // every 60 degrees, we draw a point at the outer radius
                    float x = Center.X + Radius * (float)Math.Cos(radians);
                    float y = Center.Y + Radius * (float)Math.Sin(radians);
                    points.Add(new SKPoint(x, y));
                }
                else
                {
                    // every 30 degrees, we draw a point at the inner radius
                    float innerRadius = Radius / 2.0F;
                    float x = Center.X + innerRadius * (float)Math.Cos(radians);
                    float y = Center.Y + innerRadius * (float)Math.Sin(radians);
                    points.Add(new SKPoint(x, y));
                }
            }

            using SKPath path = Utilities.BuildClosedPath(points);

            //path.MoveTo(points[0]);

            Bounds = path.Bounds;

            using SKAutoCanvasRestore autoRestore = new(canvas, true);
            if (Rotation != 0)
            {
                canvas.RotateDegrees(Rotation, Bounds.MidX, Bounds.MidY);
            }

            if (FillType != DrawingFillType.None)
            {
                // draw the filled star first if the fill is enabled
                canvas.DrawPath(path, _fillPaint);
            }

            canvas.DrawPath(path, _starPaint);
        }


        public override bool HitTest(SKPoint worldPos)
        {
            return Bounds.Contains(worldPos);
        }

        public override IShapeState CaptureState()
        {
            return new DrawnMapComponentState
            {
                Center = Center,
                Radius = Radius,
                ComponentColor = StarColor,
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

            Center = s.Center;
            Radius = s.Radius;
            StarColor = s.ComponentColor;
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
