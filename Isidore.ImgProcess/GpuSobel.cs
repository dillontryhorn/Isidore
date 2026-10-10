using System;
using Isidore.Maths;

namespace Isidore.ImgProcess
{
    /// <summary>
    /// Fuses both Sobel convolutions and gradient magnitude into one GPU pass.
    /// The caller keeps its normal CPU implementation when dispatch is unavailable.
    /// </summary>
    internal static class GpuSobel
    {
        private const long AutomaticMinimum = 2000000;

        public static bool TryProcess(double[,] image, double[,] kernel0,
            double[,] kernel1, out Tuple<double[,], double[,], double[,]> result)
        {
            result = null;
            try { return TryProcessCore(image, kernel0, kernel1, out result); }
            catch (OutOfMemoryException) { return false; }
        }

        private static bool TryProcessCore(double[,] image, double[,] kernel0,
            double[,] kernel1, out Tuple<double[,], double[,], double[,]> result)
        {
            result = null;
            if (image == null || kernel0 == null || kernel1 == null ||
                image.LongLength == 0 || image.LongLength > int.MaxValue)
                return false;

            // Four image buffers and both coefficient arrays must fit the
            // runtime's maximum budget; avoid host packing and driver setup
            // for batches that will necessarily fall back.
            if (32L * image.LongLength + 8L * (kernel0.LongLength + kernel1.LongLength)
                > 512L * 1024 * 1024) return false;

            long work = image.LongLength * (kernel0.LongLength + kernel1.LongLength);
            if (!GpuAcceleration.ShouldUseKernel(KernelSource, "sobel_pair", work,
                AutomaticMinimum, 256L * 1024 * 1024, 16L * 1024 * 1024))
                return false;

            int length0 = image.GetLength(0);
            int length1 = image.GetLength(1);
            double[,] gradient0 = new double[length0, length1];
            double[,] gradient1 = new double[length0, length1];
            double[,] magnitude = new double[length0, length1];
            // The public Sobel kernels can be replaced or edited by callers.
            // Capture their current coefficients for this operation.
            double[,] snapshot0 = (double[,])kernel0.Clone();
            double[,] snapshot1 = (double[,])kernel1.Clone();
            if (!GpuAcceleration.TryExecute(KernelSource, "sobel_pair",
                (int)image.LongLength,
                GpuArgument.Input(image), GpuArgument.Input(snapshot0), GpuArgument.Input(snapshot1),
                GpuArgument.Output(gradient0), GpuArgument.Output(gradient1), GpuArgument.Output(magnitude),
                GpuArgument.Scalar(length0), GpuArgument.Scalar(length1),
                GpuArgument.Scalar(kernel0.GetLength(0)), GpuArgument.Scalar(kernel0.GetLength(1)),
                GpuArgument.Scalar(kernel1.GetLength(0)), GpuArgument.Scalar(kernel1.GetLength(1))))
                return false;

            result = Tuple.Create(magnitude, gradient0, gradient1);
            return true;
        }

        private const string KernelSource = @"
#ifdef cl_khr_fp64
#pragma OPENCL EXTENSION cl_khr_fp64 : enable
#elif defined(cl_amd_fp64)
#pragma OPENCL EXTENSION cl_amd_fp64 : enable
#endif
#pragma OPENCL FP_CONTRACT OFF

__kernel void sobel_pair(__global const double* image,
    __global const double* kernel0, __global const double* kernel1,
    __global double* gradient0, __global double* gradient1, __global double* magnitude,
    int length0, int length1, int k00, int k01, int k10, int k11)
{
    size_t index = get_global_id(0);
    size_t count = (size_t)length0 * (size_t)length1;
    if (index >= count) return;
    int x = (int)(index / (size_t)length1);
    int y = (int)(index % (size_t)length1);
    double first = 0.0;
    double second = 0.0;
    // Match Arr.Convolve's tap order, reversed kernel and even-kernel anchor.
    // Skip out-of-bounds taps, including coefficients that could be nonfinite.
    for (int kx = 0; kx < k00; ++kx)
    {
        int sx = x + kx - (k00 + 1) / 2 + 1;
        if (sx < 0 || sx >= length0) continue;
        for (int ky = 0; ky < k01; ++ky)
        {
            int sy = y + ky - (k01 + 1) / 2 + 1;
            if (sy >= 0 && sy < length1)
                first = first + image[(size_t)sx * length1 + sy] *
                    kernel0[(size_t)(k00 - 1 - kx) * k01 + (k01 - 1 - ky)];
        }
    }
    for (int kx = 0; kx < k10; ++kx)
    {
        int sx = x + kx - (k10 + 1) / 2 + 1;
        if (sx < 0 || sx >= length0) continue;
        for (int ky = 0; ky < k11; ++ky)
        {
            int sy = y + ky - (k11 + 1) / 2 + 1;
            if (sy >= 0 && sy < length1)
                second = second + image[(size_t)sx * length1 + sy] *
                    kernel1[(size_t)(k10 - 1 - kx) * k11 + (k11 - 1 - ky)];
        }
    }
    gradient0[index] = first;
    gradient1[index] = second;
    magnitude[index] = sqrt(first * first + second * second);
}
";
    }
}
