using System;

namespace Isidore.Maths
{
    /// <summary>
    /// Provides common mathematical array operations.
    /// </summary>
    public static partial class Arr
    {

        /// <summary>
        /// 2D Discrete Convolution
        /// </summary>
        /// <typeparam name="T"> Data Type </typeparam>
        /// <param name="arr"> Input array </param>
        /// <param name="kern"> Convolution kernel </param>
        /// <returns> Convolved array </returns>
        public static T[,] Convolve<T>(T[,] arr, T[,] kern)
        {
            T[,] gpuResult;
            if (TryConvolutionGpu(arr, kern, out gpuResult)) return gpuResult;

            // Supporting expressions
            Func<T, T, T> add = Operator<T>.Add;
            Func<T, T, T> multi = Operator<T>.Multiply;

            // kernel and image lengths
            int aDim0 = arr.GetLength(0);
            int aDim1 = arr.GetLength(1);
            int kDim0 = kern.GetLength(0);
            int kDim1 = kern.GetLength(1);
            int hkDim0 = (kDim0 + 1) / 2;
            int hkDim1 = (kDim1 + 1) / 2;

            T[,] carr = new T[aDim0, aDim1];

            for (int aIdx0 = 0; aIdx0 < aDim0; aIdx0++)
                for (int aIdx1 = 0; aIdx1 < aDim1; aIdx1++)
                    for (int kIdx0 = 0; kIdx0 < kDim0; kIdx0++)
                    {
                        int kDim0Loc = kDim0 - 1 - kIdx0;
                        int aDim0Loc = aIdx0 + (kIdx0 - hkDim0) + 1;
                        if (aDim0Loc >= 0 && aDim0Loc < aDim0)
                            for (int kIdx1 = 0; kIdx1 < kDim1; kIdx1++)
                            {
                                int kDim1Loc = kDim1 - 1 - kIdx1;
                                int aDim1Loc = aIdx1 + (kIdx1 - hkDim1) + 1;
                                if (aDim1Loc >= 0 && aDim1Loc < aDim1)
                                    carr[aIdx0, aIdx1] = 
                                        add(carr[aIdx0, aIdx1],
                                        multi(arr[aDim0Loc, aDim1Loc], 
                                        kern[kDim0Loc, kDim1Loc]));
                            }
                    }
            return carr;
        }

        /// <summary>
        /// 2D Discrete Convolution
        /// </summary>
        /// <typeparam name="Tout"> Returned data type </typeparam>
        /// <typeparam name="T1"> Array data type </typeparam>
        /// <typeparam name="T2"> Kernel Data type </typeparam>
        /// <param name="arr"> Input array </param>
        /// <param name="kern"> Convolution kernel </param>
        /// <returns> Convolved array </returns>
        public static Tout[,] Convolve<Tout, T1, T2>(T1[,] arr, T2[,] kern)
        {
            Type tTout = typeof(Tout);
            Tout[,] tarr = Operator.Convert<T1,Tout>(arr);
            Tout[,] tkern = Operator.Convert<T2, Tout>(kern);

            return Convolve(tarr, tkern);
        }

        /// <summary>
        /// 1D Discrete Convolution
        /// </summary>
        /// <typeparam name="T"> Data Type </typeparam>
        /// <param name="arr"> Input array </param>
        /// <param name="kern"> Convolution kernel </param>
        /// <returns> Convolved array </returns>
        public static T[] Convolve<T>(T[] arr, T[] kern)
        {
            T[] gpuResult;
            if (TryConvolutionGpu(arr, kern, out gpuResult)) return gpuResult;

            // Supporting expressions
            Func<T, T, T> add = Operator<T>.Add;
            Func<T, T, T> multi = Operator<T>.Multiply;

            // kernel and image lengths
            int aDim0 = arr.GetLength(0);
            int kDim0 = kern.GetLength(0);
            int hkDim0 = (kDim0 + 1) / 2;

            T[] carr = new T[aDim0];

            for (int aIdx0 = 0; aIdx0 < aDim0; aIdx0++)
                for (int kIdx0 = 0; kIdx0 < kDim0; kIdx0++)
                {
                    int kDim0Loc = kDim0 - 1 - kIdx0;
                    int aDim0Loc = aIdx0 + (kIdx0 - hkDim0) + 1;
                    if (aDim0Loc >= 0 && aDim0Loc < aDim0)
                        carr[aIdx0] = add(carr[aIdx0], 
                            multi(arr[aDim0Loc], kern[kDim0Loc]));
                }
            return carr;
        }

        /// <summary>
        /// 2D Discrete Convolution
        /// </summary>
        /// <typeparam name="Tout"> Returned data type </typeparam>
        /// <typeparam name="T1"> Array data type </typeparam>
        /// <typeparam name="T2"> Kernel Data type </typeparam>
        /// <param name="arr"> Input array </param>
        /// <param name="kern"> Convolution kernel </param>
        /// <returns> Convolved array </returns>
        public static Tout[] Convolve<Tout, T1, T2>(T1[] arr, T2[] kern)
        {
            Type tTout = typeof(Tout);
            Tout[] tarr = Operator.Convert<T1, Tout>(arr);
            Tout[] tkern = Operator.Convert<T2, Tout>(kern);

            return Convolve(tarr, tkern);
        }
    }
}
