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

            centerline = ApplyMeanderNoise(centerline, width, variationSeed);

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

                var bisectorRaw = new SKPoint(
                    normalPrev.X + normalNext.X,
                    normalPrev.Y + normalNext.Y);

                var bisector = IsNearlyZero(bisectorRaw)
                    ? normalNext
                    : Normalize(bisectorRaw);

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

                float v1 = MathF.Sin(i * 0.18f + variationSeed) * 0.16f;
                float v2 = MathF.Sin(i * 0.90f + variationSeed * 1.9f) * 0.09f;
                float v3 = MathF.Sin(i * 3.20f + variationSeed * 3.4f) * 0.05f;
                float v4 = MathF.Sin(i * 7.50f + variationSeed * 5.1f) * 0.025f;

                float amp = 0.85f + 0.35f * MathF.Sin(i * 0.11f + variationSeed * 0.6f);

                float variationFactor = 1f + (v1 + v2 + v3 + v4) * amp;
                variationFactor = Math.Clamp(variationFactor, 0.72f, 1.35f);

                float halfWidth = halfBaseWidth * widthFactor * curvatureFactor * variationFactor;

                float asym = MathF.Sin(i * 1.35f + variationSeed * 4.7f) * 0.10f;

                float leftHalfWidth = halfWidth * (1f + asym);
                float rightHalfWidth = halfWidth * (1f - asym);

                float denom = Dot(bisector, normalNext);

                float scale;

                if (MathF.Abs(denom) < 0.25f || float.IsNaN(denom))
                {
                    var dir = Normalize(new SKPoint(
                        dirPrev.X + dirNext.X,
                        dirPrev.Y + dirNext.Y));

                    var normal = new SKPoint(-dir.Y, dir.X);

                    var leftOffset = new SKPoint(
                        normal.X * leftHalfWidth,
                        normal.Y * leftHalfWidth);

                    var rightOffset = new SKPoint(
                        normal.X * rightHalfWidth,
                        normal.Y * rightHalfWidth);

                    left.Add(new SKPoint(p.X + leftOffset.X, p.Y + leftOffset.Y));
                    right.Add(new SKPoint(p.X - rightOffset.X, p.Y - rightOffset.Y));
                }
                else
                {
                    scale = halfWidth / denom;

                    const float maxMiter = 2.5f;

                    if (MathF.Abs(scale) > halfWidth * maxMiter)
                    {
                        var leftOffset = new SKPoint(
                            normalNext.X * leftHalfWidth,
                            normalNext.Y * leftHalfWidth);

                        var rightOffset = new SKPoint(
                            normalNext.X * rightHalfWidth,
                            normalNext.Y * rightHalfWidth);

                        left.Add(new SKPoint(p.X + leftOffset.X, p.Y + leftOffset.Y));
                        right.Add(new SKPoint(p.X - rightOffset.X, p.Y - rightOffset.Y));
                    }
                    else
                    {
                        float leftScale = leftHalfWidth / denom;
                        float rightScale = rightHalfWidth / denom;

                        leftScale = Math.Clamp(leftScale, -leftHalfWidth * maxMiter, leftHalfWidth * maxMiter);
                        rightScale = Math.Clamp(rightScale, -rightHalfWidth * maxMiter, rightHalfWidth * maxMiter);

                        var leftOffset = new SKPoint(
                            bisector.X * leftScale,
                            bisector.Y * leftScale);

                        var rightOffset = new SKPoint(
                            bisector.X * rightScale,
                            bisector.Y * rightScale);

                        left.Add(new SKPoint(p.X + leftOffset.X, p.Y + leftOffset.Y));
                        right.Add(new SKPoint(p.X - rightOffset.X, p.Y - rightOffset.Y));
                    }
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

        public static List<SKPoint> ApplyMeanderNoise(
            IReadOnlyList<SKPoint> centerline,
            float width,
            float seed)
        {
            int count = centerline.Count;
            var result = new List<SKPoint>(count);

            float dist = 0f;

            for (int i = 0; i < count; i++)
            {
                var p = centerline[i];

                if (i > 0)
                    dist += SKPoint.Distance(centerline[i - 1], centerline[i]);

                var dirPrev = GetPrevDirection(centerline, i);
                var dirNext = GetNextDirection(centerline, i);

                var dir = Normalize(new SKPoint(
                    dirPrev.X + dirNext.X,
                    dirPrev.Y + dirNext.Y));

                var normal = new SKPoint(-dir.Y, dir.X);

                // wavelength scales with river width
                float s = dist / width;

                float n1 = MathF.Sin(s * 0.30f + seed) * 0.9f;
                float n2 = MathF.Sin(s * 0.90f + seed * 1.7f) * 0.35f;
                float n3 = MathF.Sin(s * 2.50f + seed * 3.3f) * 0.15f;

                float noise = n1 + n2 + n3;

                float offset = noise * width * 0.6f;

                var displaced = new SKPoint(
                    p.X + normal.X * offset,
                    p.Y + normal.Y * offset);

                result.Add(displaced);
            }

            return result;
        }

        /*
        public static List<SKPoint> ApplyMeanderNoise(
            IReadOnlyList<SKPoint> centerline,
            float width,
            float seed)
        {
            int count = centerline.Count;

            var result = new List<SKPoint>(count);

            for (int i = 0; i < count; i++)
            {
                var p = centerline[i];

                var dirPrev = GetPrevDirection(centerline, i);
                var dirNext = GetNextDirection(centerline, i);

                var dir = Normalize(new SKPoint(
                    dirPrev.X + dirNext.X,
                    dirPrev.Y + dirNext.Y));

                var normal = new SKPoint(-dir.Y, dir.X);

                // layered smooth noise
                float n1 = MathF.Sin(i * 0.18f + seed) * 0.8f;
                float n2 = MathF.Sin(i * 0.55f + seed * 1.7f) * 0.35f;
                float n3 = MathF.Sin(i * 1.7f + seed * 3.1f) * 0.15f;

                float noise = n1 + n2 + n3;

                // displacement proportional to river width
                float offset = noise * width * 0.4f;

                var displaced = new SKPoint(
                    p.X + normal.X * offset,
                    p.Y + normal.Y * offset);

                result.Add(displaced);
            }

            return result;
        }
        */

        static float LengthSquared(SKPoint v)
        {
            return v.X * v.X + v.Y * v.Y;
        }

        static bool IsNearlyZero(SKPoint v)
        {
            return LengthSquared(v) < 1e-6f;
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

        public static SKPoint Normalize(SKPoint v)
        {
            float len = MathF.Sqrt(v.X * v.X + v.Y * v.Y);

            if (len < 0.00001f)
            {
                return new SKPoint(1, 0);
            }

            return new SKPoint(v.X / len, v.Y / len);
        }

        public static float Dot(SKPoint a, SKPoint b)
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
