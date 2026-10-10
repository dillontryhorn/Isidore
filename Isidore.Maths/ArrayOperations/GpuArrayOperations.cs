using System;

namespace Isidore.Maths
{
    public static partial class Arr
    {
        private const int MatrixTile = 16;
        private const long MatrixGpuMinimum = 1024L * 1024;
        private const long ConvolutionGpuMinimum = 4L * 1024 * 1024;

        // Rectangular CLR arrays store the last dimension contiguously. These
        // kernels use that layout directly, without the grid-index convention
        // used by Function.LinearIndex.
        private const string DoubleKernelTypes = @"
typedef double value_t;
typedef double accumulator_t;
#define PRODUCT(a,b) ((a) * (b))
#define RESULT(a) (a)
";
        private const string IntKernelTypes = @"
typedef int value_t;
typedef uint accumulator_t;
// Explicit modulo arithmetic preserves the CPU's unchecked Int32 overflow.
#define PRODUCT(a,b) ((uint)(a) * (uint)(b))
#define RESULT(a) as_int(a)
";

        private const string MatrixKernel = @"
#define TILE 16
__kernel void KERNEL_NAME(__global const value_t *a,
    __global const value_t *b, __global value_t *output,
    int rows, int columns, int inner)
{
    size_t column = get_global_id(0);
    size_t row = get_global_id(1);
    int localColumn = get_local_id(0);
    int localRow = get_local_id(1);
    __local value_t aTile[TILE][TILE];
    __local value_t bTile[TILE][TILE];
    accumulator_t sum = 0;

    for (size_t start = 0; start < (size_t)inner; start += TILE)
    {
        size_t aColumn = start + localColumn;
        size_t bRow = start + localRow;
        aTile[localRow][localColumn] = row < (size_t)rows && aColumn < (size_t)inner
            ? a[row * (size_t)inner + aColumn] : 0;
        bTile[localRow][localColumn] = bRow < (size_t)inner && column < (size_t)columns
            ? b[bRow * (size_t)columns + column] : 0;
        barrier(CLK_LOCAL_MEM_FENCE);

        // Preserve ascending CPU accumulation order and omit padded terms.
        // Multiplying an artificial zero by infinity would introduce a NaN.
        for (int term = 0; term < TILE && start + term < (size_t)inner; term++)
            sum += PRODUCT(aTile[localRow][term], bTile[term][localColumn]);
        barrier(CLK_LOCAL_MEM_FENCE);
    }
    if (row < (size_t)rows && column < (size_t)columns)
        output[row * (size_t)columns + column] = RESULT(sum);
}
";

        private const string Convolution2Kernel = @"
__kernel void KERNEL_NAME(__global const value_t *input,
    __global const value_t *filter, __global value_t *output,
    int rows, int columns, int kernelRows, int kernelColumns,
    int halfRows, int halfColumns)
{
    size_t column = get_global_id(0);
    size_t row = get_global_id(1);
    if (row >= (size_t)rows || column >= (size_t)columns) return;
    accumulator_t sum = 0;
    for (int kr = 0; kr < kernelRows; kr++)
    {
        long sourceRow = (long)row + kr - halfRows + 1;
        if (sourceRow < 0 || sourceRow >= rows) continue;
        for (int kc = 0; kc < kernelColumns; kc++)
        {
            long sourceColumn = (long)column + kc - halfColumns + 1;
            if (sourceColumn < 0 || sourceColumn >= columns) continue;
            size_t sourceIndex = (size_t)sourceRow * columns + sourceColumn;
            size_t kernelIndex = (size_t)(kernelRows - 1 - kr) * kernelColumns
                + kernelColumns - 1 - kc;
            sum += PRODUCT(input[sourceIndex], filter[kernelIndex]);
        }
    }
    output[row * (size_t)columns + column] = RESULT(sum);
}
";

        private const string MatrixVectorKernel = @"
__kernel void KERNEL_NAME(__global const value_t *matrix,
    __global const value_t *vector, __global value_t *output,
    int rows, int inner)
{
    size_t row = get_global_id(0);
    if (row >= (size_t)rows) return;
    accumulator_t sum = 0;
    for (int term = 0; term < inner; term++)
        sum += PRODUCT(matrix[row * (size_t)inner + term], vector[term]);
    output[row] = RESULT(sum);
}
";

        private const string Convolution1Kernel = @"
__kernel void KERNEL_NAME(__global const value_t *input,
    __global const value_t *filter, __global value_t *output,
    int length, int kernelLength, int halfLength)
{
    size_t index = get_global_id(0);
    if (index >= (size_t)length) return;
    accumulator_t sum = 0;
    for (int term = 0; term < kernelLength; term++)
    {
        long sourceIndex = (long)index + term - halfLength + 1;
        if (sourceIndex < 0 || sourceIndex >= length) continue;
        sum += PRODUCT(input[sourceIndex], filter[kernelLength - 1 - term]);
    }
    output[index] = RESULT(sum);
}
";

        private static readonly string DoubleMatrixSource = KernelSource(MatrixKernel, "matrix_double", true);
        private static readonly string IntMatrixSource = KernelSource(MatrixKernel, "matrix_int", false);
        private static readonly string DoubleMatrixVectorSource = KernelSource(MatrixVectorKernel, "matrix_vector_double", true);
        private static readonly string IntMatrixVectorSource = KernelSource(MatrixVectorKernel, "matrix_vector_int", false);
        private static readonly string DoubleConvolution2Source = KernelSource(Convolution2Kernel, "convolve2_double", true);
        private static readonly string IntConvolution2Source = KernelSource(Convolution2Kernel, "convolve2_int", false);
        private static readonly string DoubleConvolution1Source = KernelSource(Convolution1Kernel, "convolve1_double", true);
        private static readonly string IntConvolution1Source = KernelSource(Convolution1Kernel, "convolve1_int", false);

        private static string KernelSource(string body, string name, bool useDouble)
        {
            return (useDouble ? DoubleKernelTypes : IntKernelTypes) + body.Replace("KERNEL_NAME", name);
        }

        private static long WorkEstimate(long outputs, long terms)
        {
            if (outputs == 0 || terms == 0) return 0;
            return outputs > long.MaxValue / terms ? long.MaxValue : outputs * terms;
        }

        private static bool UseMatrixGpu(int rows, int columns, int inner, string source, string kernel)
        {
            if (rows == 0 || columns == 0 || inner == 0) return false;
            // A long dot product with only a few outputs cannot occupy a GPU.
            // PreferGpu bypasses this guard to make small kernels verifiable.
            if (GpuAcceleration.Mode != GpuMode.PreferGpu &&
                (rows < MatrixTile || columns < MatrixTile || inner < MatrixTile)) return false;
            return GpuAcceleration.ShouldUseKernel(source, kernel,
                WorkEstimate((long)rows * columns, inner), MatrixGpuMinimum,
                256L * 1024 * 1024, 64L * 1024 * 1024);
        }

        private static bool TryMatrixGpu<T>(T[,] first, T[,] second, out T[,] product)
        {
            product = null;
            int rows = first.GetLength(0);
            int columns = second.GetLength(1);
            int inner = first.GetLength(1);
            bool useDouble = typeof(T) == typeof(double);
            string source = useDouble ? DoubleMatrixSource : IntMatrixSource;
            string kernel = useDouble ? "matrix_double" : "matrix_int";
            if (!UseMatrixGpu(rows, columns, inner, source, kernel)) return false;
            T[,] candidate = new T[rows, columns];
            long[] global = { ((long)columns + MatrixTile - 1) / MatrixTile * MatrixTile,
                ((long)rows + MatrixTile - 1) / MatrixTile * MatrixTile };
            if (!GpuAcceleration.TryExecute(source, kernel, global,
                new long[] { MatrixTile, MatrixTile },
                GpuArgument.Input(first), GpuArgument.Input(second), GpuArgument.Output(candidate),
                GpuArgument.Scalar(rows), GpuArgument.Scalar(columns), GpuArgument.Scalar(inner))) return false;
            product = candidate;
            return true;
        }

        private static bool UseConvolutionGpu(Array input, Array kernel, string source, string name)
        {
            if (input.LongLength == 0 || kernel.LongLength == 0) return false;
            if (GpuAcceleration.Mode != GpuMode.PreferGpu && input.LongLength < 16384) return false;
            return GpuAcceleration.ShouldUseKernel(source, name,
                WorkEstimate(input.LongLength, kernel.LongLength), ConvolutionGpuMinimum,
                256L * 1024 * 1024, 32L * 1024 * 1024);
        }

        private static bool TryMatrixGpu<T>(T[,] matrix, T[] vector, out T[] product)
        {
            product = null;
            int rows = matrix.GetLength(0);
            int inner = matrix.GetLength(1);
            if (rows == 0 || inner == 0) return false;
            if (GpuAcceleration.Mode != GpuMode.PreferGpu && rows < 64) return false;
            bool useDouble = typeof(T) == typeof(double);
            string source = useDouble ? DoubleMatrixVectorSource : IntMatrixVectorSource;
            string kernel = useDouble ? "matrix_vector_double" : "matrix_vector_int";
            // This bandwidth-bound operation cannot amortize driver startup
            // within the buffer budget. An initialized driver and a sufficiently
            // large first batch, or PreferGpu, can prepare its reusable kernel.
            if (!GpuAcceleration.ShouldUseKernel(source, kernel, WorkEstimate(rows, inner),
                4L * 1024 * 1024, long.MaxValue, 48L * 1024 * 1024)) return false;
            T[] candidate = new T[rows];
            if (!GpuAcceleration.TryExecute(source, kernel, rows,
                GpuArgument.Input(matrix), GpuArgument.Input(vector), GpuArgument.Output(candidate),
                GpuArgument.Scalar(rows), GpuArgument.Scalar(inner))) return false;
            product = candidate;
            return true;
        }

        private static bool TryConvolutionGpu<T>(T[,] input, T[,] kernel, out T[,] result)
        {
            result = null;
            if (typeof(T) != typeof(double) && typeof(T) != typeof(int)) return false;
            bool useDouble = typeof(T) == typeof(double);
            string source = useDouble ? DoubleConvolution2Source : IntConvolution2Source;
            string name = useDouble ? "convolve2_double" : "convolve2_int";
            if (!UseConvolutionGpu(input, kernel, source, name)) return false;
            int rows = input.GetLength(0);
            int columns = input.GetLength(1);
            int kernelRows = kernel.GetLength(0);
            int kernelColumns = kernel.GetLength(1);
            T[,] candidate = new T[rows, columns];
            if (!GpuAcceleration.TryExecute(source, name, new long[] { columns, rows }, null,
                GpuArgument.Input(input), GpuArgument.Input(kernel), GpuArgument.Output(candidate),
                GpuArgument.Scalar(rows), GpuArgument.Scalar(columns), GpuArgument.Scalar(kernelRows),
                GpuArgument.Scalar(kernelColumns), GpuArgument.Scalar((kernelRows + 1) / 2),
                GpuArgument.Scalar((kernelColumns + 1) / 2))) return false;
            result = candidate;
            return true;
        }

        private static bool TryConvolutionGpu<T>(T[] input, T[] kernel, out T[] result)
        {
            result = null;
            if (typeof(T) != typeof(double) && typeof(T) != typeof(int)) return false;
            bool useDouble = typeof(T) == typeof(double);
            string source = useDouble ? DoubleConvolution1Source : IntConvolution1Source;
            string name = useDouble ? "convolve1_double" : "convolve1_int";
            if (!UseConvolutionGpu(input, kernel, source, name)) return false;
            T[] candidate = new T[input.Length];
            if (!GpuAcceleration.TryExecute(source, name, input.Length,
                GpuArgument.Input(input), GpuArgument.Input(kernel), GpuArgument.Output(candidate),
                GpuArgument.Scalar(input.Length), GpuArgument.Scalar(kernel.Length),
                GpuArgument.Scalar((kernel.Length + 1) / 2))) return false;
            result = candidate;
            return true;
        }
    }
}
