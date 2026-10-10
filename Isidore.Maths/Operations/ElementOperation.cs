using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Isidore.Maths
{
    /// <summary>
    /// Provides methods for performing element-wise operations on arrays
    /// </summary>
    public class Element
    {
        /// <summary>
        /// Applies the function, Op, to each element of array arr0.
        /// </summary>
        /// <typeparam name="T"> Input delegate type </typeparam>
        /// <typeparam name="Tout"> Output delegate type </typeparam>
        /// <param name="Op"> Operation delegate function </param>
        /// <param name="arr0"> 2D array to operate on </param>
        /// <returns> Modified array </returns>
        public static Tout[,] Op<T, Tout>(Func<T, Tout> Op, T[,] arr0)
        {
            int dim0 = arr0.GetLength(0);
            int dim1 = arr0.GetLength(1);
            Tout[,] result = new Tout[dim0, dim1];

            // Partitions source array by columns
            if (result.Length == 0)
                return result;
            OrderablePartitioner<Tuple<int, int>> part = Partitioner.Create(0, dim0); 
            Parallel.ForEach(part, (range) =>
            {
                for (int idx0 = range.Item1; idx0 < range.Item2; idx0++)
                    for (int idx1 = 0; idx1 <dim1; idx1++)
                        result[idx0, idx1] = Op(arr0[idx0, idx1]);
            });

            //for (int i0 = 0; i0 < dim0; i0++)
            //    for (int i1 = 0; i1 < dim1; i1++)
            //        result[i0, i1] = Op(arr0[i0, i1]);
            return result;
        }

        /// <summary>
        /// Applies the function, Op, to each element of arrays arr0 and 
        /// arr1 to generate a third array.
        /// </summary>
        /// <typeparam name="T"> Both inputs' delegate type </typeparam>
        /// <typeparam name="Tout"> Output delegate type </typeparam>
        /// <param name="Op"> Operation delegate function </param>
        /// <param name="arr0"> First 2D array </param>
        /// <param name="arr1"> Second 2D array </param>
        /// <returns> Third array after Op is applied to 
        /// arr0 and arr1 </returns>
        public static Tout[,] Op<T, Tout>(Func<T, T, Tout> Op, T[,] arr0, 
            T[,] arr1)
        {
            int sizeCheck = Compare.Size<double, double>(arr0, arr1);
            if (sizeCheck == 1)
                throw new System.ArgumentException(
                    "Array ranks do not match.", "arr0");
            if (sizeCheck == 2)
                throw new System.ArgumentException(
                    "Array sizes do not match.", "arr0");

            int dim0 = arr0.GetLength(0);
            int dim1 = arr0.GetLength(1);
            Tout[,] result = new Tout[dim0, dim1];

            // Partitions source array by columns
            if (result.Length == 0)
                return result;
            OrderablePartitioner<Tuple<int, int>> part = Partitioner.Create(0, dim0); 
            Parallel.ForEach(part, (range) =>
            {
                for (int idx0 = range.Item1; idx0 < range.Item2; idx0++)
                    for (int idx1 = 0; idx1 < dim1; idx1++)
                        result[idx0, idx1] = Op(arr0[idx0, idx1], 
                            arr1[idx0, idx1]);
            });

            //for (int i0 = 0; i0 < dim0; i0++)
            //    for (int i1 = 0; i1 < dim1; i1++)
            //        result[i0, i1] = Op(arr0[i0, i1], arr1[i0, i1]);
            return result;
        }

        /// <summary>
        /// Applies the function, Op, to each element of arrays arr0 and a 
        /// single element value, value1, to generate a second array.
        /// </summary>
        /// <typeparam name="T"> Both the array's and value's delegate
        /// type </typeparam>
        /// <typeparam name="Tout"> Output delegate type </typeparam>
        /// <param name="Op"> Operation delegate function </param>
        /// <param name="arr0"> 2D array </param>
        /// <param name="value1"> Scalar value </param>
        /// <returns> Second array after Op is applied to arr0 
        /// and value1 </returns>
        public static Tout[,] Op<T, Tout>(Func<T, T, Tout> Op, T[,] arr0, 
            T value1)
        {
            int dim0 = arr0.GetLength(0);
            int dim1 = arr0.GetLength(1);
            Tout[,] result = new Tout[dim0, dim1];

            // Partitions source array by columns
            if (result.Length == 0)
                return result;
            OrderablePartitioner<Tuple<int, int>> part = Partitioner.Create(0, dim0); 
            Parallel.ForEach(part, (range) =>
            {
                for (int idx0 = range.Item1; idx0 < range.Item2; idx0++)
                    for (int idx1 = 0; idx1 < dim1; idx1++)
                        result[idx0, idx1] = Op(arr0[idx0, idx1], value1);
            });
            return result;
        }

        /// <summary>
        /// Applies the function, Op, to single element value, value0, and 
        /// each element of arrays arr1 to generate a second array.
        /// </summary>
        /// <typeparam name="T"> Both the array's and value's delegate 
        /// type </typeparam>
        /// <typeparam name="Tout"> Output delegate type </typeparam>
        /// <param name="Op"> Operation delegate function </param>
        /// <param name="value0"> Scalar value </param>
        /// <param name="arr1"> 2D array </param>
        /// <returns>Second array after Op is applied to value0 and 
        /// arr1 </returns>
        public static Tout[,] Op<T, Tout>(Func<T, T, Tout> Op, T value0, 
            T[,] arr1)
        {
            int dim0 = arr1.GetLength(0);
            int dim1 = arr1.GetLength(1);
            Tout[,] result = new Tout[dim0, dim1];

            // Partitions source array by columns
            if (result.Length == 0)
                return result;
            OrderablePartitioner<Tuple<int, int>> part = Partitioner.Create(0, dim0); 
            Parallel.ForEach(part, (range) =>
            {
                for (int idx0 = range.Item1; idx0 < range.Item2; idx0++)
                    for (int idx1 = 0; idx1 < dim1; idx1++)
                        result[idx0, idx1] = Op(value0, arr1[idx0, idx1]);
            });

            //for (int i0 = 0; i0 < dim0; i0++)
            //    for (int i1 = 0; i1 < dim1; i1++)
            //        result[i0, i1] = Op(value0, arr1[i0, i1]);
            return result;
        }

        /// <summary>
        /// Applies the function, Op, to each element of array arr0.
        /// </summary>
        /// <typeparam name="T"> Input delegate type </typeparam>
        /// <typeparam name="Tout"> Output delegate type </typeparam>
        /// <param name="Op"> Operation delegate function </param>
        /// <param name="arr0"> 1D array to operate on </param>
        /// <returns> Modified array </returns>
        public static Tout[] Op<T, Tout>(Func<T, Tout> Op, T[] arr0)
        {
            int dim0 = arr0.GetLength(0);
            Tout[] result = new Tout[dim0];

            for (int i0 = 0; i0 < dim0; i0++)
                result[i0] = Op(arr0[i0]);
            return result;
        }

        /// <summary>
        /// Applies the function, Op, to each element of arrays arr0 and 
        /// arr1 to generate a third array.
        /// </summary>
        /// <typeparam name="T"> Both inputs' delegate type </typeparam>
        /// <typeparam name="Tout"> Output delegate type </typeparam>
        /// <param name="Op"> Operation delegate function </param>
        /// <param name="arr0"> First 2D array </param>
        /// <param name="arr1"> Second 2D array </param>
        /// <returns> Third array after Op is applied to arr0 and 
        /// arr1 </returns>
        public static Tout[] Op<T, Tout>(Func<T, T, Tout> Op, T[] arr0, 
            T[] arr1)
        {
            if (arr0.Length != arr1.Length)
                throw new System.ArgumentException(
                    "Array sizes do not match.", "arr0");

            int dim0 = arr0.Length;
            Tout[] result = new Tout[dim0];

            for (int i0 = 0; i0 < dim0; i0++)
                result[i0] = Op(arr0[i0], arr1[i0]);
            return result;
        }

        /// <summary>
        /// Applies the function, Op, to each element of arrays arr0 and a
        /// single element value, value1, to generate a second array.
        /// </summary>
        /// <typeparam name="T"> Both the array's and value's delegate 
        /// type </typeparam>
        /// <typeparam name="Tout"> Output delegate type </typeparam>
        /// <param name="Op"> Operation delegate function </param>
        /// <param name="arr0"> 2D array </param>
        /// <param name="value1"> Scalar value </param>
        /// <returns> Second array after Op is applied to arr0 and 
        /// value1 </returns>
        public static Tout[] Op<T, Tout>(Func<T, T, Tout> Op, T[] arr0, 
            T value1)
        {
            int dim0 = arr0.Length;
            Tout[] result = new Tout[dim0];

            for (int i0 = 0; i0 < dim0; i0++)
                result[i0] = Op(arr0[i0], value1);
            return result;
        }

        /// <summary>
        /// Applies the function, Op, to single element value, value0, 
        /// and each element of arrays arr1 to generate a second array.
        /// </summary>
        /// <typeparam name="T"> Both the array's and value's 
        /// delegate type </typeparam>
        /// <typeparam name="Tout"> Output delegate type </typeparam>
        /// <param name="Op"> Operation delegate function </param>
        /// <param name="value0"> Scalar value </param>
        /// <param name="arr1"> 2D array </param>
        /// <returns>Second array after Op is applied to value0 and 
        /// arr1 </returns>
        public static Tout[] Op<T, Tout>(Func<T, T, Tout> Op, T value0, 
            T[] arr1)
        {
            int dim0 = arr1.Length;
            Tout[] result = new Tout[dim0];

            for (int i0 = 0; i0 < dim0; i0++)
                result[i0] = Op(value0, arr1[i0]);
            return result;
        }
    }
}
