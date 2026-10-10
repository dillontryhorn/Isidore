using System;
//using System.Collections.Generic;
using System.Collections.Concurrent; // TO access Partitioner
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace Isidore.Maths
{

    /// <summary>
    /// Provides operators for scalar and array data types
    /// </summary>
    public static partial class Operator
    {
        # region Integer Arrays

        /// <summary>
        /// Adds two arrays
        /// </summary>
        /// <param name="arr1"> Array 1 </param>
        /// <param name="arr2"> Array 2 </param>
        /// <returns> Array sum </returns>
        public static int[] Add(int[] arr1, int[] arr2)
        {
            if (arr1.Length != arr2.Length)
                throw new System.ArgumentException(
                    "Array sizes do not match.", "arr1");

            int len1 = arr1.Length;
            int[] arrOut = new int[len1];

            // Partitions source array by columns
            //var part = Partitioner.Create(0, len1);
            //Parallel.ForEach(part, (range) =>
            //{
            //    for (int idx0 = range.Item1; idx0 < range.Item2; idx0++)
            //        arrOut[idx0] = arr1[idx0] + arr2[idx0];
            //});

            for (int idx0 = 0; idx0 < len1; idx0++)
                arrOut[idx0] = arr1[idx0] + arr2[idx0];
            return arrOut;
        }

        /// <summary>
        /// Adds a scalar value to an array
        /// </summary>
        /// <param name="arr"> Array </param>
        /// <param name="val"> Scalar value </param>
        /// <returns> Array sum </returns>
        public static int[] Add(int[] arr, int val)
        {
            int len1 = arr.Length;
            int[] arrOut = new int[len1];

            // Partitions source array by columns
            //var part = Partitioner.Create(0, len1);
            //Parallel.ForEach(part, (range) =>
            //{
            //    for (int idx0 = range.Item1; idx0 < range.Item2; idx0++)
            //            arrOut[idx0] = arr[idx0] + val;
            //});

            for (int idx0 = 0; idx0 < len1; idx0++)
                arrOut[idx0] = arr[idx0] + val;
            return arrOut;
        }

        /// <summary>
        /// Adds a scalar value to an array
        /// </summary>
        /// <param name="val"> Array </param>
        /// <param name="arr"> Scalar value </param>
        /// <returns> Array sum </returns>
        public static int[] Add(int val, int[] arr) 
        { 
            return Add(arr, val); 
        }

        /// <summary>
        /// Adds two arrays
        /// </summary>
        /// <param name="arr1"> Array 1 </param>
        /// <param name="arr2"> Array 2 </param>
        /// <returns> Array sum </returns>
        public static int[,] Add(int[,] arr1, int[,] arr2)
        {
            int sizeCheck = Compare.Size<int, int>(arr1, arr2);
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
            OrderablePartitioner<Tuple<int, int>> part = Partitioner.Create(0, len1);
            Parallel.ForEach(part, (range) =>
            {
                for (int idx0 = range.Item1; idx0 < range.Item2; idx0++)
                    for (int idx1 = 0; idx1 < len2; idx1++)
                        arrOut[idx0, idx1] = arr1[idx0, idx1] + 
                        arr2[idx0, idx1];
            });

            //for (int idx0 = 0; idx0 < len1; idx0++)
            //    for (int idx1 = 0; idx1 < len2; idx1++)
            //        arrOut[idx0, idx1] = arr1[idx0, idx1] + 
            //        arr2[idx0, idx1];
            return arrOut;
        }

        /// <summary>
        /// Adds a scalar value to an array
        /// </summary>
        /// <param name="arr"> Array </param>
        /// <param name="val"> Scalar value </param>
        /// <returns> Array sum </returns>
        public static int[,] Add(int[,] arr, int val)
        {
            int len1 = arr.GetLength(0);
            int len2 = arr.GetLength(1);
            int[,] arrOut = new int[len1, len2];

            // Partitions source array by columns
            if (arrOut.Length == 0)
                return arrOut;
            OrderablePartitioner<Tuple<int, int>> part = Partitioner.Create(0, len1);
            Parallel.ForEach(part, (range) =>
            {
                for (int idx0 = range.Item1; idx0 < range.Item2; idx0++)
                    for (int idx1 = 0; idx1 < len2; idx1++)
                        arrOut[idx0, idx1] = arr[idx0, idx1] + val;
            });

            //for (int idx0 = 0; idx0 < len1; idx0++)
            //    for (int idx1 = 0; idx1 < len2; idx1++)
            //        arrOut[idx0, idx1] = arr[idx0, idx1] + val;
            return arrOut;
        }

        /// <summary>
        /// Adds a scalar value to an array
        /// </summary>
        /// <param name="val"> Scalar values </param>
        /// <param name="arr"> Array </param>
        /// <returns> Array sum</returns>
        public static int[,] Add(int val, int[,] arr) 
        { 
            return Add(arr, val); 
        }

        # endregion Integer Arrays
        # region Double Arrays

        /// <summary>
        /// Adds two arrays
        /// </summary>
        /// <param name="arr1"> Array 1 </param>
        /// <param name="arr2"> Array 2 </param>
        /// <returns>  Array sum </returns>
        public static double[] Add(double[] arr1, double[] arr2)
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
            //        arrOut[idx0] = arr1[idx0] + arr2[idx0];
            //});

            for (int idx0 = 0; idx0 < len1; idx0++)
                arrOut[idx0] = arr1[idx0] + arr2[idx0];
            return arrOut;
        }

        /// <summary>
        /// Adds a scalar value to an array
        /// </summary>
        /// <param name="arr"> Array </param>
        /// <param name="val"> Scalar value </param>
        /// <returns> Array sum </returns>
        public static double[] Add(double[] arr, double val)
        {
            int len1 = arr.Length;
            double[] arrOut = new double[len1];

            // Partitions source array by columns
            //var part = Partitioner.Create(0, len1);
            //Parallel.ForEach(part, (range) =>
            //{
            //    for (int idx0 = range.Item1; idx0 < range.Item2; idx0++)
            //        arrOut[idx0] = arr[idx0] + val;
            //});

            for (int idx0 = 0; idx0 < len1; idx0++)
                arrOut[idx0] = arr[idx0] + val;
            return arrOut;
        }

        /// <summary>
        /// Adds a scalar value to an array
        /// </summary>
        /// <param name="val"> Array </param>
        /// <param name="arr"> Scalar value </param>
        /// <returns> Array sum </returns>
        public static double[] Add(double val, double[] arr) 
        { 
            return Add(arr, val); 
        }

        /// <summary>
        /// Adds two arrays
        /// </summary>
        /// <param name="arr1"> Array 1 </param>
        /// <param name="arr2"> Array 2 </param>
        /// <returns> Array sum </returns>
        public static double[,] Add(double[,] arr1, double[,] arr2)
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
            OrderablePartitioner<Tuple<int, int>> part = Partitioner.Create(0, len1);
            Parallel.ForEach(part, (range) =>
                {
                    for (int idx0 = range.Item1; idx0 < range.Item2; idx0++)
                        for (int idx1 = 0; idx1 < len2; idx1++)
                            arrOut[idx0, idx1] = arr1[idx0, idx1] + 
                            arr2[idx0, idx1];
                });

            //for (int idx0 = 0; idx0 < len1; idx0++)
            //    for (int idx1 = 0; idx1 < len2; idx1++)
            //        arrOut[idx0, idx1] = arr1[idx0, idx1] + 
            //        arr2[idx0, idx1];
            return arrOut;
        }

        /// <summary>
        /// Adds a scalar value to an array
        /// </summary>
        /// <param name="arr"> Array </param>
        /// <param name="val"> Scalar value </param>
        /// <returns> Array sum </returns>
        public static double[,] Add(double[,] arr, double val)
        {
            int len1 = arr.GetLength(0);
            int len2 = arr.GetLength(1);
            double[,] arrOut = new double[len1, len2];

            // Partitions source array by columns
            if (arrOut.Length == 0)
                return arrOut;
            OrderablePartitioner<Tuple<int, int>> part = Partitioner.Create(0, len1);
            Parallel.ForEach(part, (range) =>
            {
                for (int idx0 = range.Item1; idx0 < range.Item2; idx0++)
                    for (int idx1 = 0; idx1 < len2; idx1++)
                        arrOut[idx0, idx1] = arr[idx0, idx1] + val;
            });

            //for (int idx0 = 0; idx0 < len1; idx0++)
            //    for (int idx1 = 0; idx1 < len2; idx1++)
            //        arrOut[idx0, idx1] = arr[idx0, idx1] + val;
            return arrOut;
        }

        /// <summary>
        /// Adds a scalar value to and array
        /// </summary>
        /// <param name="val"> Scalar value </param>
        /// <param name="arr"> Array </param>
        /// <returns> Array sum </returns>
        public static double[,] Add(double val, double[,] arr) 
        { 
            return Add(arr, val); 
        }

        # endregion Double Arrays
        # region Float Arrays

        // I'm adding these float methods because the functional form shows
        // reasonable performance that might actually be faster that direct
        // calls.  Further investigation is necessary.

        /// <summary>
        /// Adds two arrays
        /// </summary>
        /// <param name="arr1"> Array 1 </param>
        /// <param name="arr2"> Array 2 </param>
        /// <returns> Array sum </returns>
        public static float[] Add(float[] arr1, float[] arr2)
        {
            Expression<Func<float, float, float>> exAdd = (x, y) => x + y;
            Func<float, float, float> add = exAdd.Compile();
            return Element.Op(add, arr1, arr2);
        }

        /// <summary>
        /// Adds a scalar value to an array
        /// </summary>
        /// <param name="arr"> Array </param>
        /// <param name="val"> Scalar value </param>
        /// <returns> Array sum </returns>
        public static float[] Add(float[] arr, float val)
        {
            Expression<Func<float, float, float>> exAdd = (x, y) => x + y;
            Func<float, float, float> add = exAdd.Compile();
            return Element.Op(add, arr, val);
        }

        /// <summary>
        /// Adds a scalar value to an array
        /// </summary>
        /// <param name="val"> Array </param>
        /// <param name="arr"> Scalar value </param>
        /// <returns> Array sum </returns>
        public static float[] Add(float val, float[] arr)
        {
            return Add(arr, val);
        }

        /// <summary>
        /// Adds two arrays
        /// </summary>
        /// <param name="arr1"> Array 1 </param>
        /// <param name="arr2"> Array 2 </param>
        /// <returns> Array sum </returns>
        public static float[,] Add(float[,] arr1, float[,] arr2)
        {
            Expression<Func<float, float, float>> exAdd = (x, y) => x + y;
            Func<float, float, float> add = exAdd.Compile();
            return Element.Op(add, arr1, arr2);
        }

        /// <summary>
        /// Adds a scalar value to an array
        /// </summary>
        /// <param name="arr"> Array </param>
        /// <param name="val"> Scalar value </param>
        /// <returns> Array sum </returns>
        public static float[,] Add(float[,] arr, float val)
        {
            Expression<Func<float, float, float>> exAdd = (x, y) => x + y;
            Func<float, float, float> add = exAdd.Compile();
            return Element.Op(add, arr, val);
        }

        /// <summary>
        /// Adds a scalar value to an array
        /// </summary>
        /// <param name="val"> Scalar values </param>
        /// <param name="arr"> Array </param>
        /// <returns> Array sum</returns>
        public static float[,] Add(float val, float[,] arr)
        {
            Expression<Func<float, float, float>> exAdd = (x, y) => x + y;
            Func<float, float, float> add = exAdd.Compile();
            return Element.Op(add, val, arr);
        }

        # endregion Float Arrays
        # region Generic Expressions

        /// <summary>
        /// Adds two variables
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="value1"> Variable 1 </param>
        /// <param name="value2"> Variable 2</param>
        /// <returns> Sum of variables </returns>
        public static T Add<T>(T value1, T value2) 
        { 
            return Operator<T>.Add(value1, value2); 
        }

        /// <summary>
        /// Adds two arrays
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="arr0"> Array 1 </param>
        /// <param name="arr1"> Array 2 </param>
        /// <returns> Array sum </returns>
        public static T[] Add<T>(T[] arr0, T[] arr1) 
        { 
            return Element.Op(Operator<T>.Add, arr0, arr1); 
        }

        /// <summary>
        /// Adds a scalar variable to an array
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="arr"> Array </param>
        /// <param name="val"> Scalar value </param>
        /// <returns> Array sum </returns>
        public static T[] Add<T>(T[] arr, T val) 
        { 
            return Element.Op(Operator<T>.Add, arr, val); 
        }

        /// <summary>
        /// Adds a scalar variable to an array
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="val"> Scalar value </param>
        /// <param name="arr"> Array </param>
        /// <returns> Array sum </returns>
        public static T[] Add<T>(T val, T[] arr)
        {
            return Element.Op(Operator<T>.Add, val, arr);
        }

        /// <summary>
        /// Adds two arrays
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="arr0"> Array 1 </param>
        /// <param name="arr1"> Array 2 </param>
        /// <returns> Summed Array </returns>
        public static T[,] Add<T>(T[,] arr0, T[,] arr1) 
        { 
            return Element.Op(Operator<T>.Add, arr0, arr1); 
        }

        /// <summary>
        /// Adds a scalar variable to an array
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="arr"> Array </param>
        /// <param name="val"> Scalar value </param>
        /// <returns> Array sum </returns>
        public static T[,] Add<T>(T[,] arr, T val) 
        { 
            return Element.Op(Operator<T>.Add, arr, val); 
        }

        /// <summary>
        /// Adds a scalar variable to an array
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="val"> Scalar value </param>
        /// <param name="arr"> Array </param>
        /// <returns> Array sum </returns>
        public static T[,] Add<T>(T val, T[,] arr)
        {
            return Element.Op(Operator<T>.Add, val, arr);
        }

        # endregion Generic Expressions
    }
}
