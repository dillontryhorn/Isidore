using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Isidore.ImgProcess;
using Isidore.Maths;

internal static class GpuImageRegression
{
    public static void Run()
    {
        GpuMode previousMode = GpuAcceleration.Mode;
        double[,] previousKernel0 = Sobel.Sob0;
        double[,] previousKernel1 = Sobel.Sob1;
        try
        {
            CompareImage(new double[5, 7], "zero image");
            CompareImage(CreateImage(37, 53), "rectangular patterned image");
            double[,] horizontal = new double[11, 17];
            double[,] diagonal = new double[11, 17];
            double[,] weakContinuation = new double[11, 17];
            for (int x = 0; x < 11; x++)
                for (int y = 0; y < 17; y++)
                {
                    horizontal[x, y] = x >= 5 ? 1 : 0;
                    diagonal[x, y] = x + y >= 13 ? 1 : 0;
                    weakContinuation[x, y] = x >= 5 ? (y <= 5 ? 1 : 0.3) : 0;
                }
            CompareImage(horizontal, "axis-aligned edge and equal-gradient ties");
            CompareImage(diagonal, "diagonal edge");
            CompareImage(weakContinuation, "hysteresis continuation");
            CompareImage(new byte[,] { { 0, 255, 127 }, { 10, 30, 90 }, { 255, 0, 255 } },
                "generic byte conversion");
            CompareImage(new double[,] { { 3, -2, 5, 1, 9 } }, "single-row boundary");
            CompareImage(new double[,] { { 3 }, { -2 }, { 5 }, { 1 }, { 9 } }, "single-column boundary");

            double[,] nonfinite = CreateImage(7, 9);
            nonfinite[3, 4] = double.NaN;
            CompareImage(nonfinite, "NaN propagation");

            // Public Sobel kernels can have different shapes and even widths.
            // A specialized GPU path must use their actual current coefficients.
            Sobel.Sob0 = new double[,] { { 0.25, -1.5 }, { 2, 3.75 } };
            Sobel.Sob1 = new double[,] { { -2, 0, 0.5 }, { 3, -1, 2.25 } };
            CompareImage(CreateImage(13, 19), "mutable even-sized kernels");
            Sobel.Sob0[0, 0] = -3.5;
            CompareImage(CreateImage(13, 19), "edited kernel coefficients");

            GpuAcceleration.Mode = GpuMode.Disabled;
            long beforeDisabled = GpuAcceleration.DispatchCount;
            Sobel.Process(CreateImage(16, 21));
            Assert(GpuAcceleration.DispatchCount == beforeDisabled,
                "Disabled image processing must not dispatch to the GPU.");
            var empty = Sobel.Process(new double[0, 3]);
            Assert(empty.Item1.GetLength(0) == 0 && empty.Item1.GetLength(1) == 3,
                "Empty image processing must retain its dimensions through the CPU fallback.");
        }
        finally
        {
            Sobel.Sob0 = previousKernel0;
            Sobel.Sob1 = previousKernel1;
            GpuAcceleration.Mode = previousMode;
        }
    }

    private static void CompareImage<T>(T[,] image, string label)
    {
        GpuAcceleration.Mode = GpuMode.Disabled;
        var expectedSobel = Sobel.Process(image);
        var expectedCanny = Canny.Process(image, 0.8, 0.2);
        GpuAcceleration.Mode = GpuMode.PreferGpu;
        bool available = GpuAcceleration.IsAvailable;
        long before = GpuAcceleration.DispatchCount;
        var actualSobel = Sobel.Process(image);
        var actualCanny = Canny.Process(image, 0.8, 0.2);
        Compare(expectedSobel.Item1, actualSobel.Item1, label + " magnitude");
        Compare(expectedSobel.Item2, actualSobel.Item2, label + " first gradient");
        Compare(expectedSobel.Item3, actualSobel.Item3, label + " second gradient");
        Compare(expectedCanny.Item4, actualCanny.Item4, label + " Canny magnitude");
        for (int x = 0; x < image.GetLength(0); x++)
            for (int y = 0; y < image.GetLength(1); y++)
            {
                Assert(expectedCanny.Item1[x, y] == actualCanny.Item1[x, y],
                    label + ": GPU/CPU Canny edge masks differ.");
                Assert(expectedCanny.Item6[x, y] == actualCanny.Item6[x, y],
                    label + ": GPU/CPU Canny direction bins differ.");
            }
        if (available)
            Assert(GpuAcceleration.DispatchCount >= before + 2,
                label + ": compatible hardware must execute both fused Sobel passes. " + GpuAcceleration.LastError);
        else
            Assert(GpuAcceleration.DispatchCount == before,
                label + ": unavailable hardware must retain the CPU fallback.");
    }

    private static void Compare(double[,] expected, double[,] actual, string label)
    {
        Assert(expected.GetLength(0) == actual.GetLength(0) && expected.GetLength(1) == actual.GetLength(1),
            label + ": result dimensions differ.");
        for (int x = 0; x < expected.GetLength(0); x++)
            for (int y = 0; y < expected.GetLength(1); y++)
            {
                double first = expected[x, y];
                double second = actual[x, y];
                if (double.IsNaN(first))
                    Assert(double.IsNaN(second), label + ": NaN propagation differs.");
                else if (double.IsInfinity(first))
                    Assert(first == second, label + ": infinity propagation differs.");
                else
                    Assert(Math.Abs(first - second) <= 1e-12 * Math.Max(1, Math.Abs(first)),
                        label + ": numeric results differ.");
            }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static double[,] CreateImage(int length0, int length1)
    {
        double[,] image = new double[length0, length1];
        for (int x = 0; x < length0; x++)
            for (int y = 0; y < length1; y++)
                image[x, y] = ((x * 17 + y * 29) % 113) / 16.0 + ((x / 7 + y / 11) % 2);
        return image;
    }

    /// <summary>
    /// Warm end-to-end timings include uploads/downloads and CPU Canny hysteresis.
    /// Returned CSV distinguishes actual GPU execution from CPU fallback.
    /// </summary>
    public static string Benchmark()
    {
        GpuMode previousMode = GpuAcceleration.Mode;
        StringBuilder rows = new StringBuilder("operation,width,height,cpu_ms,gpu_ms,speedup,gpu_dispatches\n");
        try
        {
            if (!GpuAcceleration.IsAvailable)
                return "Image GPU benchmarks skipped: " + GpuAcceleration.LastError;
            foreach (int size in new int[] { 256, 512, 1024 })
            {
                double[,] image = CreateImage(size, size + 13);
                BenchmarkOperation(rows, "Sobel", image, () => Sobel.Process(image));
                BenchmarkOperation(rows, "Canny", image, () => Canny.Process(image, 0.8, 0.2));
            }
            return rows.ToString();
        }
        finally { GpuAcceleration.Mode = previousMode; }
    }

    private static void BenchmarkOperation(StringBuilder rows, string name, double[,] image, Action operation)
    {
        GpuAcceleration.Mode = GpuMode.Disabled;
        operation();
        double cpuTime = MedianMilliseconds(operation);
        GpuAcceleration.Mode = GpuMode.PreferGpu;
        operation(); // Compile and allocate outside the warm measurements.
        long before = GpuAcceleration.DispatchCount;
        double gpuTime = MedianMilliseconds(operation);
        long dispatched = GpuAcceleration.DispatchCount - before;
        if (dispatched == 0)
        {
            rows.Append(name).Append(",GPU dispatch failed: ").Append(GpuAcceleration.LastError).Append('\n');
            return;
        }
        rows.AppendFormat(CultureInfo.InvariantCulture, "{0},{1},{2},{3:F3},{4:F3},{5:F3},{6}\n",
            name, image.GetLength(0), image.GetLength(1), cpuTime, gpuTime, cpuTime / gpuTime, dispatched);
    }

    private static double MedianMilliseconds(Action operation)
    {
        double[] elapsed = new double[3];
        for (int sample = 0; sample < elapsed.Length; sample++)
        {
            Stopwatch watch = Stopwatch.StartNew();
            operation();
            watch.Stop();
            elapsed[sample] = watch.Elapsed.TotalMilliseconds;
        }
        Array.Sort(elapsed);
        return elapsed[elapsed.Length / 2];
    }
}
