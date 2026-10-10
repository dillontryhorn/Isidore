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
        /// Subtracts two arrays
        /// </summary>
        /// <param name="arr1"> Array 1 </param>
        /// <param name="arr2"> Array 2 </param>
        /// <returns> Array difference </returns>
        public static int[] Subtract(int[] arr1, int[] arr2)
        {
            int sizeCheck = Compare.Size<int, int>(arr1, arr2);
            if (sizeCheck == 1)
                throw new System.ArgumentException(
                    "Array ranks do not match.", "arr1");
            if (sizeCheck == 2)
                throw new System.ArgumentException(
                    "Array sizes do not match.", "arr1");

            int len1 = arr1.Length;
            int[] arrOut = new int[len1];

            // Partitions source array by columns
            //var part = Partitioner.Create(0, len1); 
            //Parallel.ForEach(part, (range) =>
            //{
            //    for (int idx0 = range.Item1; idx0 < range.Item2; idx0++)
            //            arrOut[idx0] = arr1[idx0] - arr2[idx0];
            //});

            for (int idx0 = 0; idx0 < len1; idx0++)
                arrOut[idx0] = arr1[idx0] - arr2[idx0];
            return arrOut;
        }

        /// <summary>
        /// Subtracts a scalar value from an array
        /// </summary>
        /// <param name="arr"> Array </param>
        /// <param name="val"> Scalar value </param>
        /// <returns> Array difference </returns>
        public static int[] Subtract(int[] arr, int val)
        {
            int len1 = arr.Length;
            int[] arrOut = new int[len1];

            // Partitions source array by columns
            //var part = Partitioner.Create(0, len1); 
            //Parallel.ForEach(part, (range) =>
            //{
            //    for (int idx0 = range.Item1; idx0 < range.Item2; idx0++)
            //        arrOut[idx0] = arr[idx0] - val;
            //});

            for (int idx0 = 0; idx0 < len1; idx0++)
                arrOut[idx0] = arr[idx0] - val;
            return arrOut;
        }

        /// <summary>
        /// Subtracts an array from a scalar value
        /// </summary>
        /// <param name="val"> Array </param>
        /// <param name="arr"> Scalar value </param>
        /// <returns> Array difference </returns>
        public static int[] Subtract(int val, int[] arr)
        {
            int len1 = arr.Length;
            int[] arrOut = new int[len1];

            // Partitions source array by columns
            //var part = Partitioner.Create(0, len1); 
            //Parallel.ForEach(part, (range) =>
            //{
            //    for (int idx0 = range.Item1; idx0 < range.Item2; idx0++)
            //        arrOut[idx0] = val - arr[idx0];
            //});

            for (int idx0 = 0; idx0 < len1; idx0++)
                arrOut[idx0] = val - arr[idx0];
            return arrOut;
        }

        /// <summary>
        /// Subtracts two arrays
        /// </summary>
        /// <param name="arr1"> Array 1 </param>
        /// <param name="arr2"> Array 2 </param>
        /// <returns> Array difference </returns>
        public static int[,] Subtract(int[,] arr1, int[,] arr2)
        {
            int sizeCheck = Compare.Size<double, double>(arr1, arr2);
            if (sizeCheck == 1)
                throw new System.ArgumentException(
                    "Array ranks do not match.", "arr1");
            if (sizeCheck == 2)
                throw new System.ArgumentException(
                    "Array sizes do not match.", "arr1");

            int len1 = arr1.GetLength(0);
            int len2 = arr1.GetLength(1);
            int[,] arrOut = new int[len1, len2];

            // Partitions source array by columns
            if (arrOut.Length == 0)
                return arrOut;
            OrderablePartitioner<System.Tuple<int, int>> part = Partitioner.Create(0, len1); 
            Parallel.ForEach(part, (range) =>
            {
                for (int idx0 = range.Item1; idx0 < range.Item2; idx0++)
                    for (int idx1 = 0; idx1 < len2; idx1++)
                        arrOut[idx0, idx1] = arr1[idx0, idx1] - 
                        arr2[idx0, idx1];
            });

            //for (int idx0 = 0; idx0 < len1; idx0++)
            //    for (int idx1 = 0; idx1 < len2; idx1++)
            //        arrOut[idx0, idx1] = arr1[idx0, idx1] - 
            //        arr2[idx0, idx1];
            return arrOut;
        }

        /// <summary>
        /// Subtracts a scalar value from an array
        /// </summary>
        /// <param name="arr"> Array </param>
        /// <param name="val"> Scalar value </param>
        /// <returns> Array difference </returns>
        public static int[,] Subtract(int[,] arr, int val)
        {
            int len1 = arr.GetLength(0);
            int len2 = arr.GetLength(1);
            int[,] arrOut = new int[len1, len2];

            // Partitions source array by columns
            if (arrOut.Length == 0)
                return arrOut;
            OrderablePartitioner<System.Tuple<int, int>> part = Partitioner.Create(0, len1); 
            Parallel.ForEach(part, (range) =>
            {
                for (int idx0 = range.Item1; idx0 < range.Item2; idx0++)
                    for (int idx1 = 0; idx1 < len2; idx1++)
                        arrOut[idx0, idx1] = arr[idx0, idx1] - val;
            });

            //for (int idx0 = 0; idx0 < len1; idx0++)
            //    for (int idx1 = 0; idx1 < len2; idx1++)
            //        arrOut[idx0, idx1] = arr[idx0, idx1] - val;
            return arrOut;
        }

        /// <summary>
        /// Subtracts an array from a scalar value
        /// </summary>
        /// <param name="val"> Scalar values </param>
        /// <param name="arr"> Array </param>
        /// <returns> Array difference</returns>
        public static int[,] Subtract(int val, int[,] arr)
        {
            int len1 = arr.GetLength(0);
            int len2 = arr.GetLength(1);
            int[,] arrOut = new int[len1, len2];

            // Partitions source array by columns
            if (arrOut.Length == 0)
                return arrOut;
            OrderablePartitioner<System.Tuple<int, int>> part = Partitioner.Create(0, len1); 
            Parallel.ForEach(part, (range) =>
            {
                for (int idx0 = range.Item1; idx0 < range.Item2; idx0++)
                    for (int idx1 = 0; idx1 < len2; idx1++)
                        arrOut[idx0, idx1] = val - arr[idx0, idx1];
            });

            //for (int idx0 = 0; idx0 < len1; idx0++)
            //    for (int idx1 = 0; idx1 < len2; idx1++)
            //        arrOut[idx0, idx1] = val - arr[idx0, idx1];
            return arrOut;
        }

        # endregion Integer Arrays
        # region Double Arrays

        /// <summary>
        /// Subtracts two arrays
        /// </summary>
        /// <param name="arr1"> Array 1 </param>
        /// <param name="arr2"> Array 2 </param>
        /// <returns>  Array difference </returns>
        public static double[] Subtract(double[] arr1, double[] arr2)
        {
            if (arr1.Length != arr2.Length)
                throw new System.ArgumentException(
                    "Array sizes do not match.", "arr1");

            int len1 = arr1.Length;
            double[] arrOut = new double[len1];

            // Partitions source array by columns
            //var part = Partitioner.Create(0, len1);
            //Parallel.ForEach(part, (range) =>
            //{
            //    for (int idx0 = range.Item1; idx0 < range.Item2; idx0++)
            //        arrOut[idx0] = arr1[idx0] - arr2[idx0];
            //});

            for (int idx0 = 0; idx0 < len1; idx0++)
                arrOut[idx0] = arr1[idx0] - arr2[idx0];
            return arrOut;
        }

        /// <summary>
        /// Subtracts a scalar value from an array
        /// </summary>
        /// <param name="arr"> Array </param>
        /// <param name="val"> Scalar value </param>
        /// <returns> Array difference </returns>
        public static double[] Subtract(double[] arr, double val)
        {
            int len1 = arr.Length;
            double[] arrOut = new double[len1];

            // Partitions source array by columns
            //var part = Partitioner.Create(0, len1); 
            //Parallel.ForEach(part, (range) =>
            //{
            //    for (int idx0 = range.Item1; idx0 < range.Item2; idx0++)
            //        arrOut[idx0] = arr[idx0] - val;
            //});

            for (int idx0 = 0; idx0 < len1; idx0++)
                arrOut[idx0] = arr[idx0] - val;
            return arrOut;
        }

        /// <summary>
        /// Subtracts an array from a scalar value
        /// </summary>
        /// <param name="val"> Array </param>
        /// <param name="arr"> Scalar value </param>
        /// <returns> Array difference </returns>
        public static double[] Subtract(double val, double[] arr)
        {
            int len1 = arr.Length;
            double[] arrOut = new double[len1];

            // Partitions source array by columns
            //var part = Partitioner.Create(0, len1);
            //Parallel.ForEach(part, (range) =>
            //{
            //    for (int idx0 = range.Item1; idx0 < range.Item2; idx0++)
            //        arrOut[idx0] = val - arr[idx0];
            //});

            for (int idx0 = 0; idx0 < len1; idx0++)
                arrOut[idx0] = val - arr[idx0];
            return arrOut;
        }

        /// <summary>
        /// Subtracts two arrays
        /// </summary>
        /// <param name="arr1"> Array 1 </param>
        /// <param name="arr2"> Array 2 </param>
        /// <returns> Array difference </returns>
        public static double[,] Subtract(double[,] arr1, double[,] arr2)
        {
            int sizeCheck = Compare.Size<double, double>(arr1, arr2);
            if (sizeCheck == 1)
                throw new System.ArgumentException(
                    "Array ranks do not match.", "arr1");
            if (sizeCheck == 2)
                throw new System.ArgumentException(
                    "Array sizes do not match.", "arr1");

            int len1 = arr1.GetLength(0);
            int len2 = arr1.GetLength(1);
            double[,] arrOut = new double[len1, len2];

            // Partitions source array by columns
            if (arrOut.Length == 0)
                return arrOut;
            OrderablePartitioner<System.Tuple<int, int>> part = Partitioner.Create(0, len1);
            Parallel.ForEach(part, (range) =>
            {
                for (int idx0 = range.Item1; idx0 < range.Item2; idx0++)
                    for (int idx1 = 0; idx1 < len2; idx1++)
                        arrOut[idx0, idx1] = arr1[idx0, idx1] - 
                        arr2[idx0, idx1];
            });

            //for (int idx0 = 0; idx0 < len1; idx0++)
            //    for (int idx1 = 0; idx1 < len2; idx1++)
            //        arrOut[idx0, idx1] = arr1[idx0, idx1] - 
            //        arr2[idx0, idx1];
            return arrOut;
        }

        /// <summary>
        /// Subtracts a scalar value from an array
        /// </summary>
        /// <param name="arr"> Array </param>
        /// <param name="val"> Scalar value </param>
        /// <returns> Array difference </returns>
        public static double[,] Subtract(double[,] arr, double val)
        {
            int len1 = arr.GetLength(0);
            int len2 = arr.GetLength(1);
            double[,] arrOut = new double[len1, len2];

            // Partitions source array by columns
            if (arrOut.Length == 0)
                return arrOut;
            OrderablePartitioner<System.Tuple<int, int>> part = Partitioner.Create(0, len1);
            Parallel.ForEach(part, (range) =>
            {
                for (int idx0 = range.Item1; idx0 < range.Item2; idx0++)
                    for (int idx1 = 0; idx1 < len2; idx1++)
                        arrOut[idx0, idx1] = arr[idx0, idx1] - val;
            });

            //for (int idx0 = 0; idx0 < len1; idx0++)
            //    for (int idx1 = 0; idx1 < len2; idx1++)
            //        arrOut[idx0, idx1] = arr[idx0, idx1] - val;
            return arrOut;
        }

        /// <summary>
        /// Subtracts an array from a scalar value
        /// </summary>
        /// <param name="val"> Scalar value </param>
        /// <param name="arr"> Array </param>
        /// <returns> Array difference </returns>
        public static double[,] Subtract(double val, double[,] arr)
        {
            int len1 = arr.GetLength(0);
            int len2 = arr.GetLength(1);
            double[,] arrOut = new double[len1, len2];

            // Partitions source array by columns
            if (arrOut.Length == 0)
                return arrOut;
            OrderablePartitioner<System.Tuple<int, int>> part = Partitioner.Create(0, len1);
            Parallel.ForEach(part, (range) =>
            {
                for (int idx0 = range.Item1; idx0 < range.Item2; idx0++)
                    for (int idx1 = 0; idx1 < len2; idx1++)
                        arrOut[idx0, idx1] = val - arr[idx0, idx1];
            });

            //for (int idx0 = 0; idx0 < len1; idx0++)
            //    for (int idx1 = 0; idx1 < len2; idx1++)
            //        arrOut[idx0, idx1] = val - arr[idx0, idx1];
            return arrOut;
        }

        # endregion Double Arrays

        # region Generic Expressions

        /// <summary>
        /// Subtracts two variables
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="value1"> Variable 1 </param>
        /// <param name="value2"> Variable 2</param>
        /// <returns> Array difference </returns>
        public static T Subtract<T>(T value1, T value2)
        {
            return Operator<T>.Subtract(value1, value2);
        }

        /// <summary>
        /// Subtracts two arrays
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="arr0"> Array 1 </param>
        /// <param name="arr1"> Array 2 </param>
        /// <returns> Array difference </returns>
        public static T[] Subtract<T>(T[] arr0, T[] arr1)
        {
            return Element.Op(Operator<T>.Subtract, arr0, arr1);
        }

        /// <summary>
        /// Subtracts a scalar variable from an array
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="arr"> Array </param>
        /// <param name="val"> Scalar value </param>
        /// <returns> Array difference </returns>
        public static T[] Subtract<T>(T[] arr, T val)
        {
            return Element.Op(Operator<T>.Subtract, arr, val);
        }

        /// <summary>
        /// Subtracts an array from a scalar variable
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="val"> Scalar value </param>
        /// <param name="arr"> Array </param>
        /// <returns> Array difference </returns>
        public static T[] Subtract<T>(T val, T[] arr)
        {
            return Element.Op(Operator<T>.Subtract, val, arr);
        }

        /// <summary>
        /// Subtracts two arrays
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="arr0"> Array 1 </param>
        /// <param name="arr1"> Array 2 </param>
        /// <returns> Summed Array </returns>
        public static T[,] Subtract<T>(T[,] arr0, T[,] arr1)
        {
            return Element.Op(Operator<T>.Subtract, arr0, arr1);
        }

        /// <summary>
        /// Subtracts a scalar variable from an array
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="arr"> Array </param>
        /// <param name="val"> Scalar value </param>
        /// <returns> Array difference </returns>
        public static T[,] Subtract<T>(T[,] arr, T val)
        {
            return Element.Op(Operator<T>.Subtract, arr, val);
        }

        /// <summary>
        /// Subtracts an array from a scalar variable
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="val"> Scalar value </param>
        /// <param name="arr"> Array </param>
        /// <returns> Array difference </returns>
        public static T[,] Subtract<T>(T val, T[,] arr)
        {
            return Element.Op(Operator<T>.Subtract, val, arr);
        }

        # endregion Generic Expressions
    }
}
