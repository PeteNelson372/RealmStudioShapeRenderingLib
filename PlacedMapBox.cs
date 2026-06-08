using SkiaSharp;
using System.Xml.Serialization;

namespace RealmStudioShapeRenderingLib
{
    public class PlacedMapBox : MapComponent2D, ITransformable2D
    {
        [XmlElement]
        public MapBox? BaseBox;

        [XmlIgnore]
        public SKBitmap? BoxBitmap { get; set; }

        [XmlElement]
        public SKColor BoxTint { get; set; } = SKColors.White;

        // 9-patch center region in SOURCE bitmap coordinates
        [XmlElement]
        public float BoxCenterLeft { get; set; }

        [XmlElement]
        public float BoxCenterTop { get; set; }

        [XmlElement]
        public float BoxCenterRight { get; set; }

        [XmlElement]
        public float BoxCenterBottom { get; set; }

        private SKPoint _topLeft;
        private SKPoint _bottomRight;

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
        public float Rotation { get; set; }

        [XmlElement]
        public float Scale { get; set; } = 1f;

        [XmlElement]
        public bool Mirror { get; set; }

        private SKPoint _startTopLeft;
        private SKPoint _startBottomRight;

        public PlacedMapBox()
        {
        }

        public PlacedMapBox(PlacedMapBox box)
        {
            BaseBox = box.BaseBox;

            BoxBitmap = box.BoxBitmap?.Copy();

            TopLeft = box.TopLeft;
            BottomRight = box.BottomRight;

            Rotation = box.Rotation;

            Scale = box.Scale;

            Mirror = box.Mirror;

            BoxTint = box.BoxTint;

            BoxCenterLeft = box.BoxCenterLeft;
            BoxCenterTop = box.BoxCenterTop;
            BoxCenterRight = box.BoxCenterRight;
            BoxCenterBottom = box.BoxCenterBottom;
        }

        /// <summary>
        /// World-space center/pivot point.
        /// </summary>
        public SKPoint Location
        {
            get
            {
                return new SKPoint(
                    (_topLeft.X + _bottomRight.X) * 0.5f,
                    (_topLeft.Y + _bottomRight.Y) * 0.5f);
            }

            set
            {
                SKPoint currentCenter = Location;

                float dx = value.X - currentCenter.X;
                float dy = value.Y - currentCenter.Y;

                _topLeft = new SKPoint(
                    _topLeft.X + dx,
                    _topLeft.Y + dy);

                _bottomRight = new SKPoint(
                    _bottomRight.X + dx,
                    _bottomRight.Y + dy);
            }
        }

        /// <summary>
        /// Unscaled local size.
        /// </summary>
        public SKSize Size
        {
            get
            {
                return new SKSize(
                    Math.Abs(_bottomRight.X - _topLeft.X),
                    Math.Abs(_bottomRight.Y - _topLeft.Y));
            }

            set
            {
                SKPoint center = Location;

                float halfWidth = value.Width * 0.5f;
                float halfHeight = value.Height * 0.5f;

                _topLeft = new SKPoint(
                    center.X - halfWidth,
                    center.Y - halfHeight);

                _bottomRight = new SKPoint(
                    center.X + halfWidth,
                    center.Y + halfHeight);
            }
        }

        /// <summary>
        /// Local-space bounds centered at (0,0).
        /// </summary>
        public override SKRect LocalBounds =>
            new(
                -Size.Width / 2f,
                -Size.Height / 2f,
                 Size.Width / 2f,
                 Size.Height / 2f);

        public SKRect GetLocalBounds()
        {
            return LocalBounds;
        }

        /// <summary>
        /// Axis-aligned world-space bounds.
        /// </summary>
        public override SKRect Bounds =>
            new(
                Math.Min(_topLeft.X, _bottomRight.X),
                Math.Min(_topLeft.Y, _bottomRight.Y),
                Math.Max(_topLeft.X, _bottomRight.X),
                Math.Max(_topLeft.Y, _bottomRight.Y));

        public void SetBoxBitmap(SKBitmap bitmap)
        {
            BoxBitmap = bitmap;
        }

        public override void Render(
            SKCanvas canvas,
            FontManager? fontManager = null)
        {
            if (BoxBitmap == null)
                return;

            canvas.Save();

            // -------------------------------------------------
            // Transform into object space
            // -------------------------------------------------

            canvas.Translate(Location);

            if (Mirror)
            {
                canvas.Scale(-1f, 1f);
            }

            canvas.Scale(Scale);

            canvas.RotateDegrees(Rotation);

            // -------------------------------------------------
            // Draw 9-patch into centered local rect
            // -------------------------------------------------

            SKRect destRect = LocalBounds;

            using SKPaint boxPaint = new()
            {
                Style = SKPaintStyle.Fill,

                ColorFilter =
                    SKColorFilter.CreateBlendMode(
                        BoxTint,
                        SKBlendMode.Modulate)
            };

            canvas.DrawBitmapNinePatch(
                BoxBitmap,
                new SKRectI(
                    (int)BoxCenterLeft,
                    (int)BoxCenterTop,
                    (int)BoxCenterRight,
                    (int)BoxCenterBottom),
                destRect,
                boxPaint);

            canvas.Restore();
        }

        public override bool HitTest(SKPoint worldPos)
        {
            return Bounds.Contains(worldPos);
        }

        public SKMatrix GetTransform()
        {
            SKMatrix matrix = SKMatrix.Identity;

            if (Mirror)
            {
                matrix = matrix.PostConcat(
                    SKMatrix.CreateScale(-1f, 1f));
            }

            matrix = matrix.PostConcat(
                SKMatrix.CreateScale(Scale, Scale));

            matrix = matrix.PostConcat(
                SKMatrix.CreateRotationDegrees(Rotation));

            matrix = matrix.PostConcat(
                SKMatrix.CreateTranslation(
                    Location.X,
                    Location.Y));

            return matrix;
        }

        public SKPoint[] GetTransformedCorners()
        {
            SKMatrix transform = GetTransform();

            SKRect r = LocalBounds;

            return
            [
                transform.MapPoint(
                    new SKPoint(r.Left, r.Top)),

                transform.MapPoint(
                    new SKPoint(r.Right, r.Top)),

                transform.MapPoint(
                    new SKPoint(r.Right, r.Bottom)),

                transform.MapPoint(
                    new SKPoint(r.Left, r.Bottom))
            ];
        }

        public void BeginScale()
        {
            _startTopLeft = TopLeft;
            _startBottomRight = BottomRight;
        }

        public void ApplyScale(float factor)
        {
            SKPoint center = Location;

            float startWidth =
                _startBottomRight.X - _startTopLeft.X;

            float startHeight =
                _startBottomRight.Y - _startTopLeft.Y;

            float scaledHalfWidth =
                (startWidth * factor) * 0.5f;

            float scaledHalfHeight =
                (startHeight * factor) * 0.5f;

            TopLeft = new SKPoint(
                center.X - scaledHalfWidth,
                center.Y - scaledHalfHeight);

            BottomRight = new SKPoint(
                center.X + scaledHalfWidth,
                center.Y + scaledHalfHeight);
        }

        public override IShapeState CaptureState()
        {
            return new PlacedBoxState()
            {
                BaseBox = BaseBox,

                BoxBitmap = BoxBitmap?.Copy(),

                TopLeft = TopLeft,

                BottomRight = BottomRight,

                Rotation = Rotation,

                Scale = Scale,

                Mirror = Mirror,

                BoxTint = BoxTint,

                BoxCenterLeft = BoxCenterLeft,
                BoxCenterTop = BoxCenterTop,
                BoxCenterRight = BoxCenterRight,
                BoxCenterBottom = BoxCenterBottom,
            };
        }

        public override void RestoreState(IShapeState state)
        {
            if (state is not PlacedBoxState s)
                return;

            BaseBox = s.BaseBox;

            BoxBitmap = s.BoxBitmap?.Copy();

            TopLeft = s.TopLeft;

            BottomRight = s.BottomRight;

            Rotation = s.Rotation;

            Scale = s.Scale;

            Mirror = s.Mirror;

            BoxTint = s.BoxTint;

            BoxCenterLeft = s.BoxCenterLeft;
            BoxCenterTop = s.BoxCenterTop;
            BoxCenterRight = s.BoxCenterRight;
            BoxCenterBottom = s.BoxCenterBottom;
        }


    }
}