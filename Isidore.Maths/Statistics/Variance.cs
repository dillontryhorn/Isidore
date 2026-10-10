using System;

namespace Isidore.Maths
{
    /// <summary>
    /// Statistics function
    /// </summary>
    public static partial class Stats
    {
        
        /// <summary>
        /// Population variance of a summation of data points
        /// </summary>
        /// <param name="sum"> summation of all data points </param>
        /// <param name="sumSq"> summation of the squares of all data 
        /// points </param>
        /// <param name="sampleSize"> total number of data points </param>
        /// <returns> Variance and mean of the data set </returns>
        public static double[] Variance(double sum, double sumSq, 
            int sampleSize)
        {
            if (sampleSize < 1)
                throw new ArgumentOutOfRangeException("sampleSize",
                    "At least one data point is required.");
            double N = (double)sampleSize;
            // Mean and quadratic term
            double mean = sum / N;
            double var = sumSq - 2.0 * mean * sum + N * mean * mean;
            var /= N;
            return new double[] { var, mean };
        }

        /// <summary>
        /// Variance of a summation of data points
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="sum"> summation of all data points </param>
        /// <param name="sumSq"> summation of the squares of all data 
        /// points </param>
        /// <param name="sampleSize"> total number of data points </param>
        /// <returns> Variance and mean of the data set </returns>
        public static double[] Variance<T>(T sum, T sumSq, int sampleSize)
        {
            Func<T, double> convert = Operator<T, double>.Convert;
            // Converts data to double
            double dsum = convert(sum);
            double dsumSq = convert(sumSq);
            return Variance(dsum, dsumSq, sampleSize);
        }

        /// <summary>
        /// Variance of an array
        /// </summary>
        /// <param name="arr"> Array </param>
        /// <returns> Variance and mean of the data set </returns>
        public static double[] Variance(double[] arr)
        {
            StableVariance stats = new StableVariance();
            // Steps through arrays
            int len = arr.Length;
            for (int idx = 0; idx < len; idx++)
            {
                stats.Add(arr[idx]);
            }

            return stats.Result();
        }

        /// <summary>
        /// Variance of an array
        /// </summary>
        /// <typeparam name="T"> Input data type </typeparam>
        /// <param name="arr"> Array </param>
        /// <returns> Variance and mean of the data set </returns>
        public static double[] Variance<T>(T[] arr)
        {
            // Converts array to double type
            double[] darr = Operator.Convert<T, double>(arr);
            // Passes to Variance
            return Variance(darr);
        }

        /// <summary>
        /// Variance of an array
        /// </summary>
        /// <param name="arr"> Array </param>
        /// <returns> Variance and mean of the data set </returns>
        public static double[] Variance(double[,] arr)
        {
            StableVariance stats = new StableVariance();

            int dim0 = arr.GetLength(0);
            int dim1 = arr.GetLength(1);
            for (int idx0 = 0; idx0 < dim0; idx0++)
                for (int idx1 = 0; idx1 < dim1; idx1++)
                {
                    stats.Add(arr[idx0, idx1]);
                }

            return stats.Result();
        }

        /// <summary>
        /// Variance of an array
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="arr"> Array </param>
        /// <returns> Variance and mean of the data set </returns>
        public static double[] Variance<T>(T[,] arr)
        {
            // Converts array to double
            double[,] darr = Operator.Convert<T, double>(arr);
            // Passes to variance
            return Variance(darr);
        }

        /// <summary>
        /// Finds the variance of a data array marked with a tag array
        /// </summary>
        /// <param name="arr"> Data array </param>
        /// <param name="tag"> Tag array </param>
        /// <returns> Variance of data marked with tag </returns>
        public static double[] Variance(double[,] arr, bool[,] tag)
        {
            // Checks that arr and tag are the same size
            int dim0 = arr.GetLength(0);
            int dim1 = arr.GetLength(1);
            if (dim0 != tag.GetLength(0) || dim1 != tag.GetLength(1))
                throw new System.ArgumentException(
                    "Data and tag arrays must be the same size", "arr");

            // Data needed for variance
            StableVariance stats = new StableVariance();
            // Loops through array
            for (int idx0 = 0; idx0 < dim0; idx0++)
                for (int idx1 = 0; idx1 < dim1; idx1++)
                    if (tag[idx0, idx1]) // If tags, add to sample
                    {
                        stats.Add(arr[idx0, idx1]);
                    }
            // Calls Variance
            return stats.Result();
        }

        /// <summary>
        /// Finds the variance of a data array marked with a tag array
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="arr"> Data array </param>
        /// <param name="tag"> Tag array </param>
        /// <returns> Variance of data marked with tag </returns>
        public static double[] Variance<T>(T[,] arr, bool[,] tag)
        {
            // Converts to double and passes to Variance
            double[,] darr = Operator.Convert<T, double>(arr);
            return Variance(darr, tag);
        }

        /// <summary>
        /// Finds the variance of a data array marked with a tag array
        /// </summary>
        /// <param name="arr"> Data array </param>
        /// <param name="tag"> Tag array </param>
        /// <returns> Variance of data marked with tag </returns>
        public static double[] Variance(double[] arr, bool[] tag)
        {
            // Checks that arr and tag are the same size
            int dim0 = arr.Length;
            if (dim0 != tag.Length)
                throw new System.ArgumentException(
                    "Data and tag arrays must be the same size", "arr");

            // Data needed for variance
            StableVariance stats = new StableVariance();
            // Loops through array
            for (int idx0 = 0; idx0 < dim0; idx0++)
                    if (tag[idx0]) // If tags, add to sample
                    {
                        stats.Add(arr[idx0]);
                    }
            // Calls Variance
            return stats.Result();
        }

        /// <summary>
        /// Finds the variance of a data array marked with a tag array
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="arr"> Data array </param>
        /// <param name="tag"> Tag array </param>
        /// <returns> Variance of data marked with tag </returns>
        public static double[] Variance<T>(T[] arr, bool[] tag)
        {
            // Converts to double and passes to Variance
            double[] darr = Operator.Convert<T, double>(arr);
            return Variance(darr, tag);
        }

        /// <summary>
        /// Compatibility overload for a vector with a rectangular tag array.
        /// Tags are read in row-major order; use the bool[] overload for new code.
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="arr"> Data array </param>
        /// <param name="tag"> Tag array with the same total length </param>
        /// <returns> Population variance and mean of tagged data </returns>
        public static double[] Variance<T>(T[] arr, bool[,] tag)
        {
            return Variance(arr, VectorTags(tag, arr.Length));
        }

        private static bool[] VectorTags(bool[,] tag, int length)
        {
            if (tag == null)
                throw new ArgumentNullException("tag");
            if (tag.Length != length)
                throw new ArgumentException("Data and tag arrays must have the same length.", "tag");
            bool[] tags = new bool[length];
            int index = 0;
            foreach (bool value in tag)
                tags[index++] = value;
            return tags;
        }

        // Welford accumulation on coordinates shifted by the first sample
        // avoids subtracting two large, nearly equal sums of squares.
        private struct StableVariance
        {
            private int count;
            private double origin;
            private double mean;
            private double sumSquaredDeviations;
            private bool overflowedRange;

            public void Add(double value)
            {
                if (count == 0)
                    origin = value;
                count++;
                if (overflowedRange)
                {
                    mean = mean * (1.0 - 1.0 / count) + value / count;
                    return;
                }
                double shifted = value - origin;
                if (double.IsInfinity(shifted) && !double.IsInfinity(value) && !double.IsInfinity(origin))
                {
                    // Finite samples this far apart have an unrepresentably
                    // large variance, but their mean can still be finite.
                    mean = (origin + mean) * (1.0 - 1.0 / count) + value / count;
                    origin = 0;
                    sumSquaredDeviations = double.PositiveInfinity;
                    overflowedRange = true;
                    return;
                }
                double delta = shifted - mean;
                mean += delta / count;
                sumSquaredDeviations += delta * (shifted - mean);
            }

            public double[] Result()
            {
                if (count == 0)
                    throw new ArgumentOutOfRangeException("sampleSize", "At least one data point is required.");
                return new double[] { sumSquaredDeviations / count, origin + mean };
            }
        }
    }
}
