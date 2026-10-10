using System;

namespace Isidore.Maths
{
    /// <summary>
    /// Statistics function
    /// </summary>
    public static partial class Stats
    {

        /// <summary>
        /// Standard Deviation of a summation of data points
        /// </summary>
        /// <param name="sum"> summation of all data points </param>
        /// <param name="sumSq"> summation of the squares of all 
        /// data points </param>
        /// <param name="sampleSize"> total number of data points </param>
        /// <returns> Standard Deviation and mean of the data set </returns>
        public static double[] STD(double sum, double sumSq, int sampleSize)
        {
            double[] stats = Variance(sum, sumSq, sampleSize);
            return new double[] { Math.Sqrt(stats[0]), stats[1] };
        }

        /// <summary>
        /// Standard Deviation of a summation of data points
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="sum"> summation of all data points </param>
        /// <param name="sumSq"> summation of the squares of all 
        /// data points </param>
        /// <param name="sampleSize"> total number of data points </param>
        /// <returns> Standard Deviation and mean of the data set </returns>
        public static double[] STD<T>(T sum, T sumSq, int sampleSize)
        {
            Func<T, double> convert = Operator<T, double>.Convert;
            // Converts data to double
            double dsum = convert(sum);
            double dsumSq = convert(sumSq);
            return STD(dsum, dsumSq, sampleSize);
        }

        /// <summary>
        /// Standard Deviation of an array
        /// </summary>
        /// <param name="arr"> Array </param>
        /// <returns> Standard Deviation and mean of the data set </returns>
        public static double[] STD(double[] arr)
        {
            return StandardDeviation(Variance(arr));
        }

        /// <summary>
        /// Standard Deviation of an array
        /// </summary>
        /// <typeparam name="T"> Input data type </typeparam>
        /// <param name="arr"> Array </param>
        /// <returns> Standard Deviation and mean of the data set </returns>
        public static double[] STD<T>(T[] arr)
        {
            // Converts array to double type
            double[] darr = Operator.Convert<T, double>(arr);
            // Passes to Standard Deviation
            return STD(darr);
        }

        /// <summary>
        /// Standard Deviation of an array
        /// </summary>
        /// <param name="arr"> Array </param>
        /// <returns> Standard Deviation and mean of the data set </returns>
        public static double[] STD(double[,] arr)
        {
            return StandardDeviation(Variance(arr));
        }

        /// <summary>
        /// Standard Deviation of an array
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="arr"> Array </param>
        /// <returns> Standard Deviation and mean of the data set </returns>
        public static double[] STD<T>(T[,] arr)
        {
            // Converts array to double
            double[,] darr = Operator.Convert<T, double>(arr);
            // Passes to variance
            return STD(darr);
        }

        /// <summary>
        /// Finds the variance of a data array marked with a tag array
        /// </summary>
        /// <param name="arr"> Data array </param>
        /// <param name="tag"> Tag array </param>
        /// <returns> Standard Deviation of data marked with tag </returns>
        public static double[] STD(double[,] arr, bool[,] tag)
        {
            return StandardDeviation(Variance(arr, tag));
        }

        /// <summary>
        /// Finds the variance of a data array marked with a tag array
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="arr"> Data array </param>
        /// <param name="tag"> Tag array </param>
        /// <returns> Standard Deviation of data marked with tag </returns>
        public static double[] STD<T>(T[,] arr, bool[,] tag)
        {
            // Converts to double and passes to Standard Deviation
            double[,] darr = Operator.Convert<T, double>(arr);
            return STD(darr, tag);
        }

        /// <summary>
        /// Finds the variance of a data array marked with a tag array
        /// </summary>
        /// <param name="arr"> Data array </param>
        /// <param name="tag"> Tag array </param>
        /// <returns> Standard Deviation of data marked with tag </returns>
        public static double[] STD(double[] arr, bool[] tag)
        {
            return StandardDeviation(Variance(arr, tag));
        }

        /// <summary>
        /// Finds the variance of a data array marked with a tag array
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="arr"> Data array </param>
        /// <param name="tag"> Tag array </param>
        /// <returns> Standard Deviation of data marked with tag </returns>
        public static double[] STD<T>(T[] arr, bool[] tag)
        {
            // Converts to double and passes to Standard Deviation
            double[] darr = Operator.Convert<T, double>(arr);
            return STD(darr, tag);
        }

        /// <summary>
        /// Compatibility overload for a vector with a rectangular tag array.
        /// Tags are read in row-major order; use the bool[] overload for new code.
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="arr"> Data array </param>
        /// <param name="tag"> Tag array with the same total length </param>
        /// <returns> Population standard deviation and mean of tagged data </returns>
        public static double[] STD<T>(T[] arr, bool[,] tag)
        {
            return STD(arr, VectorTags(tag, arr.Length));
        }

        private static double[] StandardDeviation(double[] variance)
        {
            return new double[] { Math.Sqrt(variance[0]), variance[1] };
        }
    }
}
