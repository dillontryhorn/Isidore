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
        /// Negates an array (Multiples by -1)
        /// </summary>
        /// <param name="arr"> Array </param>
        /// <returns> Negated array </returns>
        public static int[] Negate(int[] arr)
        {
            int[] arrOut = (int[])arr.Clone();

            // Partitions source array by columns
            //var part = Partitioner.Create(0, arrOut.Length); 
            //Parallel.ForEach(part, (range) =>
            //{
            //    for (int idx0 = range.Item1; idx0 < range.Item2; idx0++)
            //        arrOut[idx0] *= -1;
            //});

            for (int idx0 = 0; idx0 < arrOut.Length; idx0++)
                arrOut[idx0] *= -1;
            return arrOut;
        }

        /// <summary>
        /// Negates an array (Multiples by -1)
        /// </summary>
        /// <param name="arr"> Array </param>
        /// <returns> Negated array </returns>
        public static int[,] Negate(int[,] arr)
        {
            int[,] arrOut = (int[,])arr.Clone();
            int len1 = arr.GetLength(0);
            int len2 = arr.GetLength(1);

            // Partitions source array by columns
            if (arrOut.Length == 0)
                return arrOut;
            OrderablePartitioner<System.Tuple<int, int>> part = Partitioner.Create(0, len1);
            Parallel.ForEach(part, (range) =>
            {
                for (int idx0 = range.Item1; idx0 < range.Item2; idx0++)
                    for (int idx1 = 0; idx1 < len2; idx1++)
                        arrOut[idx0, idx1] *= -1;
            });

            //for (int idx0 = 0; idx0 < len1; idx0++)
            //    for (int idx1 = 0; idx0 < len2; idx0++)
            //        arrOut[idx0,idx1] *= -1;
            return arrOut;
        }

        # endregion Integer Arrays
        # region Double Arrays

        /// <summary>
        /// Negates an array (Multiples by -1)
        /// </summary>
        /// <param name="arr"> Array </param>
        /// <returns> Negated array </returns>
        public static double[] Negate(double[] arr)
        {
            double[] arrOut = (double[])arr.Clone();

            // Partitions source array by columns
            //var part = Partitioner.Create(0, arrOut.Length);
            //Parallel.ForEach(part, (range) =>
            //{
            //    for (int idx0 = range.Item1; idx0 < range.Item2; idx0++)
            //            arrOut[idx0] *= -1;
            //});

            for (int idx0 = 0; idx0 < arrOut.Length; idx0++)
                arrOut[idx0] *= -1.0;
            return arrOut;
        }

        /// <summary>
        /// Negates an array (Multiples by -1)
        /// </summary>
        /// <param name="arr"> Array </param>
        /// <returns> Negated array </returns>
        public static double[,] Negate(double[,] arr)
        {
            double[,] arrOut = (double[,])arr.Clone();
            int len1 = arr.GetLength(0);
            int len2 = arr.GetLength(1);

            // Partitions source array by columns
            if (arrOut.Length == 0)
                return arrOut;
            OrderablePartitioner<System.Tuple<int, int>> part = Partitioner.Create(0, len1);
            Parallel.ForEach(part, (range) =>
            {
                for (int idx0 = range.Item1; idx0 < range.Item2; idx0++)
                    for (int idx1 = 0; idx1 < len2; idx1++)
                        arrOut[idx0, idx1] *= -1.0;
            });

            //for (int idx0 = 0; idx0 < len1; idx0++)
            //    for (int idx1 = 0; idx0 < len2; idx0++)
            //        arrOut[idx0, idx1] *= -1.0;
            return arrOut;
        }

        # endregion Double Arrays

        # region Generic Expressions

        /// <summary>
        /// Negates a value (Multiples by -1)
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="val"> Value </param>
        /// <returns> Negated variable </returns>
        public static T Negate<T>(T val) 
        { 
            return Operator<T>.Negate(val); 
        }

        /// <summary>
        /// Negates an array (Multiples by -1)
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="arr"> Array </param>
        /// <returns> Negated array </returns>
        public static T[] Negate<T>(T[] arr) 
        { 
            return Element.Op(Operator<T>.Negate, arr);
        }

        /// <summary>
        /// Negates an array (Multiples by -1)
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="arr"> Array </param>
        /// <returns> Negated array </returns>
        public static T[,] Negate<T>(T[,] arr) 
        { 
            return Element.Op(Operator.Negate, arr); 
        }

        # endregion Generic Expressions
    }
}
