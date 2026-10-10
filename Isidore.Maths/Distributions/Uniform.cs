using System;

namespace Isidore.Maths
{
    public static partial class Distribution
    {
        /// <summary>
        /// Populates a one-dimensional array with a single value
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="length"> Array length </param>
        /// <param name="value"> Elements' value </param>
        /// <returns> 1D array of length, set to value  </returns>
        public static T[] Uniform<T>(int length, T value)
        {
            T[] arr = new T[length];
            for (int idx0 = 0; idx0 < length; idx0++)
                arr[idx0] = value;
            return arr;
        }

        /// <summary>
        /// Populates a two-dimensional array with a single value
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="length0"> Array length in the first axis </param>
        /// <param name="length1"> Array length in the second axis </param>
        /// <param name="value"> Elements' value </param>
        /// <returns> 2D array of length, set to value  </returns>
        public static T[,] Uniform<T>(int length0, int length1, T value)
        {
            T[,] arr = new T[length0, length1];
            for (int idx0 = 0; idx0 < length0; idx0++)
                for (int idx1 = 0; idx1 < length1; idx1++)
                    arr[idx0, idx1] = value;
            return arr;
        }

        /// <summary>
        /// Populates an array of size lengths with a single value 
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="lengths"> Lengths of each dimension </param>
        /// <param name="value"> value to populate the array with </param>
        /// <returns> An array with a rank equal to the length of lengths, 
        /// set to value </returns>
        public static Array Uniform<T>(int[] lengths, T value)
        {
            Array arr = Array.CreateInstance(typeof(T), lengths);
            int[] indices = new int[lengths.Length];
            for (long idx = 0; idx < arr.LongLength; idx++)
            {
                arr.SetValue(value, indices);
                // Rectangular arrays require one index for every dimension.
                for (int dimension = indices.Length - 1; dimension >= 0; dimension--)
                {
                    if (++indices[dimension] < lengths[dimension])
                        break;
                    indices[dimension] = 0;
                }
            }
            return arr;
        }
    }
}
