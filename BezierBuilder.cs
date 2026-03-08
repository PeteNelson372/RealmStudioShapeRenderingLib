namespace RealmStudioShapeRenderingLib
{
    using SkiaSharp;

    public static class BezierBuilder
    {
        public static List<SKPoint> BuildSpline(
            IReadOnlyList<SKPoint> controlPoints,
            int samplesPerSegment = 24)
        {
            var result = new List<SKPoint>();

            if (controlPoints.Count < 4)
                return result;

            for (int i = 0; i <= controlPoints.Count - 4; i += 3)
            {
                var p0 = controlPoints[i];
                var p1 = controlPoints[i + 1];
                var p2 = controlPoints[i + 2];
                var p3 = controlPoints[i + 3];

                for (int s = 0; s <= samplesPerSegment; s++)
                {
                    float t = s / (float)samplesPerSegment;

                    result.Add(EvaluateBezier(p0, p1, p2, p3, t));
                }
            }

            return result;
        }

        private static SKPoint EvaluateBezier(
            SKPoint p0,
            SKPoint p1,
            SKPoint p2,
            SKPoint p3,
            float t)
        {
            float u = 1f - t;

            float tt = t * t;
            float uu = u * u;

            float uuu = uu * u;
            float ttt = tt * t;

            return new SKPoint(
                uuu * p0.X +
                3 * uu * t * p1.X +
                3 * u * tt * p2.X +
                ttt * p3.X,

                uuu * p0.Y +
                3 * uu * t * p1.Y +
                3 * u * tt * p2.Y +
                ttt * p3.Y
            );
        }
    }
}
