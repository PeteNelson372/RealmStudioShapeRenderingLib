namespace RealmStudioShapeRenderingLib
{
    using SkiaSharp;

    public static class RiverGeometryBuilder
    {
        public static RiverGeometry BuildRiverPolygon(
            IReadOnlyList<SKPoint> centerline,
            float width,
            bool sourceFade,
            float variationSeed)
        {
            centerline = Resample(centerline, width * 0.25f);

            int count = centerline.Count;
            var left = new List<SKPoint>(count);
            var right = new List<SKPoint>(count);

            if (count < 2)
            {
                return new RiverGeometry()
                {
                    Polygon = new SKPath(),
                    LeftBank = left,
                    RightBank = right,
                };
            }

            float halfBaseWidth = width * 0.5f;

            for (int i = 0; i < count; i++)
            {
                var p = centerline[i];

                var dirPrev = GetPrevDirection(centerline, i);
                var dirNext = GetNextDirection(centerline, i);

                var normalPrev = new SKPoint(-dirPrev.Y, dirPrev.X);
                var normalNext = new SKPoint(-dirNext.Y, dirNext.X);

                var bisector = Normalize(new SKPoint(
                    normalPrev.X + normalNext.X,
                    normalPrev.Y + normalNext.Y));

                float t = i / (float)(count - 1);

                float widthFactor = 1f;

                if (sourceFade)
                {
                    const float wideningExponent = 1.2f;
                    widthFactor = MathF.Pow(t, wideningExponent);
                    widthFactor = MathF.Max(widthFactor, 0.05f);
                }

                float curvature = 1f - Dot(dirPrev, dirNext);
                const float curvatureStrength = 0.25f;

                float curvatureFactor = 1f + curvature * curvatureStrength;

                float variation = MathF.Sin(i * 0.45f + variationSeed) * 0.08f;

                float variationFactor = Math.Clamp(1f + variation, 0.9f, 1.1f);

                float halfWidth = halfBaseWidth * widthFactor * curvatureFactor * variationFactor;

                float denom = Dot(bisector, normalNext);

                float scale;

                if (MathF.Abs(denom) < 0.25f || float.IsNaN(denom))
                {
                    // fallback to simple perpendicular offset
                    var dir = Normalize(new SKPoint(
                        dirPrev.X + dirNext.X,
                        dirPrev.Y + dirNext.Y));

                    var normal = new SKPoint(-dir.Y, dir.X);

                    var offset = new SKPoint(
                        normal.X * halfWidth,
                        normal.Y * halfWidth);

                    left.Add(new SKPoint(p.X + offset.X, p.Y + offset.Y));
                    right.Add(new SKPoint(p.X - offset.X, p.Y - offset.Y));
                }
                else
                {
                    scale = halfWidth / denom;

                    const float maxMiter = 2.5f;
                    scale = Math.Clamp(scale, -halfWidth * maxMiter, halfWidth * maxMiter);

                    var offset = new SKPoint(
                        bisector.X * scale,
                        bisector.Y * scale);

                    left.Add(new SKPoint(p.X + offset.X, p.Y + offset.Y));
                    right.Add(new SKPoint(p.X - offset.X, p.Y - offset.Y));
                }

            }

            var path = new SKPath();

            path.MoveTo(left[0]);

            for (int i = 1; i < left.Count; i++)
            {
                path.LineTo(left[i]);
            }

            for (int i = right.Count - 1; i >= 0; i--)
            {
                path.LineTo(right[i]);
            }

            path.Close();

            var returnGeometry = new RiverGeometry()
            {
                Polygon = new SKPath(path),
                LeftBank = left,
                RightBank = right,
            };

            return returnGeometry;
        }

        private static SKPoint GetPrevDirection(IReadOnlyList<SKPoint> pts, int i)
        {
            if (i == 0)
                return Normalize(new SKPoint(
                    pts[1].X - pts[0].X,
                    pts[1].Y - pts[0].Y));

            return Normalize(new SKPoint(
                pts[i].X - pts[i - 1].X,
                pts[i].Y - pts[i - 1].Y));
        }

        private static SKPoint GetNextDirection(IReadOnlyList<SKPoint> pts, int i)
        {
            if (i == pts.Count - 1)
                return Normalize(new SKPoint(
                    pts[i].X - pts[i - 1].X,
                    pts[i].Y - pts[i - 1].Y));

            return Normalize(new SKPoint(
                pts[i + 1].X - pts[i].X,
                pts[i + 1].Y - pts[i].Y));
        }

        private static SKPoint Normalize(SKPoint v)
        {
            float len = MathF.Sqrt(v.X * v.X + v.Y * v.Y);

            if (len < 0.00001f)
            {
                return new SKPoint(1, 0);
            }

            return new SKPoint(v.X / len, v.Y / len);
        }

        private static float Dot(SKPoint a, SKPoint b)
        {
            return a.X * b.X + a.Y * b.Y;
        }

        private static float DistanceToPath(SKPoint p, SKPath perimeter)
        {
            using var measure = new SKPathMeasure(perimeter);

            float length = measure.Length;

            float bestDist = float.MaxValue;

            const int samples = 64;

            for (int i = 0; i <= samples; i++)
            {
                float d = length * i / samples;

                measure.GetPosition(d, out var pos);

                float dx = pos.X - p.X;
                float dy = pos.Y - p.Y;

                float dist = dx * dx + dy * dy;

                if (dist < bestDist)
                    bestDist = dist;
            }

            return MathF.Sqrt(bestDist);
        }

        public static List<SKPoint> Resample(
            IReadOnlyList<SKPoint> pts,
            float spacing)
        {
            var result = new List<SKPoint>();

            if (pts.Count == 0)
                return result;

            result.Add(pts[0]);

            float accumulated = 0f;

            for (int i = 1; i < pts.Count; i++)
            {
                var a = pts[i - 1];
                var b = pts[i];

                float dx = b.X - a.X;
                float dy = b.Y - a.Y;

                float dist = MathF.Sqrt(dx * dx + dy * dy);

                if (dist <= 0.001f)
                    continue;

                while (accumulated + dist >= spacing)
                {
                    float t = (spacing - accumulated) / dist;

                    a = new SKPoint(
                        a.X + dx * t,
                        a.Y + dy * t);

                    result.Add(a);

                    dx = b.X - a.X;
                    dy = b.Y - a.Y;

                    dist = MathF.Sqrt(dx * dx + dy * dy);

                    accumulated = 0f;
                }

                accumulated += dist;
            }

            return result;
        }
    }

    public class RiverGeometry
    {
        public SKPath Polygon { get; set; } = new SKPath();
        public List<SKPoint> LeftBank { get; set; } = [];
        public List<SKPoint> RightBank { get; set; } = [];
    }
}
