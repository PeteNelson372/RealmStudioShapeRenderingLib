using SkiaSharp;
using System.Diagnostics;

namespace RealmStudioShapeRenderingLib
{
    public static class PathRenderer
    {
        public static void Render(SKCanvas canvas, IReadOnlyList<SKPoint> points, PathRenderStyle style)
        {
            if (points.Count < 2)
                return;

            using var path = Utilities.BuildPath(points);

            switch (style.MapPathType)
            {
                case PathType.SolidLinePath:
                    RenderSolid(canvas, path, style);
                    break;

                case PathType.DottedLinePath:
                    RenderDotted(canvas, path, style);
                    break;

                case PathType.DashedLinePath:
                    RenderDashed(canvas, path, style);
                    break;

                case PathType.DashDotLinePath:
                    RenderDashDot(canvas, path, style);
                    break;

                case PathType.DashDotDotLinePath:
                    RenderDashDotDot(canvas, path, style);
                    break;

                case PathType.DoubleSolidBorderPath:
                    RenderDoubleLine(canvas, path, style);
                    break;

                case PathType.ChevronLinePath:
                    RenderChevron(canvas, points, style);
                    break;

                case PathType.LineAndDashesPath:
                    RenderLineAndDashes(canvas, points, style);
                    break;

                case PathType.ShortIrregularDashPath:
                    RenderIrregularDash(canvas, points, style);
                    break;

                case PathType.ThickSolidLinePath:
                    RenderThick(canvas, path, style);
                    break;

                case PathType.SolidBlackBorderPath:
                    RenderBordered(canvas, path, style);
                    break;

                case PathType.BorderedGradientPath:
                    RenderBorderedGradient(canvas, points, style);
                    break;

                case PathType.BorderedLightSolidPath:
                    RenderBorderedLight(canvas, points, style);
                    break;

                case PathType.BearTracksPath:
                case PathType.BirdTracksPath:
                case PathType.FootprintsPath:
                    RenderMarkers(canvas, points, style);
                    break;

                case PathType.RailroadTracksPath:
                    RenderRailroad(canvas, points, style);
                    break;

                case PathType.TexturedPath:
                    RenderTexture(canvas, path, style);
                    break;

                case PathType.BorderAndTexturePath:
                    RenderBorderTexture(canvas, path, style);
                    break;

                case PathType.RoundTowerWall:
                    RenderWall(canvas, points, style, true);
                    break;

                case PathType.SquareTowerWall:
                    RenderWall(canvas, points, style, false);
                    break;

                case PathType.SolidWall:
                    RenderSolidWall(canvas, path, style);
                    break;
            }
        }

        private static void RenderSolidWall(SKCanvas canvas, SKPath path, PathRenderStyle style)
        {
            throw new NotImplementedException();
        }

        private static void RenderWall(SKCanvas canvas, IReadOnlyList<SKPoint> points, PathRenderStyle style, bool v)
        {
            throw new NotImplementedException();
        }

        private static void RenderBorderTexture(
            SKCanvas canvas,
            SKPath path,
            PathRenderStyle style)
        {
            if (!style.UseTexture || style.Texture == null)
            {
                return;
            }

            // --- Draw path ---
            float outerWidth = style.Width;
            float innerWidth = style.Width - (style.BorderWidth * 2);

            using var borderPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = style.BorderColor,
                StrokeWidth = outerWidth,
                StrokeJoin = SKStrokeJoin.Round,
                StrokeCap = SKStrokeCap.Butt,
                IsAntialias = true
            };

            canvas.DrawPath(path, borderPaint);

            // --- Build texture fill path ---
            using var strokePaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = innerWidth,
                StrokeJoin = SKStrokeJoin.Round,
                StrokeCap = SKStrokeCap.Butt,
                IsAntialias = true
            };

            using var fillPath = new SKPath();
            strokePaint.GetFillPath(path, fillPath);

            // --- 2. Draw texture ---
            using var shader = SKShader.CreateBitmap(
                style.Texture,
                SKShaderTileMode.Repeat,
                SKShaderTileMode.Repeat);

            using var texturePaint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Shader = shader,
                IsAntialias = true
            };

            canvas.DrawPath(fillPath, texturePaint);


        }

        private static void RenderTexture(
            SKCanvas canvas,
            SKPath path,
            PathRenderStyle style)
        {
            if (!style.UseTexture || style.Texture == null)
            {
                return;
            }

            // --- 1. Stroke to fill geometry ---
            using var strokePaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = style.Width,
                StrokeJoin = SKStrokeJoin.Round,
                StrokeCap = SKStrokeCap.Butt,
                IsAntialias = true
            };

            using var fillPath = new SKPath();
            strokePaint.GetFillPath(path, fillPath);

            using var shader = SKShader.CreateBitmap(
                style.Texture,
                SKShaderTileMode.Repeat,
                SKShaderTileMode.Repeat);

            // --- 3. Paint ---
            using var paint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Shader = shader,
                //Color = SKColors.White.WithAlpha((byte)(style.TextureOpacity * 255)),
                IsAntialias = true
            };

            // --- 4. Draw ---
            canvas.DrawPath(fillPath, paint);
        }

        private static void RenderRailroad(
            SKCanvas canvas,
            IReadOnlyList<SKPoint> points,
            PathRenderStyle style)
        {
            if (points == null || points.Count < 2)
                return;

            style.RailOffset = style.Width * 0.5f;
            float offset = style.RailOffset;

            // --- Build rail paths ---
            using var leftRail = Utilities.BuildOffsetPath(points, offset);
            using var rightRail = Utilities.BuildOffsetPath(points, -offset);

            style.BorderWidth = style.Width * 0.2f;

            // --- Rail paint (thinner) ---
            using var railPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = style.BorderColor,
                StrokeWidth = style.BorderWidth,
                StrokeCap = SKStrokeCap.Butt,
                StrokeJoin = SKStrokeJoin.Bevel,
                IsAntialias = true
            };

            // --- Tie paint (thicker) ---
            using var tiePaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = style.Color,
                StrokeWidth = style.BorderWidth * 1.8f,
                StrokeCap = SKStrokeCap.Butt,
                IsAntialias = true
            };

            // --- Draw rails ---
            canvas.DrawPath(leftRail, railPaint);
            canvas.DrawPath(rightRail, railPaint);

            // --- Build center path for sampling ---
            using var centerPath = Utilities.BuildPath(points);
            using var measure = new SKPathMeasure(centerPath, false);

            float length = measure.Length;
            if (length <= 0)
                return;

            style.TieSpacing = style.Width * 2f;
            float spacing = style.TieSpacing;

            // ties extend slightly past rails
            style.TieOverhang = style.Width * 0.3f;
            float halfLength = offset + style.TieOverhang;

            // sampling delta for stable tangent
            float delta = MathF.Max(0.5f, spacing * 0.1f);

            // --- Draw ties ---
            float endClearance = offset + style.TieOverhang + style.BorderWidth;

            for (float d = endClearance; d < length - endClearance; d += spacing)
            {
                if (!measure.GetPosition(d, out var p0))
                    continue;

                float d2 = MathF.Min(d + delta, length);

                if (!measure.GetPosition(d2, out var p1))
                    continue;

                float dx = p1.X - p0.X;
                float dy = p1.Y - p0.Y;

                float len = MathF.Sqrt(dx * dx + dy * dy);
                if (len < 1e-5f)
                    continue;

                dx /= len;
                dy /= len;

                float nx = -dy;
                float ny = dx;

                var a = new SKPoint(
                    p0.X - nx * (offset + style.TieOverhang),
                    p0.Y - ny * (offset + style.TieOverhang));

                var b = new SKPoint(
                    p0.X + nx * (offset + style.TieOverhang),
                    p0.Y + ny * (offset + style.TieOverhang));

                canvas.DrawLine(a, b, tiePaint);
            }
        }

        private static void RenderMarkers(
            SKCanvas canvas,
            IReadOnlyList<SKPoint> points,
            PathRenderStyle style)
        {
            if (points == null || points.Count < 2)
                return;

            var picture = style.Marker;
            if (picture == null)
                return;

            var bounds = picture.CullRect;
            if (bounds.Height <= 0 || bounds.Width <= 0)
                return;

            using var path = Utilities.BuildPath2(points);
            using var measure = new SKPathMeasure(path, false);

            float length = measure.Length;
            if (length <= 0)
                return;

            // --- scale SVG to match path width ---
            float scale = style.Width / bounds.Height;

            float spacing = style.Width * style.MarkerSpacing;

            // center of SVG
            float cx = bounds.MidX;
            float cy = bounds.MidY;

            // stable tangent sampling
            float delta = MathF.Max(0.5f, spacing * 0.1f);

            for (float d = 0; d < length; d += spacing)
            {
                // --- stable tangent ---
                if (!measure.GetPosition(d, out var p0))
                    continue;

                float d2 = MathF.Min(d + delta, length);

                if (!measure.GetPosition(d2, out var p1))
                    continue;

                float dx = p1.X - p0.X;
                float dy = p1.Y - p0.Y;

                float len = MathF.Sqrt(dx * dx + dy * dy);
                if (len < 1e-5f)
                    continue;

                dx /= len;
                dy /= len;

                float angle = MathF.Atan2(dy, dx);

                using (new SKAutoCanvasRestore(canvas))
                {
                    // position on path
                    canvas.Translate(p0.X, p0.Y);

                    // align to path direction
                    canvas.RotateRadians(angle);

                    // scale to desired width
                    canvas.Scale(scale, scale);

                    // center the SVG on the path
                    canvas.Translate(-cx, -cy);

                    // draw marker
                    canvas.DrawPicture(picture);
                }
            }
        }

        private static void RenderBorderedLight(
            SKCanvas canvas,
            IReadOnlyList<SKPoint> points,
            PathRenderStyle style)
        {
            if (points == null || points.Count < 2)
                return;

            style.BorderWidth = style.Width * 0.2f;
            float borderWidth = style.BorderWidth;
            float totalWidth = style.Width;

            // --- 1. Fill band (lighter color) ---
            // Positioned midway inside the band so it spans correctly
            float fillOffset = borderWidth + (totalWidth * 0.5f);

            using (var fillPath = Utilities.BuildOffsetPath(points, fillOffset))
            using (var fillPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = Utilities.Lighten(style.Color, 0.35f).WithAlpha(192),   // lighter shade
                StrokeWidth = totalWidth,   // full band width
                StrokeCap = SKStrokeCap.Butt,
                StrokeJoin = SKStrokeJoin.Bevel,
                IsAntialias = true
            })
            {
                canvas.DrawPath(fillPath, fillPaint);
            }

            // --- 2. Border line (draw last, on top) ---
            using (var borderPath = Utilities.BuildOffsetPath(points, borderWidth))
            using (var borderPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = style.BorderColor,
                StrokeWidth = borderWidth,
                StrokeCap = SKStrokeCap.Butt,
                StrokeJoin = SKStrokeJoin.Round,
                IsAntialias = true
            })
            {
                canvas.DrawPath(borderPath, borderPaint);
            }
        }

        private static void RenderBorderedGradient(
            SKCanvas canvas,
            IReadOnlyList<SKPoint> points,
            PathRenderStyle style)
        {
            if (points == null || points.Count < 2)
                return;

            style.BorderWidth = style.Width * 0.2f;

            float borderWidth = style.BorderWidth;
            float totalWidth = style.Width;

            // Number of gradient bands
            int steps = Math.Max(12, (int)(totalWidth / 2f));
            float stepSize = totalWidth / steps;


            // --- 1. Draw gradient bands (left → right inward) ---
            for (int i = 0; i < steps; i++)
            {
                float t = i / (float)(steps - 1);   // 0 → 1

                // Offset from border inward
                float offset = borderWidth + (t * totalWidth);

                // Smooth falloff (quadratic)
                float alpha = MathF.Pow(1f - t, 1.5f);
                alpha *= alpha;

                byte a = (byte)(alpha * 255);

                using var path = Utilities.BuildOffsetPath(points, offset);

                using var paint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    Color = style.Color.WithAlpha(a),
                    StrokeWidth = stepSize + 1f,   // overlap to avoid gaps
                    StrokeCap = SKStrokeCap.Butt,
                    StrokeJoin = SKStrokeJoin.Round,
                    IsAntialias = true
                };

                canvas.DrawPath(path, paint);
            }

            // --- 2. Draw outer border line (left side) ---
            using (var borderPath = Utilities.BuildOffsetPath(points, borderWidth))
            using (var borderPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = style.BorderColor,
                StrokeWidth = borderWidth,
                StrokeCap = SKStrokeCap.Butt,
                StrokeJoin = SKStrokeJoin.Round,
                IsAntialias = true
            })
            {
                canvas.DrawPath(borderPath, borderPaint);
            }
        }

        private static void RenderBordered(SKCanvas canvas, SKPath path, PathRenderStyle style)
        {
            float outerWidth = style.Width;
            float innerWidth = style.Width - (style.BorderWidth * 2);

            // --- Outer border (black) ---
            using var borderPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = style.BorderColor,
                StrokeWidth = outerWidth,
                StrokeJoin = SKStrokeJoin.Round,
                StrokeCap = SKStrokeCap.Butt,
                IsAntialias = true
            };

            // --- Inner fill (path color) ---
            using var fillPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = style.Color,
                StrokeWidth = innerWidth,
                StrokeJoin = SKStrokeJoin.Round,
                StrokeCap = SKStrokeCap.Butt,
                IsAntialias = true
            };

            // draw order matters
            canvas.DrawPath(path, borderPaint);
            canvas.DrawPath(path, fillPaint);
        }

        private static void RenderThick(SKCanvas canvas, SKPath path, PathRenderStyle style)
        {
            using SKPaint solidPaint = PaintObjects.DashPaint.Clone();
            solidPaint.StrokeWidth = style.Width * 2f;
            solidPaint.Color = style.Color;

            canvas.DrawPath(path, solidPaint);
        }

        private static void RenderIrregularDash(SKCanvas canvas, IReadOnlyList<SKPoint> points, PathRenderStyle style)
        {
            using var path = Utilities.BuildPath(points);

            using var paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = style.Color,
                StrokeWidth = style.BorderWidth,
                StrokeCap = SKStrokeCap.Butt,
                IsAntialias = true
            };

            float spacing = style.Width * 2f;   // distance between bars
            float halfWidth = style.Width * 0.5f;

            using var measure = new SKPathMeasure(path, false);

            float length = measure.Length;

            for (float d = 0; d < length; d += spacing)
            {
                if (!measure.GetPositionAndTangent(d, out var pos, out var tan))
                    continue;

                // normalize tangent
                float len = MathF.Sqrt(tan.X * tan.X + tan.Y * tan.Y);
                if (len < 1e-5f)
                    continue;

                float tx = tan.X / len;
                float ty = tan.Y / len;

                // perpendicular (normal)
                float nx = -ty;
                float ny = tx;

                // endpoints of the bar
                var p0 = new SKPoint(
                    pos.X - nx * halfWidth,
                    pos.Y - ny * halfWidth);

                var p1 = new SKPoint(
                    pos.X + nx * halfWidth,
                    pos.Y + ny * halfWidth);

                canvas.DrawLine(p0, p1, paint);
            }
        }

        private static void RenderLineAndDashes(
           SKCanvas canvas,
           IReadOnlyList<SKPoint> points,
           PathRenderStyle style)
        {
            float offset = style.Width * 0.5f;

            using var solidPath = Utilities.BuildOffsetPath(points, offset);
            using var dashPath = Utilities.BuildOffsetPath(points, -offset);

            using var solidPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = style.Color,
                StrokeWidth = style.BorderWidth,
                StrokeCap = SKStrokeCap.Butt,
                StrokeJoin = SKStrokeJoin.Round,
                IsAntialias = true
            };

            using var dashPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = style.Color,
                StrokeWidth = style.BorderWidth,
                StrokeCap = SKStrokeCap.Butt,
                StrokeJoin = SKStrokeJoin.Round,
                IsAntialias = true,
                PathEffect = SKPathEffect.CreateDash(
                    [style.Width, style.Width], 0)
            };

            canvas.DrawPath(solidPath, solidPaint);
            canvas.DrawPath(dashPath, dashPaint);
        }

        private static void RenderChevron(
            SKCanvas canvas,
            IReadOnlyList<SKPoint> points,
            PathRenderStyle style)
        {
            using var path = Utilities.BuildPath(points);

            style.BorderWidth = style.Width * 0.5f;

            using var paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = style.Color,
                StrokeWidth = style.BorderWidth,
                StrokeCap = SKStrokeCap.Square,
                IsAntialias = true
            };

            style.ChevronSpacing = style.Width * 2.5f;
            float spacing = style.ChevronSpacing;
            float size = style.Width;

            using var chevron = Utilities.CreateChevron(size);

            using var measure = new SKPathMeasure(path, false);

            float length = measure.Length;

            for (float d = 0; d < length; d += spacing)
            {
                if (!measure.GetPositionAndTangent(d, out var pos, out var tan))
                    continue;

                float angle = MathF.Atan2(tan.Y, tan.X);

                var matrix = SKMatrix.CreateRotation(angle, 0, 0);

                matrix = matrix.PostConcat(SKMatrix.CreateTranslation(pos.X, pos.Y));

                using var transformed = new SKPath();
                chevron.Transform(matrix, transformed);

                canvas.DrawPath(transformed, paint);
            }
        }

        private static void RenderDoubleLine(SKCanvas canvas, SKPath path, PathRenderStyle style)
        {
            float outerWidth = style.Width;
            float innerWidth = style.Width - (style.BorderWidth * 2);

            var bounds = canvas.LocalClipBounds;

            using var outerPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = style.Color,
                StrokeWidth = outerWidth,
                StrokeJoin = SKStrokeJoin.Round,
                StrokeCap = SKStrokeCap.Butt,
                IsAntialias = true
            };

            using var clearPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                BlendMode = SKBlendMode.Clear,
                StrokeWidth = innerWidth,
                StrokeJoin = SKStrokeJoin.Round,
                StrokeCap = SKStrokeCap.Butt,
                IsAntialias = true
            };

            // isolate into a layer
            using (new SKAutoCanvasRestore(canvas, true))
            {
                canvas.SaveLayer(bounds, null);

                // draw thick outer stroke
                canvas.DrawPath(path, outerPaint);

                // punch out center
                canvas.DrawPath(path, clearPaint);

                // layer is composited back automatically
            }
        }

        private static void RenderDashDotDot(SKCanvas canvas, SKPath path, PathRenderStyle style)
        {
            float[] intervals = [style.Width * 2, style.Width * 2, 0, style.Width * 2, 0, style.Width * 2]; ;
            SKPathEffect pathLineEffect = SKPathEffect.CreateDash(intervals, 0);

            using SKPaint dashDotPaint = PaintObjects.DashPaint.Clone();
            dashDotPaint.StrokeWidth = style.Width;
            dashDotPaint.Color = style.Color;
            dashDotPaint.PathEffect = pathLineEffect;

            canvas.DrawPath(path, dashDotPaint);
        }

        private static void RenderDashDot(SKCanvas canvas, SKPath path, PathRenderStyle style)
        {
            float[] intervals = [style.Width * 2, style.Width * 2, 0, style.Width * 2];
            SKPathEffect pathLineEffect = SKPathEffect.CreateDash(intervals, 0);

            using SKPaint dashDotPaint = PaintObjects.DashPaint.Clone();
            dashDotPaint.StrokeWidth = style.Width;
            dashDotPaint.Color = style.Color;
            dashDotPaint.PathEffect = pathLineEffect;

            canvas.DrawPath(path, dashDotPaint);
        }

        private static void RenderDashed(SKCanvas canvas, SKPath path, PathRenderStyle style)
        {
            float[] intervals = [style.Width * 2, style.Width * 2];
            SKPathEffect pathLineEffect = SKPathEffect.CreateDash(intervals, 0);

            using SKPaint dashPaint = PaintObjects.DashPaint.Clone();
            dashPaint.StrokeWidth = style.Width;
            dashPaint.Color = style.Color;
            dashPaint.PathEffect = pathLineEffect;

            canvas.DrawPath(path, dashPaint);
        }

        private static void RenderDotted(SKCanvas canvas, SKPath path, PathRenderStyle style)
        {
            float[] intervals = [0, style.Width * 2];
            SKPathEffect pathLineEffect = SKPathEffect.CreateDash(intervals, 0);

            using SKPaint dottedPaint = PaintObjects.DashPaint.Clone();
            dottedPaint.StrokeWidth = style.Width;
            dottedPaint.Color = style.Color;
            dottedPaint.PathEffect = pathLineEffect;

            canvas.DrawPath(path, dottedPaint);
        }

        private static void RenderSolid(SKCanvas canvas, SKPath path, PathRenderStyle style)
        {
            using SKPaint solidPaint = PaintObjects.DashPaint.Clone();
            solidPaint.StrokeWidth = style.Width;
            solidPaint.Color = style.Color;

            canvas.DrawPath(path, solidPaint);
        }

    }
}
