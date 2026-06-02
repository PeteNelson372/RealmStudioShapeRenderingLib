using Clipper2Lib;
using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public static class Utilities
    {
        private const float PI_OVER_180 = (float)Math.PI / 180F;
        private const double SELECTION_FUZZINESS = 4;

        public static float CalculatePolygonArea(List<SKPoint> polygonPoints)
        {
            if (polygonPoints.Count < 3)
            {
                return 0;
            }

            // use shoelace algorithm to calculate area of the polygon
            float area = 0;

            int j = polygonPoints.Count - 1;
            for (int i = 0; i < polygonPoints.Count; i++)
            {
                area += (polygonPoints[j].X + polygonPoints[i].X) * (polygonPoints[j].Y - polygonPoints[i].Y);
                j = i;  // j is previous vertex to i
            }

            area = Math.Abs(area / 2.0F);

            return area;
        }

        public static SKPoint PointOnCircle(float radius, float angleInDegrees, SKPoint origin)
        {
            // Convert from degrees to radians via multiplication by PI/180
            float angleInRadians = angleInDegrees * PI_OVER_180;

            float x = (float)(radius * Math.Cos(angleInRadians)) + origin.X;
            float y = (float)(radius * Math.Sin(angleInRadians)) + origin.Y;

            return new SKPoint(x, y);
        }

        public static bool LineContainsPoint(SKPoint pointToCheck, SKPoint lineStartPoint, SKPoint lineEndPoint)
        {
            SKPoint leftPoint;
            SKPoint rightPoint;

            // Normalize start/end to left right to make the offset calc simpler.
            if (lineStartPoint.X <= lineEndPoint.X)
            {
                leftPoint = lineStartPoint;
                rightPoint = lineEndPoint;
            }
            else
            {
                leftPoint = lineEndPoint;
                rightPoint = lineStartPoint;
            }

            // If point is out of bounds, no need to do further checks.                  
            if (pointToCheck.X + SELECTION_FUZZINESS < Math.Min(leftPoint.X, rightPoint.X) || Math.Max(leftPoint.X, rightPoint.X) < pointToCheck.X - SELECTION_FUZZINESS)
            {
                return false;
            }
            else if (pointToCheck.Y + SELECTION_FUZZINESS < Math.Min(leftPoint.Y, rightPoint.Y) || Math.Max(leftPoint.Y, rightPoint.Y) < pointToCheck.Y - SELECTION_FUZZINESS)
            {
                return false;
            }

            double deltaX = rightPoint.X - leftPoint.X;
            double deltaY = rightPoint.Y - leftPoint.Y;

            // If the line is straight, the earlier boundary check is enough to determine that the point is on the line.
            // Also prevents division by zero exceptions.
            if (deltaX == 0 || deltaY == 0)
            {
                return true;
            }

            double slope = deltaY / deltaX;
            double offset = leftPoint.Y - leftPoint.X * slope;
            double calculatedY = pointToCheck.X * slope + offset;

            // Check calculated Y matches the points Y coord with some easing.
            bool lineContains = pointToCheck.Y - SELECTION_FUZZINESS <= calculatedY && calculatedY <= pointToCheck.Y + SELECTION_FUZZINESS;

            return lineContains;
        }

        public static SKPoint[] GetPoints(uint quantity, SKPoint p1, SKPoint p2)
        {
            var points = new SKPoint[quantity];

            double distance = SKPoint.Distance(p1, p2);

            points[0] = p1;
            points[quantity - 1] = p2;

            double tdelta = distance / quantity;

            for (int i = 1; i < quantity; i++)
            {
                double t = (i * tdelta) / distance;
                points[i].X = (int)Math.Ceiling((1 - t) * p1.X + t * p2.X);
                points[i].Y = (int)Math.Ceiling((1 - t) * p1.Y + t * p2.Y);
            }

            return points;
        }

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

        public static SKPath BuildClosedPath(IReadOnlyList<SKPoint> points)
        {
            var path = new SKPath();

            if (points == null || points.Count < 2)
                return path;

            path.MoveTo(points[0]);

            for (int i = 1; i < points.Count; i++)
            {
                path.LineTo(points[i]);
            }

            //path.LineTo(points[0]);

            path.Close();

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

        public static SymbolFileFormat InferFileFormat(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();

            return ext switch
            {
                ".png" => SymbolFileFormat.PNG,
                ".jpg" or ".jpeg" => SymbolFileFormat.JPG,
                ".bmp" => SymbolFileFormat.BMP,
                ".svg" => SymbolFileFormat.Vector,
                _ => SymbolFileFormat.NotSet
            };
        }

        public static string NormalizePath(string path)
        {
            return Path.GetFullPath(path)
                .Replace('\\', '/')
                .ToLowerInvariant();
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

        public static SKPoint InterpolatePoint(SKPoint a, SKPoint b, float t)
        {
            return new SKPoint(
                a.X + ((b.X - a.X) * t),
                a.Y + ((b.Y - a.Y) * t));
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

        private const float MinLightness = 0.15f;
        private const float MaxLightness = 0.85f;

        public static SKColor Darken(SKColor c, float amount)
        {
            amount = Math.Clamp(amount, 0f, 1f);

            float factor = 1f - amount;

            return new SKColor(
                (byte)(c.Red * factor),
                (byte)(c.Green * factor),
                (byte)(c.Blue * factor),
                (byte)(c.Alpha));
        }

        public static SKColor Lighten(SKColor c, float amount)
        {
            amount = Math.Clamp(amount, 0f, 1f);

            return new SKColor(
                (byte)(c.Red + (255 - c.Red) * amount),
                (byte)(c.Green + (255 - c.Green) * amount),
                (byte)(c.Blue + (255 - c.Blue) * amount),
                (byte)(c.Alpha));
        }

        public static SKBitmap ScaleSKBitmap(SKBitmap bitmap, float scale)
        {
            if (bitmap == null || scale <= 0f)
                throw new ArgumentException("Invalid bitmap or scale.");

            int width = Math.Max(1, (int)Math.Round(bitmap.Width * scale));
            int height = Math.Max(1, (int)Math.Round(bitmap.Height * scale));

            var result = new SKBitmap(width, height, bitmap.ColorType, bitmap.AlphaType);

            bitmap.ScalePixels(result, SKSamplingOptions.Default);

            return result;
        }

        public static SKBitmap SetBitmapOpacity(SKBitmap source, float opacity)
        {
            ArgumentNullException.ThrowIfNull(source);

            if (opacity == 1)
            {
                return source.Copy();
            }

            opacity = Clamp(opacity, 0f, 1f);

            var result = new SKBitmap(source.Width, source.Height, source.ColorType, source.AlphaType);

            using var canvas = new SKCanvas(result);
            canvas.Clear(SKColors.Transparent);

            // --- Color matrix (same idea as GDI+) ---
            float[] matrix =
            {
                1, 0, 0, 0, 0,   // R
                0, 1, 0, 0, 0,   // G
                0, 0, 1, 0, 0,   // B
                0, 0, 0, opacity, 0   // A (Matrix33 equivalent)
            };

            using var paint = new SKPaint
            {
                IsAntialias = false,                
                ColorFilter = SKColorFilter.CreateColorMatrix(matrix)
            };

            // draw original into new bitmap with opacity applied
            canvas.DrawBitmap(source, 0, 0, paint);

            return result;
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

        
        public static SKPath BuildOffsetPath(IReadOnlyList<SKPoint> pts, float offset)
        {
            var path = new SKPath();

            if (pts.Count < 3)
                return path;

            var offsetPts = new List<SKPoint>(pts.Count);

            for (int i = 1; i < pts.Count - 1; i++)
            {
                var n0 = GetNormal(pts[i - 1], pts[i]);
                var n1 = GetNormal(pts[i], pts[i + 1]);

                var normal = new SKPoint(n0.X + n1.X, n0.Y + n1.Y);

                float len = MathF.Sqrt(normal.X * normal.X + normal.Y * normal.Y);
                
                if (len > 1e-5f)
                {
                    normal = new SKPoint(normal.X / len, normal.Y / len);
                }

                offsetPts.Add(new SKPoint(
                    pts[i].X + normal.X * offset,
                    pts[i].Y + normal.Y * offset));
            }

            path.MoveTo(offsetPts[0]);

            for (int i = 1; i < offsetPts.Count; i++)
            {
                path.LineTo(offsetPts[i]);
            }

            return path;
        }

        internal static List<SKPoint> GetParallelRegionPoints(List<SKPoint> points, float distance, ParallelDirection location)
        {
            PathD clipperPath = [];

            foreach (SKPoint point in points)
            {
                clipperPath.Add(new PointD(point.X, point.Y));
            }

            PathsD clipperPaths = [];

            clipperPaths.Add(clipperPath);

            float d = (location == ParallelDirection.Below) ? -distance : distance;

            // offset polyline
            PathsD inflatedPaths = Clipper.InflatePaths(clipperPaths, d, JoinType.Round, EndType.Polygon, miterLimit: 1.0, arcTolerance: 0.05);

            if (inflatedPaths.Count > 0)
            {
                PathD inflatedPathD = inflatedPaths.First();

                List<SKPoint> inflatedPath = [];

                foreach (PointD p in inflatedPathD)
                {
                    inflatedPath.Add(new SKPoint((float)p.x, (float)p.y));
                }

                return inflatedPath;
            }
            else
            {
                return points;
            }
        }

        public static SKPoint GetNormal(SKPoint a, SKPoint b)
        {
            float dx = b.X - a.X;
            float dy = b.Y - a.Y;

            float len = MathF.Sqrt(dx * dx + dy * dy);
            if (len < 1e-5f)
                return new SKPoint(0, 0);

            dx /= len;
            dy /= len;

            // perpendicular
            return new SKPoint(-dy, dx);
        }

        public static void ExtractStrokeEdges(
            SKPath centerPath,
            float offset,
            PathRenderStyle style,
            out SKPath leftPath,
            out SKPath rightPath)
        {
            using var strokePaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = offset * 2,
                StrokeJoin = SKStrokeJoin.Round,
                StrokeCap = SKStrokeCap.Butt,
                IsAntialias = true
            };

            using var outline = new SKPath();
            strokePaint.GetFillPath(centerPath, outline);

            var contour = ExtractContour(outline);

            SplitOutline(contour, out var leftPts, out var rightPts);

            leftPath = BuildPath(leftPts);
            rightPath = BuildPath(rightPts);
        }

        public static SKPath CreateChevron(float size, float angleDeg = 45f)
        {
            float angle = MathF.PI * angleDeg / 180f;

            float dx = MathF.Cos(angle) * size;
            float dy = MathF.Sin(angle) * size;

            var path = new SKPath();

            path.MoveTo(-dx, -dy);
            path.LineTo(0, 0);
            path.LineTo(-dx, dy);

            return path;
        }

        // returns a list of SKPoints
        public static List<SKPoint> ExtractContour(SKPath outline)
        {
            var pts = new List<SKPoint>();

            using var iter = outline.CreateRawIterator();
            var p = new SKPoint[4];

            while (true)
            {
                var verb = iter.Next(p);
                if (verb == SKPathVerb.Done)
                    break;

                switch (verb)
                {
                    case SKPathVerb.Move:
                        pts.Add(p[0]);
                        break;

                    case SKPathVerb.Line:
                        pts.Add(p[1]);
                        break;

                    case SKPathVerb.Quad:
                        pts.Add(p[2]);
                        break;

                    case SKPathVerb.Cubic:
                        pts.Add(p[3]);
                        break;
                }
            }

            return pts;
        }

        public static void SplitOutline(
            List<SKPoint> contour,
            out List<SKPoint> left,
            out List<SKPoint> right)
        {
            left = new List<SKPoint>();
            right = new List<SKPoint>();

            if (contour.Count < 4)
                return;

            // The outline is a loop:
            // first half = one side
            // second half = other side (reversed)

            int half = contour.Count / 2;

            for (int i = 0; i < half; i++)
                left.Add(contour[i]);

            for (int i = contour.Count - 1; i >= half; i--)
                right.Add(contour[i]);
        }

        // returns a list of SKPaths
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
