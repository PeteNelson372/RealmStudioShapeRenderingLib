using Clipper2Lib;
using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public static class Utilities
    {
        private const float PI_OVER_180 = (float)Math.PI / 180F;
        private const double SELECTION_FUZZINESS = 4;

        public static SKBitmap ResizeSKBitmap(SKBitmap bitmap, SKSizeI newsize)
        {
            SKBitmap resizedSKBitmap = new(newsize.Width, newsize.Height);
            bitmap.ScalePixels(resizedSKBitmap, SKSamplingOptions.Default);

            return resizedSKBitmap;
        }

        public static string MakeSafeFileName(string fileName)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                fileName =
                    fileName.Replace(c, '_');
            }

            return fileName.Trim();
        }

        public static List<SKPoint> PolyPoints(SKPoint location, float sides, float radius, float start)
        {
            List<SKPoint> points = [];

            float x_center = location.X;
            float y_center = location.Y;
            float angle = start;
            float angle_increment = (float)(2.0F * Math.PI / sides);

            for (int i = 0; i < sides; i++)
            {
                float x = (float)(x_center + radius * Math.Cos(angle));
                float y = (float)(y_center + radius * Math.Sin(angle));

                points.Add(new SKPoint(x, y));

                angle += angle_increment;
            }

            return points;
        }

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

        public static List<SKPoint> GetPointsInCircle(SKPoint cursorPoint, int radius, int stepSize)
        {
            if (radius <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(radius), "Argument must be positive.");
            }

            List<SKPoint> pointsInCircle = [];

            int minX = (int)Math.Max(0, cursorPoint.X - radius);
            int maxX = (int)(cursorPoint.X + radius);
            int minY = (int)Math.Max(0, cursorPoint.Y - radius);
            int maxY = (int)(cursorPoint.Y + radius);

            for (int i = minX; i <= maxX; i += stepSize)
            {
                for (int j = minY; j <= maxY; j += stepSize)
                {
                    SKPoint p = new(i, j);
                    if (PointInCircle(radius, cursorPoint, p))
                    {
                        pointsInCircle.Add(p);
                    }
                }
            }

            return pointsInCircle;
        }

        public static bool PointInCircle(float radius, SKPoint origin, SKPoint pointToTest)
        {
            float square_dist = SKPoint.DistanceSquared(origin, pointToTest);
            return square_dist < (radius * radius);
        }

        public static SKPath BuildPath(IReadOnlyList<SKPoint> points)
        {
            using SKPathBuilder pathBuilder = new();

            if (points == null || points.Count < 2)
            {
                return new SKPath();
            }

            pathBuilder.MoveTo(points[0]);

            for (int i = 1; i < points.Count; i++)
            {
                pathBuilder.LineTo(points[i]);
            }

            var path = pathBuilder.Snapshot();
            pathBuilder.Detach();

            return path;
        }

        public static SKPath ReversePath(SKPath path)
        {
            using SKPathBuilder pathBuilder = new();

            if (path.Points == null || path.IsEmpty || path.PointCount < 2)
            {
                return new SKPath();
            }

            pathBuilder.MoveTo(path.Points[^1]);

            for (int i = path.PointCount - 2; i >= 0; i--)
            {
                pathBuilder.LineTo(path.Points[i]);
            }

            var reversedPath = pathBuilder.Snapshot();
            pathBuilder.Detach();

            return reversedPath;
        }

        public static SKPath BuildClosedPath(IReadOnlyList<SKPoint> points)
        {
            using SKPathBuilder pathBuilder = new();

            if (points == null || points.Count < 2)
            {
                return new SKPath();
            }

            pathBuilder.MoveTo(points[0]);

            for (int i = 1; i < points.Count; i++)
            {
                pathBuilder.LineTo(points[i]);
            }

            pathBuilder.Close();

            var path = pathBuilder.Snapshot();
            pathBuilder.Detach();

            return path;
        }

        public static SKPath BuildPath2(IReadOnlyList<SKPoint> points)
        {
            using SKPathBuilder pathBuilder = new();

            if (points == null || points.Count < 2)
            {
                return new SKPath();
            }

            pathBuilder.MoveTo(points[0]);

            for (int j = 0; j < points.Count - 2; j += 3)
            {
                pathBuilder.CubicTo(points[j], points[j + 1], points[j + 2]);
            }

            var path = pathBuilder.Snapshot();
            pathBuilder.Detach();

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
            if (scale <= 0f)
            {
                return bitmap;
            }

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
            canvas.DrawBitmap(source, 0, 0, SKSamplingOptions.Default, paint);

            return result;
        }

        public static SKBitmap ResizeBitmap(SKBitmap source, int width, int height)
        {
            SKBitmap resized = new(width, height);

            using SKCanvas canvas = new(resized);

            SKSamplingOptions sampling = new(SKFilterMode.Linear, SKMipmapMode.Linear);

            canvas.DrawImage(
                SKImage.FromBitmap(source),
                new SKRect(0, 0, width, height),
                sampling);

            return resized;
        }

        public static SKBitmap BuildColorizedBrushBitmap(SKBitmap densityBrush, SKColor selectedColor)
        {
            //
            // NOTE:
            //
            // This implementation intentionally uses GetPixel()/SetPixel().
            //
            // Several attempts were made to replace it with direct pixel access
            // (Span<>, pointers, Gray8 access, etc.) for performance.
            //
            // Although those implementations were substantially faster, none
            // exactly reproduced Skia's behavior for all supported brush formats
            // (Gray8, RGBA, multi-bitmap brushes, premultiplied alpha, etc.).
            //
            // Since brush preparation is now performed asynchronously and after scaling, the
            // performance of this method is no longer user-visible, so correctness
            // is preferred.
            //

            SKBitmap result =
                new(
                    densityBrush.Width,
                    densityBrush.Height,
                    SKColorType.Rgba8888,
                    SKAlphaType.Premul);

            for (int y = 0; y < densityBrush.Height; y++)
            {
                for (int x = 0; x < densityBrush.Width; x++)
                {
                    SKColor brushPixel = densityBrush.GetPixel(x, y);

                    //
                    // Brush is grayscale:
                    // Black = full paint
                    // White = no paint
                    //

                    byte gray =
                        (byte)((brushPixel.Red +
                                brushPixel.Green +
                                brushPixel.Blue) / 3);

                    byte coverage = (byte)(255 - gray);

                    byte alpha = (byte)((selectedColor.Alpha * coverage) / 255);

                    result.SetPixel(x, y, new SKColor(
                            selectedColor.Red,
                            selectedColor.Green,
                            selectedColor.Blue,
                            alpha));
                }
            }

            return result;
        }

        public static SKBitmap ExtractRegion(
            SKBitmap source,
            int x,
            int y,
            int width,
            int height)
        {
            SKBitmap result = new(width, height);

            using SKCanvas canvas = new(result);

            SKRect sourceRect = new(
                x,
                y,
                x + width,
                y + height);

            SKRect destRect = new(
                0,
                0,
                width,
                height);

            canvas.DrawBitmap(
                source,
                sourceRect,
                destRect,
                SKSamplingOptions.Default);

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

            var newPathBuilder = new SKPathBuilder();

            foreach (var contour in contours)
            {
                var simplified = SimplifyContour([.. contour.Points]);

                if (simplified.Count < 3)
                    continue;

                newPathBuilder.MoveTo(simplified[0]);

                for (int i = 1; i < simplified.Count; i++)
                    newPathBuilder.LineTo(simplified[i]);

                newPathBuilder.Close();
            }

            var newPath = newPathBuilder.Snapshot();
            newPathBuilder.Detach();

            return newPath;
        }

        
        public static SKPath BuildOffsetPath(IReadOnlyList<SKPoint> pts, float offset)
        {
            var pathBuilder = new SKPathBuilder();

            if (pts.Count < 3)
                return new SKPath();

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

            pathBuilder.MoveTo(offsetPts[0]);

            for (int i = 1; i < offsetPts.Count; i++)
            {
                pathBuilder.LineTo(offsetPts[i]);
            }

            var path = pathBuilder.Snapshot();
            pathBuilder.Detach();

            return path;
        }

        public static SKPath BuildOffsetPath2(IReadOnlyList<SKPoint> points, float offset)
        {
            if (points.Count < 2)
                return new SKPath();

            List<SKPoint> offsetPoints =
                OffsetPoints(points, offset);

            return BuildPath(offsetPoints);
        }

        public static List<SKPoint> OffsetPoints(IReadOnlyList<SKPoint> points, float offset)
        {
            List<SKPoint> result = new(points.Count);

            if (points.Count < 2)
                return result;

            for (int i = 0; i < points.Count; i++)
            {
                SKPoint tangent;

                if (i == 0)
                {
                    tangent = points[1] - points[0];
                }
                else if (i == points.Count - 1)
                {
                    tangent = points[^1] - points[^2];
                }
                else
                {
                    // Use a wider stencil when possible for a smoother tangent.
                    int i0 = Math.Max(0, i - 2);
                    int i1 = Math.Min(points.Count - 1, i + 2);

                    tangent = points[i1] - points[i0];
                }

                float length = MathF.Sqrt(
                    tangent.X * tangent.X +
                    tangent.Y * tangent.Y);

                if (length < 1e-6f)
                {
                    result.Add(points[i]);
                    continue;
                }

                tangent = new SKPoint(
                    tangent.X / length,
                    tangent.Y / length);

                // Rotate tangent 90° to get the left normal.
                SKPoint normal = new(
                    -tangent.Y,
                     tangent.X);

                result.Add(new SKPoint(
                    points[i].X + normal.X * offset,
                    points[i].Y + normal.Y * offset));
            }

            return result;
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

            using SKPathBuilder strokePathBuilder = new();

            strokePaint.GetFillPath(centerPath, strokePathBuilder, 1.0f);
            using var outline = strokePathBuilder.Snapshot();
            strokePathBuilder.Detach();

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

            var pathBuilder = new SKPathBuilder();

            pathBuilder.MoveTo(-dx, -dy);
            pathBuilder.LineTo(0, 0);
            pathBuilder.LineTo(-dx, dy);

            var path = pathBuilder.Snapshot();
            pathBuilder.Detach();

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

                using var contourPathBuilder = new SKPathBuilder();

                const int sampleCount = 128; // increase if shapes are complex
                bool first = true;

                for (int i = 0; i < sampleCount; i++)
                {
                    float distance = length * i / (sampleCount - 1);

                    if (measure.GetPosition(distance, out var pt))
                    {
                        if (first)
                        {
                            contourPathBuilder.MoveTo(pt);
                            first = false;
                        }
                        else
                        {
                            contourPathBuilder.LineTo(pt);
                        }
                    }
                }

                contourPathBuilder.Close();

                var contourPath = contourPathBuilder.Snapshot();
                contourPathBuilder.Detach();

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
