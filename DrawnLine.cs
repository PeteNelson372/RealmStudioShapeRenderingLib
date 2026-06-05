using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public sealed class DrawnLine : MapComponent2D, IDrawnMapComponent
    {
        private List<SKPoint> _points = [];
        private SKColor _color = SKColors.Black;
        private int _brushSize = 2;
        private bool _drawTexture = false;
        private string _textureId = string.Empty;
        private SKImage? _texture = null;
        private int _textureOpacity = 255;
        private float _textureScale = 1.0f;

        private bool _shaderValuesModified = true;

        private readonly SKPaint ShaderPaint = new()
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
            StrokeMiter = 1.0f
        };

        public List<SKPoint> Points
        {
            get => _points;
            set
            {
                _points = value ?? throw new ArgumentNullException(nameof(value), "Points cannot be null.");
            }
        }

        public SKColor Color
        {
            get => _color;
            set
            {
                _color = value;
                _shaderValuesModified = true;
            }
        }

        public int BrushSize
        {
            get => _brushSize;
            set
            {
                _brushSize = value;
            }
        }

        public bool DrawTexture
        {
            get => _drawTexture;
            set
            {
                _drawTexture = value;
                _shaderValuesModified = true;
            }
        }

        public string TextureId
        {
            get => _textureId;
            set
            {
                _textureId = value ?? throw new ArgumentNullException(nameof(value), "TextureId cannot be null.");
                _shaderValuesModified = true;
            }
        }

        public SKImage? Texture
        {
            get => _texture;
            set
            {
                _texture = value;
                _shaderValuesModified = true;
            }
        }

        public int TextureOpacity
        {
            get => _textureOpacity;
            set
            {
                _textureOpacity = value;
                _shaderValuesModified = true;
            }
        }

        public float TextureScale
        {
            get => _textureScale;
            set
            {
                _textureScale = value;
                _shaderValuesModified = true;
            }
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

        public override void Render(SKCanvas canvas, FontManager? fontManager = null)
        {
            if (Points.Count < 2)
            {
                return;
            }

            SKPath path = Utilities.BuildPath(Points);

            path.GetBounds(out SKRect bounds);

            Bounds = bounds;

            ShaderPaint.Color = Color;
            ShaderPaint.StrokeWidth = BrushSize;
            ShaderPaint.StrokeCap = SKStrokeCap.Round;

            if (_shaderValuesModified)
            {
                SKShader StrokeShader = SKShader.CreateColor(Color);

                if (DrawTexture && Texture != null)
                {
                    using SKBitmap scaledTexture = Utilities.ScaleSKBitmap(SKBitmap.FromImage(Texture), TextureScale);
                    using SKBitmap textureBitmap = Utilities.SetBitmapOpacity(scaledTexture, TextureOpacity / 255f);

                    // combine the stroke color with the bitmap color
                    ShaderPaint.ColorFilter = SKColorFilter.CreateBlendMode(Color, SKBlendMode.Modulate);

                    // if the fill type is texture, we need to create a shader from the bitmap
                    StrokeShader = SKShader.CreateBitmap(textureBitmap, SKShaderTileMode.Repeat, SKShaderTileMode.Repeat);
                }

                ShaderPaint.Shader = StrokeShader;
                _shaderValuesModified = false;
            }

            canvas.DrawPath(path, ShaderPaint);
            path.Dispose();
        }
    }
}
