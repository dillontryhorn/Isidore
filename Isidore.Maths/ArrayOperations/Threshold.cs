using System;

namespace Isidore.Maths
{
    public static partial class Arr
    {
        /// <summary>
        /// Returns an array where values less than the threshold 
        /// are set to zero
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="arr"> Input array</param>
        /// <param name="thresh"> Threshold values </param>
        /// <returns> Thresholded array </returns>
        public static T[,] Threshold<T>(T[,] arr, T thresh = default(T))
        {
            Func<T, T, bool> lte = Operator<T>.LessThanOrEqual;

            int len0 = arr.GetLength(0);
            int len1 = arr.GetLength(1);
            T[,] threshArr = new T[len0, len1];
            for (int i0 = 0; i0 < len0; i0++)
                for (int i1 = 0; i1 < len1; i1++)
                    if (lte(arr[i0, i1], thresh))
                        threshArr[i0, i1] = default(T); //Sets to zero
                    else
                        threshArr[i0, i1] = arr[i0, i1];
            return threshArr;
        }

        /// <summary>
        /// Returns an array where values less than the threshold 
        /// are set to zero
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="arr"> Input array</param>
        /// <param name="thresh"> Threshold values </param>
        /// <returns> Thresholded array </returns>
        public static T[] Threshold<T>(T[] arr, T thresh = default(T))
        {
            Func<T, T, bool> lte = Operator<T>.LessThanOrEqual;

            int len0 = arr.GetLength(0);
            T[] threshArr = new T[len0];
            for (int i0 = 0; i0 < len0; i0++)
                    if (lte(arr[i0], thresh))
                        threshArr[i0] = default(T); //Sets to zero
                    else
                        threshArr[i0] = arr[i0];
            return threshArr;
        }
        
        /// <summary>
        /// Returns an array where values less than the threshold are 
        /// set to zero and the number of pixels above threshold
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="arr"> Input array </param>
        /// <param name="pixelCount"> Number of pixels above 
        /// threshold </param>
        /// <param name="thresh"> Threshold values </param>
        /// <returns> Thresholded array </returns>
        public static T[,] Threshold<T>(T[,] arr, out int pixelCount, 
            T thresh = default(T))
        {
            Func<T, T, bool> lte = Operator<T>.LessThanOrEqual;

            pixelCount = 0;
            int len0 = arr.GetLength(0);
            int len1 = arr.GetLength(1);
            T[,] threshArr = new T[len0, len1];
            for (int i0 = 0; i0 < len0; i0++)
                for (int i1 = 0; i1 < len1; i1++)
                    if (lte(arr[i0, i1], thresh))
                        threshArr[i0, i1] = default(T); //Sets to zero
                    else
                    {
                        threshArr[i0, i1] = arr[i0, i1];
                        pixelCount++;
                    }
            return threshArr;
        }

        /// <summary>
        /// Returns an array where values less than the threshold are 
        /// set to zero and the number of pixels above threshold
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="arr"> Input array </param>
        /// <param name="pixelCount"> Number of pixels above 
        /// threshold </param>
        /// <param name="thresh"> Threshold values </param>
        /// <returns> Thresholded array </returns>
        public static T[] Threshold<T>(T[] arr, out int pixelCount, 
            T thresh = default(T))
        {
            Func<T, T, bool> lte = Operator<T>.LessThanOrEqual;

            pixelCount = 0;
            int len0 = arr.GetLength(0);
            T[] threshArr = new T[len0];
            for (int i0 = 0; i0 < len0; i0++)
                if (lte(arr[i0], thresh))
                    threshArr[i0] = default(T); //Sets to zero
                else
                {
                    threshArr[i0] = arr[i0];
                    pixelCount++;
                }
            return threshArr;
        }
    }
}
