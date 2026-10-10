using System;
using System.Threading.Tasks;
using Isidore.Maths;

internal static class GpuRuntimeRegression
{
    public static void Run()
    {
        GpuMode previous = GpuAcceleration.Mode;
        try
        {
            GpuAcceleration.Mode = GpuMode.Disabled;
            double[] untouched = { 42 };
            if (GpuAcceleration.TryExecute("", "none", 1, GpuArgument.Output(untouched)) || untouched[0] != 42)
                throw new Exception("Disabled acceleration must leave host outputs unchanged.");
            if (GpuAcceleration.ShouldUse(100000000, 1))
                throw new Exception("Disabled acceleration must never select a GPU.");
            GpuAcceleration.ReleaseResources();
            GpuAcceleration.Mode = GpuMode.Automatic;
            if (!GpuAcceleration.MeetsWorkloadThreshold(1000, 1, 1000) ||
                GpuAcceleration.MeetsWorkloadThreshold(100, 1, 1000))
                throw new Exception("Workload checks must apply cold thresholds without initializing the driver.");
            if (GpuAcceleration.ShouldUse(100, 1, 1000))
                throw new Exception("Automatic mode must avoid initialization below the cold threshold.");
            if (!GpuAcceleration.IsAvailable)
            {
                Console.WriteLine("SKIP GPU hardware comparisons: " + GpuAcceleration.LastError);
                return;
            }
            Console.WriteLine("GPU: " + GpuAcceleration.DeviceName);
            GpuAcceleration.Mode = GpuMode.PreferGpu;
            const string source = @"
__kernel void runtime_probe(__global const double* x, __global double* y,
    __global double* products, int count) {
    int i = get_global_id(0); if (i >= count) return;
    double product = x[i] * (1.0 - 0x1.0p-27);
    products[i] = product;
    y[i] = product - 1.0;
}";
            double[] values = { 1 + Math.Pow(2, -27), 0, -1, double.PositiveInfinity,
                double.NaN, 1e-310, double.Epsilon, -0.0 };
            double[] output = new double[values.Length];
            double[] products = new double[values.Length];
            long before = GpuAcceleration.DispatchCount;
            if (!GpuAcceleration.TryExecute(source, "runtime_probe", values.Length,
                GpuArgument.Input(values), GpuArgument.Output(output), GpuArgument.Output(products),
                GpuArgument.Scalar(values.Length)))
                throw new Exception("GPU probe failed: " + GpuAcceleration.LastError);
            for (int i = 0; i < output.Length; i++)
            {
                double product = values[i] * (1 - Math.Pow(2, -27));
                double expected = product - 1;
                if (!double.IsNaN(product) &&
                    BitConverter.DoubleToInt64Bits(products[i]) != BitConverter.DoubleToInt64Bits(product))
                    throw new Exception("GPU products must preserve subnormals and signed zero at index " + i);
                if (!(output[i].Equals(expected)))
                    throw new Exception("GPU double arithmetic differs from CPU at index " + i);
            }
            if (output[0] != 0 || GpuAcceleration.DispatchCount != before + 1)
                throw new Exception("GPU execution must disable contraction and report its completed dispatch.");

            const string identity = @"
__kernel void identity(__global const double* input, __global double* output, int count) {
 int i=get_global_id(0); if(i<count) output[i]=input[i];
}";
            GpuAcceleration.Mode = GpuMode.Automatic;
            if (GpuAcceleration.ShouldUseKernel(identity, "identity", 100, 1, 1, 1000))
                throw new Exception("An initialized driver must still apply the first-compilation threshold.");
            GpuAcceleration.Mode = GpuMode.PreferGpu;
            Parallel.For(1, 17, length =>
            {
                double[,] input = new double[length, 3];
                for (int x = 0; x < length; x++)
                    for (int y = 0; y < 3; y++) input[x, y] = x * 10 + y + length;
                double[,] result = new double[length, 3];
                if (!GpuAcceleration.TryExecute(identity, "identity", input.Length,
                    GpuArgument.Input(input), GpuArgument.Output(result), GpuArgument.Scalar(input.Length)))
                    throw new Exception("Concurrent GPU dispatch failed: " + GpuAcceleration.LastError);
                for (int x = 0; x < length; x++)
                    for (int y = 0; y < 3; y++)
                        if (input[x, y] != result[x, y]) throw new Exception("Concurrent buffers mixed results.");
            });
            GpuAcceleration.Mode = GpuMode.Automatic;
            if (!GpuAcceleration.ShouldUseKernel(identity, "identity", 100, 1, 1, 1000))
                throw new Exception("A compiled kernel should use its normal warm threshold.");
            GpuAcceleration.Mode = GpuMode.PreferGpu;
            double[] retained = { 17, 23 };
            if (GpuAcceleration.TryExecute(identity, "missing_kernel", 2,
                GpuArgument.Output(retained)) || retained[0] != 17 || retained[1] != 23)
                throw new Exception("Failed kernels must not publish partial results.");
            // Release/reinitialize must also be safe after a compiler failure.
            GpuAcceleration.ReleaseResources();
            if (!GpuAcceleration.IsAvailable) throw new Exception("GPU rediscovery failed after resource release.");
        }
        finally { GpuAcceleration.Mode = previous; }
    }
}
