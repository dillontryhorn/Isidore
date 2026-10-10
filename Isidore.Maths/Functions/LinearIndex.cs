namespace Isidore.Maths
{
    public partial class Function
    {
        /// <summary>
        /// Given an N-dimensional index, returns the corresponding 
        /// location in an array
        /// </summary>
        /// <param name="index"> N-dimensional index</param>
        /// <param name="resolution">  Array resolution </param>
        /// <returns> Position in the array </returns>
        public static int LinearIndex(int[] index, int[] resolution)
        {
            // Checks that index is a long as the rank
            if (index.Length != resolution.Length)
                throw new System.ArgumentException(
                    "The index must be as long as the rank.", 
                    "LinearIndex");

            // The first dimension varies fastest, matching SubscriptIndex.
            int location = 0;
            int stride = 1;
            for (int idx = 0; idx < resolution.Length; idx++)
            {
                location += stride * index[idx];
                stride *= resolution[idx];
            }

            return location;
        }

        /// <summary>
        /// Given a linear index and a resolution, returns the
        /// subscript indices
        /// </summary>
        /// <param name="index"> Linear index </param>
        /// <param name="resolution"> Resolution of each dimension </param>
        /// <returns> subscript indices </returns>
        public static int[] SubscriptIndex(int index, int[] resolution)
        {
            int rank = resolution.Length;
            int[] subIdx = new int[rank];
            int runSeg = 1; // Provides a running segment size
            for (int dIdx = 0; dIdx < rank; dIdx++)
            {
                // Defines index
                subIdx[dIdx] = (index / runSeg) % resolution[dIdx];
                runSeg *= resolution[dIdx];
            }

            return subIdx;
        }
    }
}
