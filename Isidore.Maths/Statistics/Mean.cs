namespace Isidore.Maths
{
    public static partial class Stats
    {
        # region Integers

        /// <summary>
        /// Finds the mean of an array
        /// </summary>
        /// <param name="arr"> Array </param>
        /// <returns> Array mean </returns>
        public static int Mean(int[] arr)
        {
            CheckMeanInput(arr);
            return Sum(arr) / arr.Length;
        }

        /// <summary>
        /// Finds the mean of an array
        /// </summary>
        /// <param name="arr"> Array </param>
        /// <returns> Array mean </returns>
        public static int Mean(int[,] arr)
        {
            CheckMeanInput(arr);
            return Sum(arr) / arr.Length;
        }

        # endregion Integers
        # region Doubles

        /// <summary>
        /// Finds the mean of an array
        /// </summary>
        /// <param name="arr"> Array </param>
        /// <returns> Array mean </returns>
        public static double Mean(double[] arr)
        {
            CheckMeanInput(arr);
            return Sum(arr) / (double)arr.Length;
        }

        /// <summary>
        /// Finds the mean of an array
        /// </summary>
        /// <param name="arr"> Array </param>
        /// <returns> Array mean </returns>
        public static double Mean(double[,] arr)
        {
            CheckMeanInput(arr);
            return Sum(arr) / (double)arr.Length;
        }

        # endregion Doubles
        # region Generics

        /// <summary>
        /// Finds the mean of an array
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="arr"> Array </param>
        /// <returns> Array mean </returns>
        public static T Mean<T>(T[] arr)
        {
            CheckMeanInput(arr);
            T sum = Sum<T>(arr);
            T denom = Operator.Convert<int,T>(arr.Length);
            T mean = Operator.Divide(sum, denom);
            return mean;
        }

        /// <summary>
        /// Finds the mean of an array
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="arr"> Array </param>
        /// <returns> Array mean </returns>
        public static T Mean<T>(T[,] arr)
        {
            CheckMeanInput(arr);
            T sum = Sum<T>(arr);
            T denom = Operator.Convert<int, T>(arr.Length);
            T mean = Operator.Divide(sum, denom);
            return mean;           
        }

        # endregion Generics

        private static void CheckMeanInput(System.Array arr)
        {
            if (arr == null)
                throw new System.ArgumentNullException("arr");
            if (arr.Length == 0)
                throw new System.ArgumentException("The array must contain at least one value.", "arr");
        }
    }
}
