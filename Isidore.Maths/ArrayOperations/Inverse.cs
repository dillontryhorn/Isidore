using System;

namespace Isidore.Maths
{
    public static partial class Arr
    {
        /// <summary>
        /// Performs a matrix inversion using Gauss-Jordan elimination.
        /// Code is adapted from ILIndustrial Light and Magic base code 
        /// via PBRT
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="m"> Matrix </param>
        /// <returns> Inverted matrix </returns>
        /// 
        public static double[,] Inverse<T>(T[,] m)
        {
            // This converts to a double array, since the matrix 
            // inversion is a floating point operation
            double[,] dm = Operator.Convert<T,double>(m);
            return Inverse(dm); // Calls the inverse operation below
        }

        /// <summary>
        /// Performs a matrix inversion
        /// </summary>
        /// <param name="m"> Matrix </param>
        /// <returns> Inverted matrix </returns>
        public static double[,] Inverse(double[,] m)
        {
            if (m == null)
                throw new ArgumentNullException("m");
            if (m.GetLength(0) != m.GetLength(1))
                throw new ArgumentException("The matrix must be square.", "m");
            int len = m.GetLength(0);
            int[] indxc = new int[len];
            int[] indxr = new int[len];
            int[] ipiv = new int[len];
            double[,] minv = (double[,])m.Clone();

            for (int i = 0; i < len; i++)
            {
                int irow = -1, icol = -1;
                double big = 0;
                // Selects pivot axis
                for (int j = 0; j < len; j++)
                    if (ipiv[j] != 1)
                        for (int k = 0; k < len; k++)
                            if (ipiv[k] == 0)
                                if (Math.Abs(minv[j, k]) >= big)
                                {
                                    big = Math.Abs(minv[j, k]);
                                    irow = j;
                                    icol = k;
                                }
                                else if (ipiv[k] > 1)
                                    throw new System.ArgumentException(
                                        "Can not invert singular matrix.");
                ++ipiv[icol];

                // Swap rows and columns based on pivot
                if (irow != icol)
                    for (int k = 0; k < len; ++k)
                        Function.Swap(ref minv[irow, k], 
                            ref minv[icol, k]);
                indxr[i] = irow;
                indxc[i] = icol;
                if (minv[icol, icol] == 0)
                    throw new System.ArgumentException(
                        "Can not invert singular matrix.");

                // Scales rows so pivot is set to one
                double pivinv = 1 / minv[icol, icol];
                minv[icol, icol] = 1;
                for (int j = 0; j < len; j++)
                    minv[icol, j] *= pivinv;

                // Subtract pivot row from matrix to zero out the columns
                for (int j = 0; j < len; j++)
                    if (j != icol)
                    {
                        double save = minv[j, icol];
                        minv[j, icol] = 0;
                        for (int k = 0; k < len; k++)
                            minv[j, k] -= minv[icol, k] * save;
                    }
            }

            // Permutations represented by swapping columns
            for (int j = len-1; j >= 0; j--)
                if (indxr[j] != indxc[j])
                    for (int k = 0; k < len; k++)
                        Function.Swap(ref minv[k, indxr[j]], 
                            ref minv[k, indxc[j]]);

            return minv;
        }
    }
}
