using SkiaSharp;
using System;

namespace RealmStudioShapeRenderingLib
{
    public static class Utilities
    {
        public static float Clamp(float value, float min, float max)
        {
            return Math.Min(Math.Max(value, min), max);
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
    }
}
