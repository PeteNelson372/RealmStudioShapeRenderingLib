using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public static class RegionRenderer
    {
        public static void Render(SKCanvas canvas, IReadOnlyList<SKPoint> points, RegionRenderStyle style)
        {
            if (points.Count < 3)
                return;

            using var path = Utilities.BuildClosedPath(points);

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

                case PathType.LineAndDashesPath:
                    RenderLineAndDashes(canvas, path, style);
                    break;

                case PathType.SolidBlackBorderPath:
                    RenderBordered(canvas, path, style);
                    break;

                case PathType.BorderedGradientPath:
                    RenderBorderedGradient(canvas, path, style);
                    break;

                case PathType.BorderedLightSolidPath:
                    RenderBorderedLight(canvas, path, style);
                    break;
            }
        }


        private static void RenderBorderedLight(
            SKCanvas canvas,
            SKPath path,
            RegionRenderStyle style)
        {
            SKColor innerColor = style.BorderColor.WithAlpha((byte)style.Opacity);

            SKPaint regionInnerPaint = new()
            {
                Color = innerColor,
                Style = SKPaintStyle.Fill,
            };

            if (style.Smoothing > 0)
            {
                regionInnerPaint.PathEffect = SKPathEffect.CreateCorner(style.Smoothing);
            }

            canvas.DrawPath(path, regionInnerPaint);

            using SKPaint borderPaint = new()
            {
                Color = SKColors.Black,
                StrokeWidth = style.BorderWidth * 0.2F,
                Style = SKPaintStyle.Stroke,
                StrokeCap = SKStrokeCap.Round,
                StrokeJoin = SKStrokeJoin.Round,
                IsAntialias = true,
            };

            if (style.Smoothing > 0)
            {
                borderPaint.PathEffect = SKPathEffect.CreateCorner(style.Smoothing);
            }

            canvas.DrawPath(path, borderPaint);

            using SKPaint linePaint1 = new()
            {
                StrokeWidth = style.BorderWidth * 0.8F,
                Style = SKPaintStyle.Stroke,
                StrokeCap = SKStrokeCap.Round,
                StrokeJoin = SKStrokeJoin.Round,
                IsAntialias = true,
            };

            if (style.Smoothing > 0)
            {
                linePaint1.PathEffect = SKPathEffect.CreateCorner(style.Smoothing);
            }

            SKColor clr = new(style.BorderColor.Red, style.BorderColor.Green, style.BorderColor.Blue, 102);
            linePaint1.Color = clr;

            List<SKPoint> parallelPoints = Utilities.GetParallelRegionPoints([.. path.Points], style.BorderWidth * 0.2F, ParallelDirection.Below);
            using SKPath p1 = Utilities.BuildClosedPath(parallelPoints);

            canvas.DrawPath(p1, linePaint1);
        }

        private static void RenderBorderedGradient(
            SKCanvas canvas,
            SKPath path,
            RegionRenderStyle style)
        {

            using SKPaint gradientPaint = new()
            {
                StrokeWidth = style.BorderWidth,
                Color = style.BorderColor,
                Style = SKPaintStyle.Stroke,
                StrokeCap = SKStrokeCap.Round,
                StrokeJoin = SKStrokeJoin.Round,
                IsAntialias = true,
                PathEffect = SKPathEffect.CreateCorner(style.Smoothing)
            };

            SKColor innerColor = style.BorderColor.WithAlpha((byte)style.Opacity);

            using SKPaint regionInnerPaint = new()
            {
                Color = innerColor,
                Style = SKPaintStyle.Fill,
                PathEffect = SKPathEffect.CreateCorner(style.Smoothing)
            };

            canvas.DrawPath(path, regionInnerPaint);

            gradientPaint.StrokeWidth = style.BorderWidth * 0.2F;
            gradientPaint.Color = SKColors.Black;
            canvas.DrawPath(path, gradientPaint);

            gradientPaint.StrokeWidth = style.BorderWidth * 0.05F;

            gradientPaint.Color = style.BorderColor.WithAlpha(160);
            List<SKPoint> parallelPoints = Utilities.GetParallelRegionPoints([.. path.Points], style.BorderWidth * 0.05F, ParallelDirection.Below);
            using SKPath p1 = Utilities.BuildClosedPath(parallelPoints);
            canvas.DrawPath(p1, gradientPaint);

            gradientPaint.Color = style.BorderColor.WithAlpha(150);
            parallelPoints = Utilities.GetParallelRegionPoints([.. path.Points], style.BorderWidth * 0.1F, ParallelDirection.Below);
            using SKPath p2 = Utilities.BuildClosedPath(parallelPoints);
            canvas.DrawPath(p2, gradientPaint);

            gradientPaint.Color = style.BorderColor.WithAlpha(140);
            parallelPoints = Utilities.GetParallelRegionPoints([.. path.Points], style.BorderWidth * 0.15F, ParallelDirection.Below);
            using SKPath p3 = Utilities.BuildClosedPath(parallelPoints);
            canvas.DrawPath(p3, gradientPaint);

            gradientPaint.Color = style.BorderColor.WithAlpha(130);
            parallelPoints = Utilities.GetParallelRegionPoints([.. path.Points], style.BorderWidth * 0.2F, ParallelDirection.Below);
            using SKPath p4 = Utilities.BuildClosedPath(parallelPoints);
            canvas.DrawPath(p4, gradientPaint);

            gradientPaint.Color = style.BorderColor.WithAlpha(120);
            parallelPoints = Utilities.GetParallelRegionPoints([.. path.Points], style.BorderWidth * 0.25F, ParallelDirection.Below);
            using SKPath p5 = Utilities.BuildClosedPath(parallelPoints);
            canvas.DrawPath(p5, gradientPaint);

            gradientPaint.Color = style.BorderColor.WithAlpha(110);
            parallelPoints = Utilities.GetParallelRegionPoints([.. path.Points], style.BorderWidth * 0.3F, ParallelDirection.Below);
            using SKPath p6 = Utilities.BuildClosedPath(parallelPoints);
            canvas.DrawPath(p6, gradientPaint);

            gradientPaint.Color = style.BorderColor.WithAlpha(100);
            parallelPoints = Utilities.GetParallelRegionPoints([.. path.Points], style.BorderWidth * 0.35F, ParallelDirection.Below);
            using SKPath p7 = Utilities.BuildClosedPath(parallelPoints);
            canvas.DrawPath(p7, gradientPaint);

            gradientPaint.Color = style.BorderColor.WithAlpha(90);
            parallelPoints = Utilities.GetParallelRegionPoints([.. path.Points], style.BorderWidth * 0.4F, ParallelDirection.Below);
            using SKPath p8 = Utilities.BuildClosedPath(parallelPoints);
            canvas.DrawPath(p8, gradientPaint);

            gradientPaint.Color = style.BorderColor.WithAlpha(80);
            parallelPoints = Utilities.GetParallelRegionPoints([.. path.Points], style.BorderWidth * 0.45F, ParallelDirection.Below);
            using SKPath p9 = Utilities.BuildClosedPath(parallelPoints);
            canvas.DrawPath(p9, gradientPaint);

            gradientPaint.Color = style.BorderColor.WithAlpha(70);
            parallelPoints = Utilities.GetParallelRegionPoints([.. path.Points], style.BorderWidth * 0.5F, ParallelDirection.Below);
            using SKPath p10 = Utilities.BuildClosedPath(parallelPoints);
            canvas.DrawPath(p10, gradientPaint);

            gradientPaint.Color = style.BorderColor.WithAlpha(60);
            parallelPoints = Utilities.GetParallelRegionPoints([.. path.Points], style.BorderWidth * 0.55F, ParallelDirection.Below);
            using SKPath p11 = Utilities.BuildClosedPath(parallelPoints);
            canvas.DrawPath(p11, gradientPaint);

            gradientPaint.Color = style.BorderColor.WithAlpha(50);
            parallelPoints = Utilities.GetParallelRegionPoints([.. path.Points], style.BorderWidth * 0.6F, ParallelDirection.Below);
            using SKPath p12 = Utilities.BuildClosedPath(parallelPoints);
            canvas.DrawPath(p12, gradientPaint);

            gradientPaint.Color = style.BorderColor.WithAlpha(40);
            parallelPoints = Utilities.GetParallelRegionPoints([.. path.Points], style.BorderWidth * 0.65F, ParallelDirection.Below);
            using SKPath p13 = Utilities.BuildClosedPath(parallelPoints);
            canvas.DrawPath(p13, gradientPaint);

            gradientPaint.Color = style.BorderColor.WithAlpha(30);
            parallelPoints = Utilities.GetParallelRegionPoints([.. path.Points], style.BorderWidth * 0.7F, ParallelDirection.Below);
            using SKPath p14 = Utilities.BuildClosedPath(parallelPoints);
            canvas.DrawPath(p14, gradientPaint);

            gradientPaint.Color = style.BorderColor.WithAlpha(20);
            parallelPoints = Utilities.GetParallelRegionPoints([.. path.Points], style.BorderWidth * 0.75F, ParallelDirection.Below);
            using SKPath p15 = Utilities.BuildClosedPath(parallelPoints);
            canvas.DrawPath(p15, gradientPaint);
        }

        private static void RenderBordered(SKCanvas canvas, SKPath path, RegionRenderStyle style)
        {
            // --- Outer border (black) ---
            using var borderPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Black,
                StrokeWidth = style.BorderWidth * 0.2f,
                StrokeJoin = SKStrokeJoin.Round,
                StrokeCap = SKStrokeCap.Butt,
                IsAntialias = true,
                PathEffect = SKPathEffect.CreateCorner(style.Smoothing)
            };

            // --- Inner fill (path color) ---
            using var fillPaint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = style.Color,
                IsAntialias = true,
                PathEffect = SKPathEffect.CreateCorner(style.Smoothing)
            };

            List<SKPoint> parallelPoints = Utilities.GetParallelRegionPoints([.. path.Points], style.BorderWidth * 0.5f, ParallelDirection.Above);
            using SKPath p1 = Utilities.BuildClosedPath(parallelPoints);

            parallelPoints = Utilities.GetParallelRegionPoints([.. path.Points], style.BorderWidth * 0.5f, ParallelDirection.Below);
            using SKPath p2 = Utilities.BuildClosedPath(parallelPoints);


            // drawing order is critical here to achieve the desired visual effect
            canvas.DrawPath(p1, fillPaint);

            // have to substitute lightening for opacity, since high opacity just lets the color
            // drawn first to show through, so the effect of a border between the black
            // lines is lost
            fillPaint.Color = Utilities.Lighten(style.Color, (255f - style.Opacity) / 255f);

            canvas.DrawPath(p2, fillPaint);

            canvas.DrawPath(p1, borderPaint);

            canvas.DrawPath(p2, borderPaint);


        }

        private static void RenderLineAndDashes(
           SKCanvas canvas,
           SKPath path,
           RegionRenderStyle style)
        {
            SKColor innerColor = style.BorderColor.WithAlpha((byte)style.Opacity);

            using SKPaint regionInnerPaint = new()
            {
                Color = innerColor,
                Style = SKPaintStyle.Fill,
                PathEffect = SKPathEffect.CreateCorner(style.Smoothing)
            };

            canvas.DrawPath(path, regionInnerPaint);

            using var solidPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = style.Color,
                StrokeWidth = style.BorderWidth * 0.2f,
                StrokeCap = SKStrokeCap.Butt,
                StrokeJoin = SKStrokeJoin.Round,
                IsAntialias = true,
                PathEffect = SKPathEffect.CreateCorner(style.Smoothing)
            };

            List<SKPoint> parallelPoints = Utilities.GetParallelRegionPoints([.. path.Points], style.BorderWidth * 0.75f, ParallelDirection.Above);
            using SKPath p1 = Utilities.BuildClosedPath(parallelPoints);

            canvas.DrawPath(path, solidPaint);

            parallelPoints = Utilities.GetParallelRegionPoints([.. path.Points], style.BorderWidth * 0.75f, ParallelDirection.Below);
            using SKPath p2 = Utilities.BuildClosedPath(parallelPoints);

            using var dashPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = style.Color,
                StrokeWidth = style.BorderWidth * 0.2f,
                StrokeCap = SKStrokeCap.Butt,
                StrokeJoin = SKStrokeJoin.Round,
                IsAntialias = true,
                PathEffect = SKPathEffect.CreateDash([style.Width, style.Width], 0)
            };

            if (style.Smoothing > 0)
            {
                dashPaint.PathEffect = SKPathEffect.CreateCompose(SKPathEffect.CreateCorner(style.Smoothing), dashPaint.PathEffect);
            }

            canvas.DrawPath(p2, dashPaint);
        }

        private static void RenderDoubleLine(SKCanvas canvas, SKPath path, RegionRenderStyle style)
        {
            SKColor innerColor = style.BorderColor.WithAlpha((byte)style.Opacity);

            using SKPaint regionInnerPaint = new()
            {
                Color = innerColor,
                Style = SKPaintStyle.Fill,
                PathEffect = SKPathEffect.CreateCorner(style.Smoothing)
            };

            using SKPaint borderPaint = new()
            {
                Color = SKColors.Black,
                StrokeWidth = style.BorderWidth * 0.2F,
                Style = SKPaintStyle.Stroke,
                StrokeCap = SKStrokeCap.Round,
                StrokeJoin = SKStrokeJoin.Round,
                IsAntialias = true,
                PathEffect = SKPathEffect.CreateCorner(style.Smoothing)
            };

            List<SKPoint> parallelPoints = Utilities.GetParallelRegionPoints([.. path.Points], style.BorderWidth, ParallelDirection.Below);
            using SKPath p1 = Utilities.BuildClosedPath(parallelPoints);

            canvas.DrawPath(p1, regionInnerPaint);

            canvas.DrawPath(path, borderPaint);
            canvas.DrawPath(p1, borderPaint);
        }

        private static void RenderDashDotDot(SKCanvas canvas, SKPath path, RegionRenderStyle style)
        {
            SKColor innerColor = style.BorderColor.WithAlpha((byte)style.Opacity);

            using SKPaint regionInnerPaint = new()
            {
                Color = innerColor,
                Style = SKPaintStyle.Fill,
                PathEffect = SKPathEffect.CreateCorner(style.Smoothing)
            };

            canvas.DrawPath(path, regionInnerPaint);
            float[] intervals = [style.Width * 2, style.Width * 2, 0, style.Width * 2, 0, style.Width * 2]; ;
            SKPathEffect pathLineEffect = SKPathEffect.CreateDash(intervals, 0);

            using SKPaint dashDotDotPaint = PaintObjects.DashPaint.Clone();
            dashDotDotPaint.StrokeWidth = style.BorderWidth;
            dashDotDotPaint.Color = style.Color;
            dashDotDotPaint.PathEffect = pathLineEffect;

            if (style.Smoothing > 0)
            {
                dashDotDotPaint.PathEffect = SKPathEffect.CreateCompose(SKPathEffect.CreateCorner(style.Smoothing), pathLineEffect);
            }

            canvas.DrawPath(path, dashDotDotPaint);
        }

        private static void RenderDashDot(SKCanvas canvas, SKPath path, RegionRenderStyle style)
        {
            SKColor innerColor = style.BorderColor.WithAlpha((byte)style.Opacity);

            using SKPaint regionInnerPaint = new()
            {
                Color = innerColor,
                Style = SKPaintStyle.Fill,
                PathEffect = SKPathEffect.CreateCorner(style.Smoothing)
            };

            canvas.DrawPath(path, regionInnerPaint);

            float[] intervals = [style.Width * 2, style.Width * 2, 0, style.Width * 2];
            SKPathEffect pathLineEffect = SKPathEffect.CreateDash(intervals, 0);

            using SKPaint dashDotPaint = PaintObjects.DashPaint.Clone();
            dashDotPaint.StrokeWidth = style.BorderWidth;
            dashDotPaint.Color = style.Color;
            dashDotPaint.PathEffect = pathLineEffect;

            if (style.Smoothing > 0)
            {
                dashDotPaint.PathEffect = SKPathEffect.CreateCompose(SKPathEffect.CreateCorner(style.Smoothing), pathLineEffect);
            }

            canvas.DrawPath(path, dashDotPaint);
        }

        private static void RenderDashed(SKCanvas canvas, SKPath path, RegionRenderStyle style)
        {
            SKColor innerColor = style.BorderColor.WithAlpha((byte)style.Opacity);

            using SKPaint regionInnerPaint = new()
            {
                Color = innerColor,
                Style = SKPaintStyle.Fill,
                PathEffect = SKPathEffect.CreateCorner(style.Smoothing)
            };

            canvas.DrawPath(path, regionInnerPaint);

            float[] intervals = [style.Width * 2, style.Width * 2];
            SKPathEffect pathLineEffect = SKPathEffect.CreateDash(intervals, 0);

            using SKPaint dashPaint = PaintObjects.DashPaint.Clone();
            dashPaint.StrokeWidth = style.BorderWidth;
            dashPaint.Color = style.Color;
            dashPaint.PathEffect = pathLineEffect;

            if (style.Smoothing > 0)
            {
                dashPaint.PathEffect = SKPathEffect.CreateCompose(SKPathEffect.CreateCorner(style.Smoothing), pathLineEffect);
            }

            canvas.DrawPath(path, dashPaint);
        }

        private static void RenderDotted(SKCanvas canvas, SKPath path, RegionRenderStyle style)
        {
            SKColor innerColor = style.BorderColor.WithAlpha((byte)style.Opacity);

            using SKPaint regionInnerPaint = new()
            {
                Color = innerColor,
                Style = SKPaintStyle.Fill,
                PathEffect = SKPathEffect.CreateCorner(style.Smoothing)
            };

            canvas.DrawPath(path, regionInnerPaint);

            float[] intervals = [0, style.Width * 2];
            SKPathEffect pathLineEffect = SKPathEffect.CreateDash(intervals, 0);

            using SKPaint dottedPaint = PaintObjects.DashPaint.Clone();
            dottedPaint.StrokeWidth = style.BorderWidth;
            dottedPaint.Color = style.Color;
            dottedPaint.PathEffect = pathLineEffect;

            if (style.Smoothing > 0)
            {
                dottedPaint.PathEffect = SKPathEffect.CreateCompose(SKPathEffect.CreateCorner(style.Smoothing), pathLineEffect);
            }

            canvas.DrawPath(path, dottedPaint);
        }

        private static void RenderSolid(SKCanvas canvas, SKPath path, RegionRenderStyle style)
        {
            SKColor innerColor = style.BorderColor.WithAlpha((byte)style.Opacity);

            using SKPaint regionInnerPaint = new()
            {
                Color = innerColor,
                Style = SKPaintStyle.Fill,
                PathEffect = SKPathEffect.CreateCorner(style.Smoothing)
            };

            canvas.DrawPath(path, regionInnerPaint);

            using SKPaint solidPaint = PaintObjects.DashPaint.Clone();
            solidPaint.StrokeWidth = style.BorderWidth;
            solidPaint.Color = style.Color;

            if (style.Smoothing > 0)
            {
                solidPaint.PathEffect = SKPathEffect.CreateCorner(style.Smoothing);
            }

            canvas.DrawPath(path, solidPaint);
        }
    }
}
