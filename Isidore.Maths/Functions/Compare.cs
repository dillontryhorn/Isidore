using System;

namespace Isidore.Maths
{
    /// <summary>
    /// Function for comparing various properties of two instances
    /// </summary>
    public class Compare
    {
        /// <summary>
        /// Compares the size of two array (Of the same or different types)
        /// </summary>
        /// <typeparam name="T1"> Data type of array 1 </typeparam>
        /// <typeparam name="T2"> Data type of array 2 </typeparam>
        /// <param name="arr1"> Array 1 </param>
        /// <param name="arr2"> Array 2</param>
        /// <returns> 0: Identical size, 1: Non-equal ranks, 
        /// 2: Non-equal lengths </returns>
        public static int Size<T1, T2>(Array arr1, Array arr2)
        {
            // Checks to see if they have the same number of dimensions
            if (arr1.Rank != arr2.Rank)
                return 1;
            // If so, checks lengths of each dimension
            for (int idx = 0; idx < arr1.Rank; idx++)
                if (arr1.GetLength(idx) != arr2.GetLength(idx))
                    return 2;
            // At this point, the arrays are the same size
            return 0;
        }
    }
}
