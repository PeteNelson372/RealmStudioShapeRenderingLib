using SimplexNoise;
using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{

    public static class WaterGeometryBuilder
    {
        public static SKPath GenerateRandomLakePath(SKPoint location, float lakeSize)
        {
            int fieldSize = 500;

            float[,] field = GetNoiseGeneratedLakeField();

            float waterLevel = 0.5f;

            bool[,] mask = BuildLakeMask(field, waterLevel);

            bool[,] lake = FloodFillLake(mask);

            List<SKPoint> contour = TraceContour(lake);

            SKPath path = BuildLakePath(contour, location, lakeSize, fieldSize);

            return path;
        }

        public static float[,] GetNoiseGeneratedLakeField()
        {
            // LAKE

            // see: https://www.redblobgames.com/maps/terrain-from-noise/#demo
            // and https://github.com/WardBenjamin/SimplexNoise

            float width = 500;
            float height = 500;

            Noise.Seed = Random.Shared.Next(int.MaxValue - 1);

            // scale parameter: larger scale value = denser noise, so scale = wavelength (higher wavelength = denser noise)
            // or scale is inverse of frequency

            float[,] noiseArray1 = Noise.Calc2D((int)width, (int)height, 0.008F);
            float[,] noiseArray2 = Noise.Calc2D((int)width, (int)height, 0.01F);

            float[,] elevation = new float[(int)width, (int)height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    //float e = (noiseArray1[x, y] / 255.0F);
                    float e = ((noiseArray1[x, y] / 255.0F) + (noiseArray2[x, y] / 255.0F)) / 2.0F;
                    elevation[x, y] = e;
                }
            }


            float[,] distance = new float[(int)width, (int)height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float nx = 2 * x / width - 1;
                    float ny = 2 * y / height - 1;

                    distance[x, y] = 1 - (float)Math.Min(1, (nx * nx + ny * ny) / Math.Sqrt(2));
                }
            }

            float interpolationWeight = 0.6F;

            float[,] field = new float[(int)width, (int)height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    field[x, y] = float.Lerp(elevation[x, y], 1 - distance[x, y], interpolationWeight);
                }
            }

            return field;
        }

        public static bool[,] BuildLakeMask(float[,] field, float waterLevel)
        {
            int w = field.GetLength(0);
            int h = field.GetLength(1);

            bool[,] mask = new bool[w, h];

            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    mask[x, y] = field[x, y] < waterLevel;

            return mask;
        }

        public static bool[,] FloodFillLake(bool[,] mask)
        {
            int w = mask.GetLength(0);
            int h = mask.GetLength(1);

            bool[,] lake = new bool[w, h];

            int cx = w / 2;
            int cy = h / 2;

            if (!mask[cx, cy])
                return lake;

            Queue<(int x, int y)> q = new();
            q.Enqueue((cx, cy));

            while (q.Count > 0)
            {
                var (x, y) = q.Dequeue();

                if (x < 0 || y < 0 || x >= w || y >= h)
                    continue;

                if (!mask[x, y] || lake[x, y])
                    continue;

                lake[x, y] = true;

                q.Enqueue((x + 1, y));
                q.Enqueue((x - 1, y));
                q.Enqueue((x, y + 1));
                q.Enqueue((x, y - 1));
            }

            return lake;
        }

        public static List<SKPoint> ExtractContour(bool[,] lake)
        {
            int w = lake.GetLength(0);
            int h = lake.GetLength(1);

            List<SKPoint> pts = [];

            for (int x = 0; x < w - 1; x++)
                for (int y = 0; y < h - 1; y++)
                {
                    if (!lake[x, y])
                    {
                        continue;
                    }

                    if (!lake[x + 1, y] ||
                        !lake[x - 1, y] ||
                        !lake[x, y + 1] ||
                        !lake[x, y - 1])
                    {
                        pts.Add(new SKPoint(x, y));
                    }
                }

            return pts;
        }

        public static List<SKPoint> TraceContour(bool[,] lakeMask)
        {
            int w = lakeMask.GetLength(0);
            int h = lakeMask.GetLength(1);

            // Moore neighborhood directions (clockwise)
            int[] dx = { 1, 1, 0, -1, -1, -1, 0, 1 };
            int[] dy = { 0, 1, 1, 1, 0, -1, -1, -1 };

            // find starting pixel
            int startX = -1;
            int startY = -1;

            for (int y = 1; y < h - 1 && startX < 0; y++)
            {
                for (int x = 1; x < w - 1; x++)
                {
                    if (lakeMask[x, y] && !lakeMask[x - 1, y])
                    {
                        startX = x;
                        startY = y;
                        break;
                    }
                }
            }

            if (startX < 0)
                return new List<SKPoint>();

            List<SKPoint> contour = new();

            int cx = startX;
            int cy = startY;

            int prevDir = 0;

            do
            {
                contour.Add(new SKPoint(cx, cy));

                bool found = false;

                for (int i = 0; i < 8; i++)
                {
                    int dir = (prevDir + i) % 8;

                    int nx = cx + dx[dir];
                    int ny = cy + dy[dir];

                    if (nx < 0 || ny < 0 || nx >= w || ny >= h)
                        continue;

                    if (lakeMask[nx, ny])
                    {
                        cx = nx;
                        cy = ny;

                        prevDir = (dir + 5) % 8;

                        found = true;
                        break;
                    }
                }

                if (!found)
                    break;

            }
            while (cx != startX || cy != startY);

            return contour;
        }

        public static SKPath BuildLakePath(
            List<SKPoint> contour,
            SKPoint location,
            float lakeSize,
            int fieldSize)
        {
            SKPath path = new();

            if (contour.Count < 3)
                return path;

            path.MoveTo(contour[0]);

            for (int i = 1; i < contour.Count; i++)
                path.LineTo(contour[i]);

            path.Close();

            float scale = lakeSize / fieldSize;

            path.Transform(SKMatrix.CreateScale(scale, scale));

            path.Transform(
                SKMatrix.CreateTranslation(
                    location.X - lakeSize / 2,
                    location.Y - lakeSize / 2));

            return path;
        }
    }
}
