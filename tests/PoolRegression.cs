using System;
using System.Threading.Tasks;
using Isidore.Maths;

internal static class PoolRegression
{
    private const string CopySource = @"
__kernel void pooled_copy(__global const double* values, __global const int* codes,
    __global double* copiedValues, __global int* copiedCodes, int count) {
    int i = get_global_id(0);
    if (i < count) { copiedValues[i] = values[i]; copiedCodes[i] = codes[i]; }
}";

    public static void Run()
    {
        GpuMode previous = GpuAcceleration.Mode;
        try
        {
            GpuAcceleration.Mode = GpuMode.PreferGpu;
            if (!GpuAcceleration.IsAvailable)
            {
                Console.WriteLine("SKIP pooled GPU transfer comparisons: " + GpuAcceleration.LastError);
                return;
            }

            // Alternate short and long buffers so cached device allocations and
            // pooled host buckets are reused with different logical byte lengths.
            int[] lengths = { 513, 1, 129, 3, 4097, 5, 17, 257, 1 };
            for (int i = 0; i < lengths.Length; i++)
                CopyMixedArrays(lengths[i], i % 4 + 1, i + 1);
            Parallel.For(1, 17, i => CopyMixedArrays(i * 11 + 1, i % 4 + 1, i * 100));

            // Check the largest pooled double output and mixed original/pool
            // staging, then exceed the cap by only four bytes for the int output.
            CopyMixedArrays(128 * 1024, 2, 122);
            CopyMixedArrays(128 * 1024 + 1, 3, 123);
            CopyMixedArrays(256 * 1024 + 1, 4, 124);

            FailedDispatchRetainsOutputs();
            // Subsequent reuse must still succeed after the failed dispatch.
            CopyMixedArrays(7, 4, 99);
        }
        finally { GpuAcceleration.Mode = previous; }
    }

    private static void CopyMixedArrays(int count, int rank, int tag)
    {
        double[] values = new double[count];
        int[] codes = new int[count];
        for (int i = 0; i < count; i++)
        {
            switch (i % 8)
            {
                case 0: values[i] = BitConverter.Int64BitsToDouble(long.MinValue); break;
                case 1: values[i] = double.Epsilon; break;
                case 2: values[i] = -double.Epsilon; break;
                case 3: values[i] = double.PositiveInfinity; break;
                case 4: values[i] = double.NegativeInfinity; break;
                case 5: values[i] = BitConverter.Int64BitsToDouble(0x7ff8000000000042L); break;
                case 6: values[i] = 1e-310; break;
                default: values[i] = tag + i * 0.125; break;
            }
            codes[i] = unchecked((tag * 397) ^ (i * 119) ^ int.MinValue);
        }

        Array inputValues = RankedArray(typeof(double), count, rank);
        Array inputCodes = RankedArray(typeof(int), count, 5 - rank);
        Array outputValues = RankedArray(typeof(double), count, 5 - rank);
        Array outputCodes = RankedArray(typeof(int), count, rank);
        Buffer.BlockCopy(values, 0, inputValues, 0, Buffer.ByteLength(values));
        Buffer.BlockCopy(codes, 0, inputCodes, 0, Buffer.ByteLength(codes));
        if (!GpuAcceleration.TryExecute(CopySource, "pooled_copy", count,
            GpuArgument.Input(inputValues), GpuArgument.Input(inputCodes),
            GpuArgument.Output(outputValues), GpuArgument.Output(outputCodes),
            GpuArgument.Scalar(count)))
            throw new Exception("Mixed pooled GPU transfer failed: " + GpuAcceleration.LastError);

        double[] copiedValues = new double[count];
        int[] copiedCodes = new int[count];
        Buffer.BlockCopy(outputValues, 0, copiedValues, 0, Buffer.ByteLength(copiedValues));
        Buffer.BlockCopy(outputCodes, 0, copiedCodes, 0, Buffer.ByteLength(copiedCodes));
        for (int i = 0; i < count; i++)
            if (BitConverter.DoubleToInt64Bits(values[i]) != BitConverter.DoubleToInt64Bits(copiedValues[i]) ||
                codes[i] != copiedCodes[i])
                throw new Exception("Pooled downloads mixed or changed output bytes at index " + i + ".");
    }

    private static Array RankedArray(Type element, int count, int rank)
    {
        int[] dimensions = new int[rank];
        for (int axis = 0; axis < rank; axis++) dimensions[axis] = axis == rank - 1 ? count : 1;
        return Array.CreateInstance(element, dimensions);
    }

    private static void FailedDispatchRetainsOutputs()
    {
        const string source = @"
__kernel void pooled_failure(__global double* values, __global int* codes, int count) {
    int i = get_global_id(0);
    if (i < count) { values[i] = 0.0; codes[i] = 0; }
}";
        double[,] values = { { 17.25, -0.5, 29.0 } };
        int[,,] codes = { { { int.MinValue, 19, int.MaxValue } } };
        long before = GpuAcceleration.DispatchCount;
        // A fresh kernel with its final scalar argument omitted must fail at
        // launch, leaving both output arrays and the success counter untouched.
        if (GpuAcceleration.TryExecute(source, "pooled_failure", values.Length,
            GpuArgument.Output(values), GpuArgument.Output(codes)) ||
            values[0, 0] != 17.25 || values[0, 1] != -0.5 || values[0, 2] != 29.0 ||
            codes[0, 0, 0] != int.MinValue || codes[0, 0, 1] != 19 || codes[0, 0, 2] != int.MaxValue ||
            GpuAcceleration.DispatchCount != before)
            throw new Exception("Failed GPU dispatch must retain every caller output and its success count.");
    }
}
