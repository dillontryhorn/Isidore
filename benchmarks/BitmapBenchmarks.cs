using System;
using System.Drawing;
using BenchmarkDotNet.Attributes;
using Isidore.ImgProcess;

namespace Isidore.Benchmarks
{
    [Config(typeof(PerformanceConfig)), MemoryDiagnoser]
    public class BitmapReadBenchmarks
    {
        [Params(512)] public int Width { get; set; }
        [Params(513)] public int Height { get; set; }
        private Bitmap image;

        [GlobalSetup]
        public void Setup()
        {
            image = new Bitmap(Width, Height);
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    image.SetPixel(x, y, Color.FromArgb((x + y) & 255, x & 255, y & 255, (x ^ y) & 255));
        }

        [GlobalCleanup]
        public void Cleanup() { image.Dispose(); }

        [Benchmark(Baseline = true)]
        public Color[,] GetPixelRead()
        {
            int width = image.Width;
            int height = image.Height;
            Color[,] result = new Color[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++) result[x, y] = image.GetPixel(x, y);
            return result;
        }

        [Benchmark]
        public Color[,] BulkRead() { return ConvertImg.toColor(image); }
    }

    [Config(typeof(PerformanceConfig)), MemoryDiagnoser]
    public class BitmapWriteBenchmarks
    {
        [Params(512)] public int Width { get; set; }
        [Params(513)] public int Height { get; set; }
        private double[,] values;

        [GlobalSetup]
        public void Setup()
        {
            values = new double[Width, Height];
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++) values[x, y] = x * 0.125 - y * 0.375 + (x * y % 73);
        }

        [Benchmark(Baseline = true)]
        public void SetPixelWrite()
        {
            using (Bitmap image = OriginalGrayScale(values)) { }
        }

        [Benchmark]
        public void BulkWrite()
        {
            using (Bitmap image = ConvertImg.toBitmap(values)) { }
        }

        private static Bitmap OriginalGrayScale<T>(T[,] values)
        {
            int width = values.GetLength(0);
            int height = values.GetLength(1);
            long length = values.LongLength;
            double maximum = double.MinValue;
            double minimum = double.MaxValue;
            Type targetType = typeof(double);
            for (long i = 0; i < length; i++)
            {
                double pixel = (double)Convert.ChangeType(values[i % width, i / width], targetType);
                if (maximum < pixel) maximum = pixel;
                if (minimum > pixel) minimum = pixel;
            }
            maximum -= minimum;
            Bitmap image = new Bitmap(width, height);
            try
            {
                if (maximum == 0.0)
                {
                    for (int x = 0; x < width; x++)
                        for (int y = 0; y < height; y++) image.SetPixel(x, y, Color.Black);
                }
                else
                {
                    for (int x = 0; x < width; x++)
                        for (int y = 0; y < height; y++)
                        {
                            int pixel = (int)(255.0 * (Convert.ToDouble(values[x, y]) - minimum) / maximum);
                            image.SetPixel(x, y, Color.FromArgb(pixel, pixel, pixel));
                        }
                }
                return image;
            }
            catch
            {
                image.Dispose();
                throw;
            }
        }
    }
}
