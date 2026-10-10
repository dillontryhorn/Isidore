using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Isidore.Maths;

internal static class GpuMathsRegression
{
    public static void Run()
    {
        GpuMode saved = GpuAcceleration.Mode;
        try
        {
            bool available = GpuAcceleration.IsAvailable;
            MatrixProducts(available);
            MatrixVectorProducts(available);
            Convolutions(available);
            EmptyInvalidAndCustomInputs();
            AutomaticSmallWorkloads();
            ConcurrentCalls(available);
            Console.WriteLine(available ? "Maths GPU verified on " + GpuAcceleration.DeviceName + "."
                : "Maths GPU hardware checks skipped; CPU fallback verified. " + GpuAcceleration.LastError);
        }
        finally { GpuAcceleration.Mode = saved; }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void RequireDispatch(bool available, long before, string operation)
    {
        if (available && GpuAcceleration.DispatchCount == before)
            throw new Exception(operation + " fell back despite an available GPU: " + GpuAcceleration.LastError);
    }

    private static void Equal(Array expected, Array actual, string message)
    {
        Check(expected.Rank == actual.Rank && expected.Length == actual.Length, message + " shape differs");
        for (int axis = 0; axis < expected.Rank; axis++)
            Check(expected.GetLength(axis) == actual.GetLength(axis), message + " dimension differs");
        var e = expected.GetEnumerator();
        var a = actual.GetEnumerator();
        int index = 0;
        while (e.MoveNext() && a.MoveNext())
        {
            if (e.Current is double)
            {
                double x = (double)e.Current;
                double y = (double)a.Current;
                bool match = double.IsNaN(x) ? double.IsNaN(y)
                    : double.IsInfinity(x) ? x == y
                    : !double.IsNaN(y) && !double.IsInfinity(y) &&
                        Math.Abs(x - y) <= 1e-12 * Math.Max(1, Math.Abs(x));
                Check(match, message + " differs at element " + index + ": " + x + " versus " + y);
            }
            else Check(e.Current.Equals(a.Current), message + " differs at element " + index);
            index++;
        }
    }

    private static double[,] Doubles(int rows, int columns, int seed)
    {
        var random = new Random(seed);
        var result = new double[rows, columns];
        for (int row = 0; row < rows; row++)
            for (int column = 0; column < columns; column++)
                result[row, column] = random.NextDouble() * 2 - 1;
        return result;
    }

    private static int[,] Integers(int rows, int columns, int seed)
    {
        var random = new Random(seed);
        var result = new int[rows, columns];
        for (int row = 0; row < rows; row++)
            for (int column = 0; column < columns; column++)
                result[row, column] = random.Next(int.MinValue, int.MaxValue);
        return result;
    }

    private static void MatrixProducts(bool available)
    {
        double[,] first = Doubles(17, 19, 31);
        double[,] second = Doubles(19, 23, 37);
        double[,] originalFirst = (double[,])first.Clone();
        double[,] originalSecond = (double[,])second.Clone();
        GpuAcceleration.Mode = GpuMode.Disabled;
        double[,] expected = Arr.MatrixMultiply(first, second);
        long before = GpuAcceleration.DispatchCount;
        GpuAcceleration.Mode = GpuMode.PreferGpu;
        Equal(expected, Arr.MatrixMultiply(first, second), "Rectangular partial-tile double matrix product");
        RequireDispatch(available, before, "Double matrix product");
        before = GpuAcceleration.DispatchCount;
        Equal(expected, Arr.MatrixMultiply<double>(first, second), "Explicit generic double matrix product");
        RequireDispatch(available, before, "Generic double matrix product");
        Equal(originalFirst, first, "Matrix first input preservation");
        Equal(originalSecond, second, "Matrix second input preservation");

        int[,] intsFirst = Integers(21, 18, 41);
        int[,] intsSecond = Integers(18, 7, 43);
        int[,] originalInts = (int[,])intsFirst.Clone();
        GpuAcceleration.Mode = GpuMode.Disabled;
        int[,] expectedInts = Arr.MatrixMultiply(intsFirst, intsSecond);
        GpuAcceleration.Mode = GpuMode.PreferGpu;
        before = GpuAcceleration.DispatchCount;
        Equal(expectedInts, Arr.MatrixMultiply(intsFirst, intsSecond), "Unchecked integer matrix overflow");
        RequireDispatch(available, before, "Integer matrix product");
        before = GpuAcceleration.DispatchCount;
        Equal(expectedInts, Arr.MatrixMultiply<int>(intsFirst, intsSecond), "Explicit generic integer matrix product");
        RequireDispatch(available, before, "Generic integer matrix product");
        Equal(originalInts, intsFirst, "Integer matrix input preservation");

        // The last inner tile is partial. Its missing terms must be skipped,
        // since artificial padding multiplied by infinity would yield NaN.
        var wide = new double[1, 17];
        var tall = new double[17, 1];
        for (int term = 0; term < 17; term++) wide[0, term] = 1;
        tall[16, 0] = double.PositiveInfinity;
        GpuAcceleration.Mode = GpuMode.Disabled;
        expected = Arr.MatrixMultiply(wide, tall);
        GpuAcceleration.Mode = GpuMode.PreferGpu;
        Equal(expected, Arr.MatrixMultiply(wide, tall), "Infinite values in a partial matrix tile");

        double[,] specialFirst = { { double.NaN, 1, 2 }, { double.Epsilon, -0.0, 1 } };
        double[,] specialSecond = { { 1, 1 }, { 0, double.NegativeInfinity }, { 2, 3 } };
        GpuAcceleration.Mode = GpuMode.Disabled;
        expected = Arr.MatrixMultiply(specialFirst, specialSecond);
        GpuAcceleration.Mode = GpuMode.PreferGpu;
        Equal(expected, Arr.MatrixMultiply(specialFirst, specialSecond), "Matrix NaN, infinity and subnormal behavior");
        double[,] subnormal = Arr.MatrixMultiply(new[,] { { double.Epsilon } }, new[,] { { 1.0 } });
        Check(subnormal[0, 0] == double.Epsilon, "GPU matrix arithmetic must retain subnormal values");
    }

    private static void Convolutions(bool available)
    {
        double[,] image = Doubles(13, 19, 47);
        double[,] kernel = Doubles(4, 6, 53);
        double[,] original = (double[,])image.Clone();
        double[,] originalKernel = (double[,])kernel.Clone();
        GpuAcceleration.Mode = GpuMode.Disabled;
        double[,] expected = Arr.Convolve(image, kernel);
        GpuAcceleration.Mode = GpuMode.PreferGpu;
        long before = GpuAcceleration.DispatchCount;
        Equal(expected, Arr.Convolve(image, kernel), "Even rectangular double convolution");
        RequireDispatch(available, before, "2D double convolution");
        Equal(original, image, "Convolution input preservation");
        Equal(originalKernel, kernel, "Convolution kernel preservation");

        int[,] ints = Integers(5, 7, 59);
        int[,] intKernel = Integers(8, 4, 61);
        GpuAcceleration.Mode = GpuMode.Disabled;
        int[,] expectedInts = Arr.Convolve(ints, intKernel);
        GpuAcceleration.Mode = GpuMode.PreferGpu;
        before = GpuAcceleration.DispatchCount;
        Equal(expectedInts, Arr.Convolve(ints, intKernel), "Large even integer convolution kernel and overflow");
        RequireDispatch(available, before, "2D integer convolution");

        double[] signal = { 1, 2, 3 };
        double[] taps = { 2, 5 };
        before = GpuAcceleration.DispatchCount;
        Equal(new double[] { 9, 16, 15 }, Arr.Convolve(signal, taps), "True convolution and even-kernel alignment");
        RequireDispatch(available, before, "1D double convolution");
        int[] intSignal = { int.MaxValue, int.MinValue, -1, 7 };
        int[] intTaps = { int.MinValue, 3, int.MaxValue, -4, 9, 2 };
        GpuAcceleration.Mode = GpuMode.Disabled;
        int[] expectedSignal = Arr.Convolve(intSignal, intTaps);
        GpuAcceleration.Mode = GpuMode.PreferGpu;
        before = GpuAcceleration.DispatchCount;
        Equal(expectedSignal, Arr.Convolve(intSignal, intTaps), "1D integer convolution overflow and edge cropping");
        RequireDispatch(available, before, "1D integer convolution");

        // Out-of-image terms are skipped rather than evaluated as 0 * infinity.
        Equal(new double[] { 2 }, Arr.Convolve(new double[] { 1 }, new[] { double.PositiveInfinity, 2.0 }),
            "1D convolution must omit nonfinite out-of-image terms");
        double[,] specialKernel = { { double.PositiveInfinity, double.NaN }, { double.NegativeInfinity, 3 } };
        Equal(new[,] { { 3.0 } }, Arr.Convolve(new[,] { { 1.0 } }, specialKernel),
            "2D convolution must omit nonfinite out-of-image terms");
        Equal(new double[] { double.Epsilon }, Arr.Convolve(new[] { double.Epsilon }, new[] { 1.0 }),
            "Convolution must retain subnormal values");

        GpuAcceleration.Mode = GpuMode.Disabled;
        expected = Arr.Convolve<double, int, int>(ints, intKernel);
        GpuAcceleration.Mode = GpuMode.PreferGpu;
        before = GpuAcceleration.DispatchCount;
        Equal(expected, Arr.Convolve<double, int, int>(ints, intKernel), "Mixed-type double convolution conversion");
        RequireDispatch(available, before, "Converted double convolution");
    }

    private static void MatrixVectorProducts(bool available)
    {
        double[,] matrix = Doubles(17, 19, 151);
        var vector = new double[19];
        for (int index = 0; index < vector.Length; index++) vector[index] = index * 0.25 - 2;
        double[,] originalMatrix = (double[,])matrix.Clone();
        double[] originalVector = (double[])vector.Clone();
        GpuAcceleration.Mode = GpuMode.Disabled;
        double[] expected = Arr.MatrixMultiply(matrix, vector);
        GpuAcceleration.Mode = GpuMode.PreferGpu;
        long before = GpuAcceleration.DispatchCount;
        Equal(expected, Arr.MatrixMultiply(matrix, vector), "Double matrix-vector product");
        RequireDispatch(available, before, "Double matrix-vector product");
        before = GpuAcceleration.DispatchCount;
        Equal(expected, Arr.MatrixMultiply<double>(matrix, vector), "Explicit generic double matrix-vector product");
        RequireDispatch(available, before, "Generic double matrix-vector product");
        Equal(originalMatrix, matrix, "Matrix-vector matrix input preservation");
        Equal(originalVector, vector, "Matrix-vector vector input preservation");

        int[,] intMatrix = Integers(23, 7, 157);
        int[] intVector = { int.MinValue, int.MaxValue, -1, 0, 17, -39, 41 };
        int[,] originalInts = (int[,])intMatrix.Clone();
        GpuAcceleration.Mode = GpuMode.Disabled;
        int[] expectedInts = Arr.MatrixMultiply(intMatrix, intVector);
        GpuAcceleration.Mode = GpuMode.PreferGpu;
        before = GpuAcceleration.DispatchCount;
        Equal(expectedInts, Arr.MatrixMultiply(intMatrix, intVector), "Integer matrix-vector overflow");
        RequireDispatch(available, before, "Integer matrix-vector product");
        before = GpuAcceleration.DispatchCount;
        Equal(expectedInts, Arr.MatrixMultiply<int>(intMatrix, intVector), "Explicit generic integer matrix-vector product");
        RequireDispatch(available, before, "Generic integer matrix-vector product");
        Equal(originalInts, intMatrix, "Integer matrix-vector input preservation");

        double[,] specials = { { double.NaN, 1, 2 }, { 1, double.PositiveInfinity, 0 },
            { 1e20, 1, -1e20 }, { double.Epsilon, 0, 0 } };
        double[] ones = { 1, 1, 1 };
        GpuAcceleration.Mode = GpuMode.Disabled;
        expected = Arr.MatrixMultiply(specials, ones);
        GpuAcceleration.Mode = GpuMode.PreferGpu;
        double[] result = Arr.MatrixMultiply(specials, ones);
        Equal(expected, result, "Matrix-vector nonfinite values and accumulation order");
        Check(result[2] == 0 && result[3] == double.Epsilon,
            "Matrix-vector arithmetic must preserve sequential cancellation and subnormal values");
    }

    private static void EmptyInvalidAndCustomInputs()
    {
        GpuAcceleration.Mode = GpuMode.PreferGpu;
        long before = GpuAcceleration.DispatchCount;
        Equal(new double[0, 5], Arr.MatrixMultiply(new double[0, 3], new double[3, 5]), "Empty matrix rows");
        Equal(new int[2, 3], Arr.MatrixMultiply(new int[2, 0], new int[0, 3]), "Empty matrix inner dimension");
        Equal(new double[0], Arr.MatrixMultiply(new double[0, 3], new double[3]), "Empty matrix-vector rows");
        Equal(new int[3], Arr.MatrixMultiply(new int[3, 0], new int[0]), "Empty matrix-vector inner dimension");
        Equal(new double[3, 0], Arr.Convolve(new double[3, 0], new double[2, 2]), "Empty convolution input");
        Equal(new int[3, 4], Arr.Convolve(new int[3, 4], new int[0, 2]), "Empty convolution kernel");
        Equal(new double[0], Arr.Convolve(new double[0], new double[] { 1 }), "Empty 1D convolution input");
        Equal(new int[3], Arr.Convolve(new int[] { 1, 2, 3 }, new int[0]), "Empty 1D convolution kernel");
        bool rejected = false;
        try { Arr.MatrixMultiply(new double[2, 3], new double[2, 4]); }
        catch (ArgumentException) { rejected = true; }
        Check(rejected, "Invalid matrix dimensions must be rejected before GPU dispatch");
        rejected = false;
        try { Arr.MatrixMultiply(new int[2, 3], new int[2]); }
        catch (ArgumentException) { rejected = true; }
        Check(rejected, "Invalid matrix-vector dimensions must be rejected before GPU dispatch");

        decimal[,] decimalFirst = { { 0.1m, 0.2m }, { 0.3m, 0.4m } };
        decimal[,] decimalSecond = { { 0.7m }, { 0.8m } };
        Equal(new[,] { { 0.23m }, { 0.53m } }, Arr.MatrixMultiply(decimalFirst, decimalSecond),
            "Custom numeric matrix operators must remain on the CPU");
        Equal(new decimal[] { 0.23m, 0.53m }, Arr.MatrixMultiply(decimalFirst, new decimal[] { 0.7m, 0.8m }),
            "Custom numeric matrix-vector operators must remain on the CPU");
        Equal(new decimal[] { 0.09m, 0.16m, 0.15m },
            Arr.Convolve(new decimal[] { 0.1m, 0.2m, 0.3m }, new decimal[] { 0.2m, 0.5m }),
            "Custom numeric convolution operators must remain on the CPU");
        Check(GpuAcceleration.DispatchCount == before, "Empty, invalid or custom-type operations must not dispatch");
    }

    private static void AutomaticSmallWorkloads()
    {
        GpuAcceleration.Mode = GpuMode.Automatic;
        long before = GpuAcceleration.DispatchCount;
        Arr.MatrixMultiply(Doubles(4, 4, 67), Doubles(4, 4, 71));
        Arr.MatrixMultiply(Doubles(4, 4, 67), new double[] { 1, 2, 3, 4 });
        Arr.Convolve(Doubles(32, 31, 73), Doubles(3, 3, 79));
        Arr.Convolve(new int[] { 1, 2, 3 }, new int[] { 2, 5 });
        Check(GpuAcceleration.DispatchCount == before, "Automatic mode must retain small workloads on the CPU");
        GpuAcceleration.Mode = GpuMode.Disabled;
        Arr.MatrixMultiply(Doubles(17, 19, 83), Doubles(19, 23, 89));
        Check(GpuAcceleration.DispatchCount == before, "Disabled mode must not dispatch supported operations");
    }

    private static void ConcurrentCalls(bool available)
    {
        double[,] first = Doubles(19, 17, 97);
        double[,] second = Doubles(17, 21, 101);
        double[,] image = Doubles(11, 13, 103);
        double[,] kernel = Doubles(5, 3, 107);
        GpuAcceleration.Mode = GpuMode.Disabled;
        double[,] product = Arr.MatrixMultiply(first, second);
        double[,] convolution = Arr.Convolve(image, kernel);
        GpuAcceleration.Mode = GpuMode.PreferGpu;
        long before = GpuAcceleration.DispatchCount;
        var jobs = new Task[4];
        for (int job = 0; job < jobs.Length; job++)
            jobs[job] = Task.Run(delegate
            {
                for (int repeat = 0; repeat < 3; repeat++)
                {
                    Equal(product, Arr.MatrixMultiply(first, second), "Concurrent matrix dispatch");
                    Equal(convolution, Arr.Convolve(image, kernel), "Concurrent convolution dispatch");
                }
            });
        Task.WaitAll(jobs);
        if (available)
            Check(GpuAcceleration.DispatchCount - before == 24, "Concurrent supported calls must all dispatch successfully");
    }

    // Optional end-to-end benchmark: includes allocation, transfers and queue
    // synchronization. Cold GPU time is reported separately from warmed calls.
    public static void Benchmark()
    {
        GpuMode saved = GpuAcceleration.Mode;
        try
        {
            if (!GpuAcceleration.IsAvailable)
            {
                Console.WriteLine("Maths GPU benchmark skipped: " + GpuAcceleration.LastError);
                return;
            }
            Console.WriteLine("Maths GPU benchmark device: " + GpuAcceleration.DeviceName);
            Console.WriteLine("workload,cpu_median_ms,gpu_median_ms,first_gpu_ms,speedup");
            foreach (int size in new[] { 128, 256, 512 })
            {
                double[,] first = Doubles(size, size, 109);
                double[,] second = Doubles(size, size, 113);
                BenchmarkOperation("matrix_double_" + size, delegate { return Arr.MatrixMultiply(first, second); });
            }
            int[,] intsFirst = Integers(256, 256, 127);
            int[,] intsSecond = Integers(256, 256, 131);
            BenchmarkOperation("matrix_int_256", delegate { return Arr.MatrixMultiply(intsFirst, intsSecond); });
            foreach (int rows in new[] { 1024, 4096 })
            {
                double[,] matrix = Doubles(rows, 4096, 149);
                var vector = new double[4096];
                for (int index = 0; index < vector.Length; index++) vector[index] = Math.Sin(index * 0.07);
                BenchmarkOperation("matrix_vector_double_" + rows + "x4096",
                    delegate { return Arr.MatrixMultiply(matrix, vector); });
            }
            int[,] intMatrix = Integers(1024, 4096, 163);
            var intVector = new int[4096];
            for (int index = 0; index < intVector.Length; index++) intVector[index] = unchecked(index * 1234567);
            BenchmarkOperation("matrix_vector_int_1024x4096", delegate { return Arr.MatrixMultiply(intMatrix, intVector); });
            foreach (int size in new[] { 256, 512, 1024 })
            {
                double[,] image = Doubles(size, size, 137);
                int width = size == 256 ? 15 : size == 512 ? 7 : 3;
                double[,] kernel = Doubles(width, width, 139);
                BenchmarkOperation("convolve_double_" + size + "x" + width,
                    delegate { return Arr.Convolve(image, kernel); });
            }
            var signal = new double[65536];
            var taps = new double[65];
            for (int index = 0; index < signal.Length; index++) signal[index] = Math.Sin(index * 0.03);
            for (int index = 0; index < taps.Length; index++) taps[index] = 1.0 / taps.Length;
            BenchmarkOperation("convolve1_double_65536x65", delegate { return Arr.Convolve(signal, taps); });
        }
        finally { GpuAcceleration.Mode = saved; }
    }

    private static void BenchmarkOperation(string name, Func<Array> operation)
    {
        GpuAcceleration.Mode = GpuMode.Disabled;
        Array baseline = operation();
        // Include discovery, program build and buffer allocation in the first
        // GPU measurement. The driver's persistent compiler cache may be warm.
        GpuAcceleration.ReleaseResources();
        GpuAcceleration.Mode = GpuMode.PreferGpu;
        long before = GpuAcceleration.DispatchCount;
        Array result;
        double firstGpu = Time(operation, out result);
        RequireDispatch(true, before, name);
        Equal(baseline, result, name + " benchmark result");
        double[] cpuTimes = new double[3];
        double[] gpuTimes = new double[3];
        GpuAcceleration.Mode = GpuMode.Disabled;
        for (int trial = 0; trial < cpuTimes.Length; trial++) cpuTimes[trial] = Time(operation, out result);
        GpuAcceleration.Mode = GpuMode.PreferGpu;
        for (int trial = 0; trial < gpuTimes.Length; trial++)
        {
            before = GpuAcceleration.DispatchCount;
            gpuTimes[trial] = Time(operation, out result);
            RequireDispatch(true, before, name);
        }
        Equal(baseline, result, name + " warmed benchmark result");
        Array.Sort(cpuTimes);
        Array.Sort(gpuTimes);
        Console.WriteLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
            "{0},{1:F3},{2:F3},{3:F3},{4:F2}", name, cpuTimes[1], gpuTimes[1], firstGpu, cpuTimes[1] / gpuTimes[1]));
    }

    private static double Time(Func<Array> operation, out Array result)
    {
        var stopwatch = Stopwatch.StartNew();
        result = operation();
        stopwatch.Stop();
        return stopwatch.Elapsed.TotalMilliseconds;
    }
}
