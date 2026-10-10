using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using Isidore.ImgProcess;

internal static class BitmapPerformanceRegression
{
    public static void Run()
    {
        foreach (PixelFormat format in new PixelFormat[] {
            PixelFormat.Format24bppRgb, PixelFormat.Format32bppRgb,
            PixelFormat.Format32bppArgb, PixelFormat.Format32bppPArgb })
            using (Bitmap bitmap = CreateColorBitmap(format))
            {
                CompareColors(bitmap, ConvertImg.toColor(bitmap), "Color bitmap " + format);
                CompareLoadedBitmap(bitmap, "Loaded color bitmap " + format);
                // A read must release its lock before the caller edits the image.
                bitmap.SetPixel(1, 2, Color.FromArgb(91, 37, 53, 71));
                CompareColors(bitmap, ConvertImg.toColor(bitmap), "Read after edit " + format);
            }

        foreach (PixelFormat format in new PixelFormat[] {
            PixelFormat.Format1bppIndexed, PixelFormat.Format4bppIndexed,
            PixelFormat.Format8bppIndexed })
            using (Bitmap bitmap = CreateIndexedBitmap(format))
            {
                CompareColors(bitmap, ConvertImg.toColor(bitmap), "Indexed palette " + format);
                CompareLoadedBitmap(bitmap, "Loaded indexed bitmap " + format);
            }

        CompareUnmanagedStride(24);
        CompareUnmanagedStride(-24);
        CompareGray(new double[5, 3], "all-zero grayscale");
        CompareGray(new double[,] { { -3.25, 7.5, 0 }, { 1.0 / 3, 255, -10 } },
            "non-square grayscale with fractional values");
        CompareGray(new byte[,] { { 0, 1, 255 }, { 128, 99, 17 } }, "byte grayscale");
        CompareGray(new decimal[,] { { -3.5m }, { 1.75m }, { 16.5m } }, "decimal grayscale");
        CompareGray(new double[,] { { double.Epsilon, 0, -double.Epsilon } }, "subnormal grayscale");
        CompareGray(new double[,] { { double.NaN, 0 }, { 1, -1 } }, "NaN grayscale exception");
        CompareGray(new double[,] { { double.PositiveInfinity, 0 } }, "infinite grayscale exception");
        CompareGray(new double[,] { { -double.MaxValue, double.MaxValue } }, "overflow grayscale exception");
        CompareGray(new double[0, 3], "empty first dimension");
        CompareGray(new double[3, 0], "empty second dimension");
        CompareGray<double>(null, "null grayscale input");
        ExpectException<NullReferenceException>(() => ConvertImg.toColor(null),
            "A null bitmap must retain its existing exception.");
        Assert(Isidore.Load.Load.Bitmap(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".png")) == null,
            "A missing bitmap file must still return null.");
    }

    private static Bitmap CreateColorBitmap(PixelFormat format)
    {
        // The odd width exercises 24-bit row padding; dimensions expose transposes.
        Bitmap bitmap = new Bitmap(7, 3, format);
        for (int x = 0; x < bitmap.Width; x++)
            for (int y = 0; y < bitmap.Height; y++)
                bitmap.SetPixel(x, y, Color.FromArgb((x * 53 + y * 11) % 256,
                    (x * 17 + y * 31) % 256, (x * 37 + y * 43) % 256, (x * 59 + y * 71) % 256));
        return bitmap;
    }

    private static Bitmap CreateIndexedBitmap(PixelFormat format)
    {
        Bitmap bitmap = new Bitmap(13, 5, format);
        ColorPalette palette = bitmap.Palette;
        for (int index = 0; index < palette.Entries.Length; index++)
            palette.Entries[index] = Color.FromArgb((index * 53) % 256,
                (index * 17 + 5) % 256, (index * 37 + 7) % 256, (index * 59 + 11) % 256);
        bitmap.Palette = palette;
        int bits = Image.GetPixelFormatSize(format);
        BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            ImageLockMode.WriteOnly, format);
        try
        {
            byte[] row = new byte[Math.Abs(data.Stride)];
            for (int y = 0; y < bitmap.Height; y++)
            {
                Array.Clear(row, 0, row.Length);
                for (int x = 0; x < bitmap.Width; x++)
                {
                    int index = (x * 3 + y * 5) % palette.Entries.Length;
                    if (bits == 8) row[x] = (byte)index;
                    else if (bits == 4) row[x / 2] |= (byte)(index << (x % 2 == 0 ? 4 : 0));
                    else row[x / 8] |= (byte)(index << (7 - x % 8));
                }
                Marshal.Copy(row, 0, IntPtr.Add(data.Scan0, y * data.Stride), row.Length);
            }
        }
        finally { bitmap.UnlockBits(data); }
        return bitmap;
    }

    private static void CompareUnmanagedStride(int stride)
    {
        const int width = 7;
        const int height = 3;
        IntPtr allocation = Marshal.AllocHGlobal(Math.Abs(stride) * height);
        try
        {
            IntPtr scan0 = stride < 0 ? IntPtr.Add(allocation, -stride * (height - 1)) : allocation;
            byte[] row = new byte[Math.Abs(stride)];
            for (int y = 0; y < height; y++)
            {
                for (int index = 0; index < row.Length; index++)
                    row[index] = (byte)(index * 7 + y * 37);
                Marshal.Copy(row, 0, IntPtr.Add(scan0, y * stride), row.Length);
            }
            using (Bitmap bitmap = new Bitmap(width, height, stride, PixelFormat.Format24bppRgb, scan0))
                CompareColors(bitmap, ConvertImg.toColor(bitmap), "Unmanaged stride " + stride);
        }
        finally { Marshal.FreeHGlobal(allocation); }
    }

    private static void CompareLoadedBitmap(Bitmap source, string label)
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".png");
        try
        {
            source.Save(path, ImageFormat.Png);
            Color[,] actual = Isidore.Load.Load.Bitmap(path);
            using (Bitmap decoded = new Bitmap(path))
                CompareColors(decoded, actual, label);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    private static void CompareColors(Bitmap bitmap, Color[,] actual, string label)
    {
        Assert(actual.GetLength(0) == bitmap.Width && actual.GetLength(1) == bitmap.Height,
            label + ": dimensions changed.");
        for (int x = 0; x < bitmap.Width; x++)
            for (int y = 0; y < bitmap.Height; y++)
                Assert(bitmap.GetPixel(x, y).Equals(actual[x, y]),
                    label + ": Color value changed at " + x + "," + y + ".");
    }

    private static void CompareGray<T>(T[,] input, string label)
    {
        Bitmap expected = null;
        Bitmap actual = null;
        Exception expectedFailure = null;
        Exception actualFailure = null;
        try
        {
            try { expected = ReferenceGray(input); }
            catch (Exception error) { expectedFailure = error; }
            try { actual = ConvertImg.toBitmap(input); }
            catch (Exception error) { actualFailure = error; }
            if (expectedFailure != null || actualFailure != null)
            {
                Assert(expectedFailure != null && actualFailure != null &&
                    expectedFailure.GetType() == actualFailure.GetType(), label + ": exception type changed.");
                return;
            }
            Assert(actual.PixelFormat == expected.PixelFormat && actual.PixelFormat == PixelFormat.Format32bppArgb,
                label + ": bitmap output format changed.");
            CompareColors(expected, ConvertImg.toColor(actual), label);
        }
        finally
        {
            if (expected != null) expected.Dispose();
            if (actual != null) actual.Dispose();
        }
    }

    // Exact original normalization/conversion sequence serves as the behavior oracle.
    private static Bitmap ReferenceGray<T>(T[,] input)
    {
        int width = input.GetLength(0);
        int height = input.GetLength(1);
        double maximum = double.MinValue;
        double minimum = double.MaxValue;
        for (long index = 0; index < input.LongLength; index++)
        {
            double value = (double)Convert.ChangeType(input[index % width, index / width], typeof(double));
            if (maximum < value) maximum = value;
            if (minimum > value) minimum = value;
        }
        maximum -= minimum;
        Bitmap bitmap = new Bitmap(width, height);
        try
        {
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    if (maximum == 0.0) bitmap.SetPixel(x, y, Color.Black);
                    else
                    {
                        int value = (int)(255.0 * (Convert.ToDouble(input[x, y]) - minimum) / maximum);
                        bitmap.SetPixel(x, y, Color.FromArgb(value, value, value));
                    }
                }
            return bitmap;
        }
        catch { bitmap.Dispose(); throw; }
    }

    private static void ExpectException<T>(Action action, string message) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new Exception(message);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
