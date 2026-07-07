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
    public sealed class DrawnFivePointStar : MapComponent2D, IDrawnMapComponent
    {
        private SKPoint _center;
        private float _radius;
        private SKColor _starColor = SKColors.Black;
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

            SKPoint p1 = new(Radius * 0.94783F + Center.X, Radius * -0.31878F + Center.Y);
            SKPoint p2 = new(Radius * 0.22132F + Center.X, Radius * -0.31131F + Center.Y);

            SKPoint p3 = new(Radius * -0.01028F + Center.X, Radius * -0.99995F + Center.Y);
            SKPoint p4 = new(Radius * -0.22768F + Center.X, Radius * -0.30669F + Center.Y);

            SKPoint p5 = new(Radius * -0.95418F + Center.X, Radius * -0.29922F + Center.Y);
            SKPoint p6 = new(Radius * -0.36204F + Center.X, Radius * 0.12176F + Center.Y);

            SKPoint p7 = new(Radius * -0.57943F + Center.X, Radius * 0.81502F + Center.Y);
            SKPoint p8 = new(Radius * 0.00393F + Center.X, Radius * 0.38195F + Center.Y);

            SKPoint p9 = new(Radius * 0.59607F + Center.X, Radius * 0.80293F + Center.Y);
            SKPoint p10 = new(Radius * 0.36447F + Center.X, Radius * 0.11429F + Center.Y);

            SKPoint p11 = new(Radius * 0.94783F + Center.X, Radius * -0.31878F + Center.Y);


            using SKPath path = new();
            path.MoveTo(p1);
            path.LineTo(p2);
            path.LineTo(p3);
            path.LineTo(p4);
            path.LineTo(p5);
            path.LineTo(p6);
            path.LineTo(p7);
            path.LineTo(p8);
            path.LineTo(p9);
            path.LineTo(p10);
            path.LineTo(p11);
            path.Close();

            path.GetBounds(out SKRect bounds);
            Bounds = bounds;
            Bounds = SKRect.Inflate(Bounds, 2, 2);

            using SKAutoCanvasRestore autoRestore = new(canvas, true);
            if (Rotation != 0)
            {
                canvas.RotateDegrees(Rotation, Bounds.MidX, Bounds.MidY);
            }

            if (FillType != DrawingFillType.None)
            {
                // draw the filled diamond first if the fill is enabled
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
            throw new NotImplementedException();
        }

        public override void RestoreState(IShapeState state)
        {
            throw new NotImplementedException();
        }
    }
}
