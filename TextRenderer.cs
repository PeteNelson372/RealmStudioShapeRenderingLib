using SkiaSharp;
using SKRect = SkiaSharp.SKRect;

namespace RealmStudioShapeRenderingLib
{
    public static class TextRenderer
    {
        public static SKRect RenderLabelText(SKCanvas canvas, MapLabel label)
        {
            SKRect bounds = SKRect.Empty;

            if (label.RenderFont == null)
            {
                return SKRect.Empty;
            }

            using (new SKAutoCanvasRestore(canvas))
            {
                // Rotate around anchor (Location)
                if (Math.Abs(label.Rotation) > 0.001f)
                {
                    canvas.Translate(label.Location.X, label.Location.Y);
                    canvas.RotateDegrees(label.Rotation);
                    canvas.Translate(-label.Location.X, -label.Location.Y);
                }

                using var fillPaint = new SKPaint
                {
                    Color = label.FontColor,
                    IsAntialias = true
                };

                if (label.CurvePath != null)
                {
                    SKPath curvePath = new(label.CurvePath);
                    float offset = label.CurvePathOffset;

                    if (label.ReverseText)
                    {
                        curvePath = Utilities.ReversePath(label.CurvePath);
                    }

                    SKPath layoutPath = BuildTextLayoutPath(curvePath, label.RenderFont, label.Text, label.CurvePathOffset);

                    DrawOnPath(canvas, label, layoutPath);

                    // -------------------------------------------------
                    // Accurate CurveBounds via SKTextBlob
                    // -------------------------------------------------
                    using var blob = SKTextBlob.CreatePathPositioned(
                        label.Text,
                        label.RenderFont,
                        layoutPath,
                        SKTextAlign.Center,
                        new SKPoint(0, 0)
                    );

                    bounds = blob != null ? blob.Bounds : bounds;

                    if (label.CurveBounds != bounds)
                    {
                        label.CurveBounds = bounds;
                        label.BoundsModified = true;                        
                    }

                }
                else
                {
                    DrawStraight(canvas, label);

                    if (bounds.IsEmpty)
                    {
                        bounds = label.Bounds;
                    }
                }

                return bounds;
            }
        }

        
        private static void DrawOnPath(SKCanvas canvas, MapLabel label, SKPath layoutPath)
        {
            if (layoutPath == null || layoutPath.IsEmpty || label.RenderFont == null)
            {
                return;
            }
            
            if (label.GlowStrength > 0)
            {
                using var glow = new SKPaint
                {
                    Color = label.GlowColor,
                    IsAntialias = true,
                    MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, label.GlowStrength)
                };

                canvas.DrawTextOnPath(label.Text, layoutPath, new SKPoint(0, 0),
                    false, SKTextAlign.Center, label.RenderFont, glow);
            }

            if (label.OutlineWidth > 0)
            {
                using var stroke = new SKPaint
                {
                    Color = label.OutlineColor,
                    IsAntialias = true,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = label.OutlineWidth,
                    StrokeJoin = SKStrokeJoin.Round
                };
                canvas.DrawTextOnPath(label.Text, layoutPath, new SKPoint(0, 0),
                    false, SKTextAlign.Center, label.RenderFont, stroke);
            }

            using var fillPaint = new SKPaint
            {
                Color = label.FontColor,
                IsAntialias = true
            };

            canvas.DrawTextOnPath(label.Text, layoutPath, new SKPoint(0, 0),
                false, SKTextAlign.Center, label.RenderFont, fillPaint);
        }

        public static SKPath TranslatePathNormal(
            SKPath path,
            float offset,
            float labelLength)
        {
            if (Math.Abs(offset) < 0.01f)
                return path;

            using SKPathMeasure measure = new(path, false);

            float pathLength = measure.Length;

            //
            // Label is currently centered on the path.
            //
            float centerDistance = pathLength * 0.5f;

            //
            // Average tangent over approximately one-half the label.
            //
            float lookAhead = MathF.Min(labelLength * 0.25f, 40f);

            SKPoint average = SKPoint.Empty;
            int count = 0;

            for (float d = centerDistance - lookAhead;
                 d <= centerDistance + lookAhead;
                 d += 3f)
            {
                float sampleDistance = Math.Clamp(d, 0f, pathLength);

                if (measure.GetTangent(sampleDistance, out SKPoint tangent))
                {
                    average += tangent;
                    count++;
                }
            }

            if (count == 0)
                return path;

            average = new SKPoint(
                average.X / count,
                average.Y / count);

            float len = MathF.Sqrt(
                average.X * average.X +
                average.Y * average.Y);

            if (len < 1e-5f)
                return path;

            average = new SKPoint(
                average.X / len,
                average.Y / len);

            //
            // Left-hand normal.
            //
            SKPoint normal = new(
                -average.Y,
                 average.X);

            float dx = normal.X * offset;
            float dy = normal.Y * offset;

            return TranslatePath(path, dx, dy);
        }

        public static SKPath TranslatePath(SKPath path, float dx, float dy)
        {
            const float SampleSpacing = 3f;

            List<SKPoint> points = SamplePath(path, SampleSpacing);

            for (int i = 0; i < points.Count; i++)
            {
                points[i] = new SKPoint(
                    points[i].X + dx,
                    points[i].Y + dy);
            }

            return Utilities.BuildPath2(points);
        }


        private static void DrawStraight(SKCanvas canvas, MapLabel label)
        {
            float x = label.BaselineLocation.X;
            float y = label.BaselineLocation.Y;

            if (label.GlowStrength > 0)
            {
                using var glow = new SKPaint
                {
                    Color = label.GlowColor,
                    IsAntialias = true,
                    MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, label.GlowStrength)
                };
                canvas.DrawText(label.Text, x, y, SKTextAlign.Left,  label.RenderFont, glow);
            }

            if (label.OutlineWidth > 0)
            {
                using var stroke = new SKPaint
                {
                    Color = label.OutlineColor,
                    IsAntialias = true,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = label.OutlineWidth,
                    StrokeJoin = SKStrokeJoin.Round
                };
                canvas.DrawText(label.Text, x, y, SKTextAlign.Left, label.RenderFont, stroke);
            }

            using var fillPaint = new SKPaint
            {
                Color = label.FontColor,
                IsAntialias = true
            };

            canvas.DrawText(label.Text, x, y, SKTextAlign.Left, label.RenderFont, fillPaint);
        }

        public static SKPath BuildTextLayoutPath(SKPath path, SKFont font, string labelText, float labelOffset)
        {
            const float SampleSpacing = 3.0f;
            const float MaxGlyphAngle = 30.0f;
            const int MaxIterations = 15;

            List<SKPoint> points = SamplePath(path, SampleSpacing);

            float averageAdvance = labelText.Sum(c => font.MeasureText(c.ToString())) / labelText.Length;

            float angle = ComputeMaximumGlyphAngle(points, averageAdvance);

            int iterations = 0;

            while (angle > MaxGlyphAngle &&
                   iterations < MaxIterations)
            {
                points = Smooth(points, 0.35f);

                angle = ComputeMaximumGlyphAngle(points, averageAdvance);

                iterations++;
            }

            //Debug.WriteLine($"Maximum glyph angle = {angle:F1} after {iterations} smoothing passes.");

            SKPath layoutPath = Utilities.BuildPath2(points);

            if (Math.Abs(labelOffset) > 0.01f)
            {
                layoutPath = TranslatePathNormal(
                    layoutPath,
                    labelOffset,
                    averageAdvance);
            }

            return layoutPath;
        }

        private static List<SKPoint> Smooth(List<SKPoint> points, float alpha)
        {
            if (points.Count < 3)
                return [.. points];

            List<SKPoint> result = new(points.Count)
            {
                //
                // Keep endpoints fixed.
                //

                points[0]
            };

            for (int i = 1; i < points.Count - 1; i++)
            {
                SKPoint previous = points[i - 1];
                SKPoint current = points[i];
                SKPoint next = points[i + 1];

                SKPoint average = new(
                    (previous.X + next.X) * 0.5f,
                    (previous.Y + next.Y) * 0.5f);

                result.Add(new SKPoint(
                    current.X + alpha * (average.X - current.X),
                    current.Y + alpha * (average.Y - current.Y)));
            }

            result.Add(points[^1]);

            return result;
        }


        private static List<SKPoint> SamplePath(SKPath path, float spacing)
        {
            List<SKPoint> points = [];

            using SKPathMeasure measure = new(path, false);

            float length = measure.Length;

            for (float d = 0; d <= length; d += spacing)
            {
                if (measure.GetPosition(d, out SKPoint p))
                {
                    points.Add(p);
                }
            }

            // Ensure the endpoint is present.
            if (measure.GetPosition(length, out SKPoint end))
            {
                if (points.Count == 0 || points[^1] != end)
                {
                    points.Add(end);
                }
            }

            return points;
        }

        private static float ComputeMaximumGlyphAngle(IReadOnlyList<SKPoint> points, float glyphWidth)
        {
            if (points.Count < 3)
                return 0;

            float maxAngle = 0;

            for (int i = 1; i < points.Count - 1; i++)
            {
                int j = FindPointAtDistance(points, i, glyphWidth);

                if (j <= i)
                    break;

                SKPoint v1 = Normalize(points[i] - points[i - 1]);
                SKPoint v2 = Normalize(points[j] - points[j - 1]);

                float dot = Math.Clamp(
                    v1.X * v2.X + v1.Y * v2.Y,
                    -1.0f,
                     1.0f);

                float angle =
                    MathF.Acos(dot) * 180f / MathF.PI;

                maxAngle = Math.Max(maxAngle, angle);
            }

            return maxAngle;
        }

        private static int FindPointAtDistance(
            IReadOnlyList<SKPoint> points,
            int start,
            float distance)
        {
            float accumulated = 0;

            for (int i = start + 1; i < points.Count; i++)
            {
                accumulated += SKPoint.Distance(
                    points[i - 1],
                    points[i]);

                if (accumulated >= distance)
                    return i;
            }

            return points.Count - 1;
        }

        private static SKPoint Normalize(SKPoint v)
        {
            float length = MathF.Sqrt(v.X * v.X + v.Y * v.Y);

            if (length < 0.0001f)
                return SKPoint.Empty;

            return new SKPoint(
                v.X / length,
                v.Y / length);
        }
    }

}
