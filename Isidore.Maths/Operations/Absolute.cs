using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Isidore.Maths
{

    /// <summary>
    /// Provides operators for scalar and array data types
    /// </summary>
    public static partial class Operator
    {
        # region Integer Arrays

        /// <summary>
        /// Absolute value of an array
        /// </summary>
        /// <param name="arr"> Array </param>
        /// <returns> Absolute value of the array </returns>
        public static int[] Absolute(int[] arr)
        {
            int[] arrOut = (int[])arr.Clone();
            for (int idx0 = 0; idx0 < arrOut.Length; idx0++)
                arrOut[idx0] = Math.Abs(arr[idx0]);
            return arrOut;
        }

        /// <summary>
        /// Absolute value of an array
        /// </summary>
        /// <param name="arr"> Array </param>
        /// <returns> Absolute value of the array </returns>
        public static int[,] Absolute(int[,] arr)
        {
            int[,] arrOut = (int[,])arr.Clone();
            int len1 = arr.GetLength(0);
            int len2 = arr.GetLength(1);

            // Partitions source array by columns
            if (arrOut.Length == 0)
                return arrOut;
            OrderablePartitioner<Tuple<int, int>> part = Partitioner.Create(0, len1); 
            Parallel.ForEach(part, (range) =>
            {
                for (int idx0 = range.Item1; idx0 < range.Item2; idx0++)
                    for (int idx1 = 0; idx1 < len2; idx1++)
                        arrOut[idx0, idx1] = Math.Abs(arr[idx0, idx1]);
            });

            //for (int idx0 = 0; idx0 < len1; idx0++)
            //    for (int idx1 = 0; idx0 < len2; idx0++)
            //        arrOut[idx0, idx1] = Math.Abs(arr[idx0,idx1]);
            return arrOut;
        }

        # endregion Integer Arrays
        # region Double Arrays

        /// <summary>
        /// Absolute value of an array
        /// </summary>
        /// <param name="arr"> Array </param>
        /// <returns> Absolute value of the array </returns>
        public static double[] Absolute(double[] arr)
        {
            double[] arrOut = (double[])arr.Clone();

            // Partitions source array by columns
            //var part = Partitioner.Create(0, arrOut.Length);
            //Parallel.ForEach(part, (range) =>
            //{
            //    for (int idx0 = range.Item1; idx0 < range.Item2; idx0++)
            //            arrOut[idx0] = Math.Abs(arr[idx0]);
            //});

            for (int idx0 = 0; idx0 < arrOut.Length; idx0++)
                arrOut[idx0] = Math.Abs(arr[idx0]);
            return arrOut;
        }

        /// <summary>
        /// Absolute value of an array
        /// </summary>
        /// <param name="arr"> Array </param>
        /// <returns> Absolute value of the array </returns>
        public static double[,] Absolute(double[,] arr)
        {
            double[,] arrOut = (double[,])arr.Clone();
            int len1 = arr.GetLength(0);
            int len2 = arr.GetLength(1);

            // Partitions source array by columns
            if (arrOut.Length == 0)
                return arrOut;
            OrderablePartitioner<Tuple<int, int>> part = Partitioner.Create(0, len1);
            Parallel.ForEach(part, (range) =>
            {
                for (int idx0 = range.Item1; idx0 < range.Item2; idx0++)
                    for (int idx1 = 0; idx1 < len2; idx1++)
                        arrOut[idx0, idx1] = Math.Abs(arr[idx0, idx1]);
            });

            //for (int idx0 = 0; idx0 < len1; idx0++)
            //    for (int idx1 = 0; idx0 < len2; idx0++)
            //        arrOut[idx0, idx1] = Math.Abs(arr[idx0,idx1]);
            return arrOut;
        }

        # endregion Double Arrays

        # region Generic Expressions

        /// <summary>
        /// Absolute value of a value
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="val"> value </param>
        /// <returns> Absolute value </returns>
        public static T Absolute<T>(T val) 
        { 
            return Operator<T>.Absolute(val); 
        }

        /// <summary>
        /// Absolute value of an array
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="arr"> Array </param>
        /// <returns> Absolute value of the array </returns>
        public static T[] Absolute<T>(T[] arr)
        {
            return Element.Op(Operator.Absolute, arr);
        }

        /// <summary>
        /// Absolute value of an array (Multiples by -1)
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="arr"> Array </param>
        /// <returns> Absolute value of the array </returns>
        public static T[,] Absolute<T>(T[,] arr) 
        { 
            return Element.Op(Operator.Absolute, arr); 
        }

        # endregion Generic Expressions
    }
}
