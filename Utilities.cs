using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public static class Utilities
    {
        public static SKPath BuildPath(IReadOnlyList<SKPoint> points)
        {
            var path = new SKPath();

            if (points == null || points.Count < 2)
                return path;

            path.MoveTo(points[0]);

            for (int i = 1; i < points.Count; i++)
            {
                path.LineTo(points[i]);
            }

            return path;
        }

        public static SKPath BuildPath2(IReadOnlyList<SKPoint> points)
        {
            SKPath path = new();

            if (points == null || points.Count < 3)
                return path;

            path.MoveTo(points[0]);

            for (int j = 0; j < points.Count - 2; j += 3)
            {
                path.CubicTo(points[j], points[j + 1], points[j + 2]);
            }

            return path;
        }

        public static SKPoint Normalize(SKPoint v)
        {
            float len = MathF.Sqrt(v.X * v.X + v.Y * v.Y);
            if (len < 1e-5f) return new SKPoint(0, 0);
            return new SKPoint(v.X / len, v.Y / len);
        }

        public static float Clamp(float value, float min, float max)
        {
            return Math.Min(Math.Max(value, min), max);
        }

        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        public static SKColor LerpColor(SKColor a, SKColor b, float t)
        {
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;

            byte r = (byte)(a.Red + (b.Red - a.Red) * t);
            byte g = (byte)(a.Green + (b.Green - a.Green) * t);
            byte bch = (byte)(a.Blue + (b.Blue - a.Blue) * t);
            byte aCh = (byte)(a.Alpha + (b.Alpha - a.Alpha) * t);

            return new SKColor(r, g, bch, aCh);
        }

        public static SKPoint ComputeCentroid(SKPath path)
        {
            var bounds = path.Bounds;
            return new SKPoint(bounds.MidX, bounds.MidY);
        }

        // -------------------------------------------------
        // Helpers
        // -------------------------------------------------

        public static float Distance(SKPoint a, SKPoint b)
        {
            return SKPoint.Distance(a, b);
        }

        public static List<SKPoint> SimplifyContour(List<SKPoint> pts, float tolerance = 0.5f)
        {
            if (pts.Count < 3)
                return pts;

            var result = new List<SKPoint>
            {
                pts[0]
            };

            for (int i = 1; i < pts.Count - 1; i++)
            {
                var a = result[^1];
                var b = pts[i];
                var c = pts[i + 1];

                float area = MathF.Abs(
                    (b.X - a.X) * (c.Y - a.Y) -
                    (b.Y - a.Y) * (c.X - a.X));

                if (area > tolerance)
                    result.Add(b);
            }

            result.Add(pts[^1]);

            return result;
        }

        public static SKPath SimplifyPath(SKPath path)
        {
            var contours = ExtractContours(path);

            var newPath = new SKPath();

            foreach (var contour in contours)
            {
                var simplified = SimplifyContour([.. contour.Points]);

                if (simplified.Count < 3)
                    continue;

                newPath.MoveTo(simplified[0]);

                for (int i = 1; i < simplified.Count; i++)
                    newPath.LineTo(simplified[i]);

                newPath.Close();
            }

            return newPath;
        }

        public static List<SKPath> ExtractContours(SKPath path)
        {
            var result = new List<SKPath>();

            if (path == null || path.IsEmpty)
                return result;

            using var measure = new SKPathMeasure(path, false);

            do
            {
                float length = measure.Length;
                if (length <= 0f)
                    continue;

                var contourPath = new SKPath();

                const int sampleCount = 128; // increase if shapes are complex
                bool first = true;

                for (int i = 0; i < sampleCount; i++)
                {
                    float distance = length * i / (sampleCount - 1);

                    if (measure.GetPosition(distance, out var pt))
                    {
                        if (first)
                        {
                            contourPath.MoveTo(pt);
                            first = false;
                        }
                        else
                        {
                            contourPath.LineTo(pt);
                        }
                    }
                }

                contourPath.Close();
                result.Add(contourPath);

            } while (measure.NextContour());

            return result;
        }

        public static SKColor[] BuildWaterColorLUT(WaterRenderSettings settings)
        {
            int maxDepth = (int)settings.ShallowDepth;

            var lut = new SKColor[maxDepth + 1];

            for (int d = 0; d <= maxDepth; d++)
            {
                float t = (float)d / maxDepth;

                t = MathF.Sqrt(t);
                t = MathF.Pow(t, settings.DeepBias);

                if (d < settings.ShelfDepth)
                {
                    float shelfT = (float)d / settings.ShelfDepth;

                    var shelfColor = Utilities.LerpColor(
                        settings.ShallowWaterColor,
                        SKColors.White.WithAlpha(120),
                        0.35f);

                    lut[d] = Utilities.LerpColor(
                        shelfColor,
                        settings.ShallowWaterColor,
                        shelfT);
                }
                else
                {
                    lut[d] = Utilities.LerpColor(
                        settings.ShallowWaterColor,
                        settings.DeepWaterColor,
                        t);
                }
            }

            return lut;
        }
    }
}
