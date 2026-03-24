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
                    RenderLineAndDashes(canvas, path, style);
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
                    RenderBorderedGradient(canvas, path, style);
                    break;

                case PathType.BorderedLightSolidPath:
                    RenderBorderedLight(canvas, path, style);
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

        private static void RenderBorderTexture(SKCanvas canvas, SKPath path, PathRenderStyle style)
        {
            throw new NotImplementedException();
        }

        private static void RenderTexture(SKCanvas canvas, SKPath path, PathRenderStyle style)
        {
            throw new NotImplementedException();
        }

        private static void RenderRailroad(SKCanvas canvas, IReadOnlyList<SKPoint> points, PathRenderStyle style)
        {
            throw new NotImplementedException();
        }

        private static void RenderMarkers(SKCanvas canvas, IReadOnlyList<SKPoint> points, PathRenderStyle style)
        {
            throw new NotImplementedException();
        }

        private static void RenderBorderedLight(SKCanvas canvas, SKPath path, PathRenderStyle style)
        {
            throw new NotImplementedException();
        }

        private static void RenderBorderedGradient(SKCanvas canvas, SKPath path, PathRenderStyle style)
        {
            throw new NotImplementedException();
        }

        private static void RenderBordered(SKCanvas canvas, SKPath path, PathRenderStyle style)
        {
            throw new NotImplementedException();
        }

        private static void RenderThick(SKCanvas canvas, SKPath path, PathRenderStyle style)
        {
            throw new NotImplementedException();
        }

        private static void RenderIrregularDash(SKCanvas canvas, IReadOnlyList<SKPoint> points, PathRenderStyle style)
        {
            throw new NotImplementedException();
        }

        private static void RenderLineAndDashes(SKCanvas canvas, SKPath path, PathRenderStyle style)
        {
            throw new NotImplementedException();
        }

        private static void RenderChevron(SKCanvas canvas, IReadOnlyList<SKPoint> points, PathRenderStyle style)
        {
            throw new NotImplementedException();
        }

        private static void RenderDoubleLine(SKCanvas canvas, SKPath path, PathRenderStyle style)
        {
            float outerWidth = style.Width + (style.BorderWidth * 2);
            float innerWidth = style.Width;

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
