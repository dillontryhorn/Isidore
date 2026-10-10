using System;

namespace Isidore.Maths
{
    public static partial class Arr
    {
        # region 2 Dimensional Arrays

        /// <summary>
        /// Matrix multiplication. Functionally m1 applies after m2.
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="m0"> First matrix (A of A*B) </param>
        /// <param name="m1"> Second matrix (B of A*B) </param>
        /// <returns> Product matrix </returns>
        public static T[,] MatrixMultiply<T>(T[,] m0, T[,] m1)
        {
            // Dimension check
            if(m0.GetLength(1) != m1.GetLength(0))
                throw new System.ArgumentException(
                    "Inner matrix dimensions must agree.", "arr1");

            // Exact primitive types share the native CPU/GPU path. Custom
            // operator types retain their original delegate semantics.
            if (typeof(T) == typeof(double))
                return (T[,])(object)MatrixMultiply((double[,])(object)m0, (double[,])(object)m1);
            if (typeof(T) == typeof(int))
                return (T[,])(object)MatrixMultiply((int[,])(object)m0, (int[,])(object)m1);
            
            // Function delegates
            Func<T, T, T> multi = Operator<T>.Multiply;
            Func<T, T, T> add = Operator<T>.Add;

            int len0 = m0.GetLength(0);
            int len1 = m1.GetLength(1);
            int len = m0.GetLength(1);
            T[,] m = new T[len0, len1];

            for (int idx0 = 0; idx0 < len0; idx0++) // First dimension
                for (int idx1 = 0; idx1 < len1; idx1++) // Second dimension
                    for (int idx = 0; idx < len; idx++) // Inner dimension
                        m[idx0, idx1] = add(m[idx0, idx1], 
                            multi(m0[idx0, idx], m1[idx, idx1]));
            return m;
        }

        /// <summary>
        /// Matrix multiplication. Functionally m1 applies after m2.
        /// </summary>
        /// <param name="m0"> First matrix (A of A*B) </param>
        /// <param name="m1"> Second matrix (B of A*B) </param>
        /// <returns> Product matrix </returns>
        public static int[,] MatrixMultiply(int[,] m0, int[,] m1)
        {
            // Dimension check
            if (m0.GetLength(1) != m1.GetLength(0))
                throw new System.ArgumentException(
                    "Inner matrix dimensions must agree.", "arr1");

            int[,] gpuProduct;
            if (TryMatrixGpu(m0, m1, out gpuProduct)) return gpuProduct;

            int len0 = m0.GetLength(0);
            int len1 = m1.GetLength(1);
            int len = m0.GetLength(1);
            int[,] m = new int[len0, len1];

            for (int idx0 = 0; idx0 < len0; idx0++) // First dimension
                for (int idx1 = 0; idx1 < len1; idx1++) // Second dimension
                    for (int idx = 0; idx < len; idx++) // Inner dimension
                        m[idx0, idx1] = m[idx0, idx1] + m0[idx0, idx] * 
                            m1[idx, idx1];
            return m;
        }

        /// <summary>
        /// Matrix multiplication. Functionally m1 applies after m2.
        /// </summary>
        /// <param name="m0"> First matrix (A of A*B) </param>
        /// <param name="m1"> Second matrix (B of A*B) </param>
        /// <returns> Product matrix </returns>
        public static double[,] MatrixMultiply(double[,] m0, double[,] m1)
        {
            // Dimension check
            if (m0.GetLength(1) != m1.GetLength(0))
                throw new System.ArgumentException(
                    "Inner matrix dimensions must agree.", "arr1");

            double[,] gpuProduct;
            if (TryMatrixGpu(m0, m1, out gpuProduct)) return gpuProduct;

            int len0 = m0.GetLength(0);
            int len1 = m1.GetLength(1);
            int len = m0.GetLength(1);
            double[,] m = new double[len0, len1];

            for (int idx0 = 0; idx0 < len0; idx0++) // First dimension
                for (int idx1 = 0; idx1 < len1; idx1++) // Second dimension
                    for (int idx = 0; idx < len; idx++) // Inner dimension
                        m[idx0, idx1] = m[idx0, idx1] + m0[idx0, idx] * 
                            m1[idx, idx1];
            return m;
        }

        # endregion 2 Dimensional Arrays
        # region 1 Dimensional Arrays

        /// <summary>
        /// Matrix multiplication. Functionally m1 applies after m2.
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="m0"> First matrix (A of A*B) </param>
        /// <param name="m1"> Second matrix (B of A*B) </param>
        /// <returns> Product matrix </returns>
        public static T[] MatrixMultiply<T>(T[,] m0, T[] m1)
        {
            // Dimension check
            if (m0.GetLength(1) != m1.Length)
                throw new System.ArgumentException(
                    "Inner matrix dimensions must agree.", "arr1");

            if (typeof(T) == typeof(double))
                return (T[])(object)MatrixMultiply((double[,])(object)m0, (double[])(object)m1);
            if (typeof(T) == typeof(int))
                return (T[])(object)MatrixMultiply((int[,])(object)m0, (int[])(object)m1);

            // Function delegates
            Func<T, T, T> multi = Operator<T>.Multiply;
            Func<T, T, T> add = Operator<T>.Add;

            int len0 = m0.GetLength(0);
            int len = m0.GetLength(1);
            T[] m = new T[len0];

            for (int idx0 = 0; idx0 < len0; idx0++) // First dimension
                    for (int idx = 0; idx < len; idx++) // Inner dimension
                        m[idx0] = add(m[idx0], 
                            multi(m0[idx0, idx], m1[idx]));
            return m;
        }

        /// <summary>
        /// Matrix multiplication. Functionally m1 applies after m2.
        /// </summary>
        /// <param name="m0"> First matrix (A of A*B) </param>
        /// <param name="m1"> Second matrix (B of A*B) </param>
        /// <returns> Product matrix </returns>
        public static int[] MatrixMultiply(int[,] m0, int[] m1)
        {
            // Dimension check
            if (m0.GetLength(1) != m1.Length)
                throw new System.ArgumentException(
                    "Inner matrix dimensions must agree.", "arr1");

            int[] gpuProduct;
            if (TryMatrixGpu(m0, m1, out gpuProduct)) return gpuProduct;

            int len0 = m0.GetLength(0);
            int len = m0.GetLength(1);
            int[] m = new int[len0];

            for (int idx0 = 0; idx0 < len0; idx0++) // First dimension
                for (int idx = 0; idx < len; idx++) // Inner dimension
                    m[idx0] = m[idx0] + m0[idx0, idx] * m1[idx];
            return m;
        }

        /// <summary>
        /// Matrix multiplication. Functionally m1 applies after m2.
        /// </summary>
        /// <param name="m0"> First matrix (A of A*B) </param>
        /// <param name="m1"> Second matrix (B of A*B) </param>
        /// <returns> Product matrix </returns>
        public static double[] MatrixMultiply(double[,] m0, double[] m1)
        {
            // Dimension check
            if (m0.GetLength(1) != m1.Length)
                throw new System.ArgumentException(
                    "Inner matrix dimensions must agree.", "arr1");

            double[] gpuProduct;
            if (TryMatrixGpu(m0, m1, out gpuProduct)) return gpuProduct;

            int len0 = m0.GetLength(0);
            int len = m0.GetLength(1);
            double[] m = new double[len0];

            for (int idx0 = 0; idx0 < len0; idx0++) // First dimension
                for (int idx = 0; idx < len; idx++) // Inner dimension
                    m[idx0] = m[idx0] + m0[idx0, idx] * m1[idx];
            return m;
        }

        # endregion 1 Dimensional Arrays

    }
}
