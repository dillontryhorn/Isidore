using System;
using Isidore.Maths;

namespace Isidore.ImgProcess
{
    /// <summary>
    /// Canny image processing
    /// </summary>
    public class Canny
    {
        /// <summary>
        /// Applies Canny edge processing to an array
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="arr"> Array </param>
        /// <param name="HiRelThresh"> Edge thinning high relative 
        /// threshold </param>
        /// <param name="LowRelThresh">Edge thinning low relative 
        /// threshold </param>
        /// <returns> 1st: Canny edge array, 2nd: 
        /// Dimension 0 gradient image, 3rd: Dimension 1 gradient image
        /// 4th: Edge magnitude, 5th: Edge angle, 
        /// 6th: Edge direction </returns>
        public static Tuple<bool[,], double[,], double[,], 
            double[,], double[,], int[,]> Process<T>
            (T[,] arr, double HiRelThresh, double LowRelThresh)
        {
            int len0 = arr.GetLength(0);
            int len1 = arr.GetLength(1);

            //Func<T, double> t2d = Operator.Convert<T, double>;

            // Step 1: Image smoothing -> Skipped

            // Step 2: Intensity Gradients & Edge Strength
            Tuple<double[,], double[,], double[,]> SobelTuple = Sobel.Process(arr); // Gets tuple data
            double[,] Mag = SobelTuple.Item1;
            double[,] G0 = SobelTuple.Item2;
            double[,] G1 = SobelTuple.Item3;

            double[] minMax = Stats.MinMax(Mag);
            // An image without gradients has a zero maximum. Dividing by it
            // would turn every magnitude into NaN.
            if (minMax[1] > 0)
                Mag = Operator.Divide(Mag, minMax[1]);

            // Step 3, 4, & 5: Gradient edge angle, 
            // Directional bins, & Edge Thinning
            double[,] Angle = new double[len0, len1];
            int[,] Dir = new int[len0, len1];
            bool[,] EdgeHi = new bool[len0, len1];
            bool[,] EdgeLow = new bool[len0, len1];
            for (int idx0 = 1; idx0 < len0-1; idx0++)
                for (int idx1 = 1; idx1 < len1-1; idx1++)
                {
                    if (Mag[idx0, idx1] >= LowRelThresh)
                    {
                        Angle[idx0, idx1] = Math.Atan2(G1[idx0, idx1], 
                            G0[idx0, idx1]) * 180.0 / Math.PI;
                        // Angle Bin 1
                        if (Angle[idx0, idx1] >= -22.5 && 
                            Angle[idx0, idx1] < 22.5 || 
                            Angle[idx0, idx1] < -157.5 || 
                            Angle[idx0, idx1] >= 157.5)
                        {
                            Dir[idx0, idx1] = 1;
                            if (Mag[idx0, idx1] > Mag[idx0 + 1, idx1] && 
                                Mag[idx0, idx1] >= Mag[idx0 - 1, idx1])
                            {
                                if (Mag[idx0, idx1] >= HiRelThresh)
                                    EdgeHi[idx0, idx1] = true;
                                else
                                    EdgeLow[idx0, idx1] = true;
                            }
                        }

                        // Angle Bin 2
                        if (Angle[idx0, idx1] >= -112.5 && 
                            Angle[idx0, idx1] < -67.5 || 
                            Angle[idx0, idx1] >= 67.5 && 
                            Angle[idx0, idx1] < 112.5)
                        {
                            Dir[idx0, idx1] = 2;
                            if (Mag[idx0, idx1] > Mag[idx0, idx1+1] && 
                                Mag[idx0, idx1] >= Mag[idx0, idx1-1])
                            {
                                if (Mag[idx0, idx1] >= HiRelThresh)
                                    EdgeHi[idx0, idx1] = true;
                                else
                                    EdgeLow[idx0, idx1] = true;
                            }
                        }

                        // Angle Bin 3
                        if (Angle[idx0, idx1] >= -67.5 && 
                            Angle[idx0, idx1] < -22.5 ||
                           Angle[idx0, idx1] >= 112.5 && 
                           Angle[idx0, idx1] < 157.5)
                        {
                            Dir[idx0, idx1] = 3;
                            if (Mag[idx0, idx1] > Mag[idx0 + 1, idx1 - 1] && 
                                Mag[idx0, idx1] >= Mag[idx0 - 1, idx1 + 1])
                            {
                                if (Mag[idx0, idx1] >= HiRelThresh)
                                    EdgeHi[idx0, idx1] = true;
                                else
                                    EdgeLow[idx0, idx1] = true;
                            }
                        }

                        // Angle Bin 4
                        if (Angle[idx0, idx1] >= -157.5 && 
                            Angle[idx0, idx1] < -112.5 ||
                           Angle[idx0, idx1] >= 22.5 && 
                           Angle[idx0, idx1] < 67.5)
                        {
                            Dir[idx0, idx1] = 4;
                            if (Mag[idx0, idx1] > Mag[idx0 + 1, idx1 + 1] && 
                                Mag[idx0, idx1] >= Mag[idx0 - 1, idx1 - 1])
                            {
                                if (Mag[idx0, idx1] >= HiRelThresh)
                                    EdgeHi[idx0, idx1] = true;
                                else
                                    EdgeLow[idx0, idx1] = true;
                            }
                        }
                    }
                    else
                        Mag[idx0, idx1] = 0;
                }

            // Step 6: Hysteresis
            bool converge = true;
            while (converge)
            {
                converge = false;
                for (int idx0 = 1; idx0 < len0 - 1; idx0++)
                    for (int idx1 = 1; idx1 < len1 - 1; idx1++)
                    {
                        if (EdgeHi[idx0, idx1])
                        {
                            for (int sidx0 = -1; sidx0 <= 1; sidx0++)
                                for (int sidx1 = -1; sidx1 <= 1; sidx1++)
                                    if (EdgeLow[idx0 + sidx0, idx1 + sidx1])
                                    {
                                        EdgeHi[idx0 + sidx0, idx1 + sidx1] 
                                            = true;
                                        EdgeLow[idx0 + sidx0, idx1 + sidx1] 
                                            = false;
                                        // Tags that some low has switch over
                                        converge = true;
                                    }
                        }
                    }
            }

            return Tuple.Create(EdgeHi, G0, G1, Mag, Angle, Dir);
        }

        /// <summary>
        /// Applies Canny edge processing to an array
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="arr"> Array </param>
        /// <param name="HiRelThresh"> Edge thinning high relative 
        /// threshold </param>
        /// <param name="LowRelThresh">Edge thinning low relative 
        /// threshold </param>
        /// <returns>1st: Canny edge array, 2nd: Edge magnitude, 
        /// 3rd: Edge direction </returns>
        public static Tuple<bool[,], double[,], int[,]> ShortProcess<T>
            (T[,] arr, double HiRelThresh, double LowRelThresh)
        {
            Tuple<bool[,], double[,], double[,], double[,], double[,], int[,]> output = Process(arr, HiRelThresh, LowRelThresh);
            
            return Tuple.Create(output.Item1, output.Item4, output.Item6);
        }

        /// <summary>
        /// Applies Canny edge processing to an array
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="arr"> Array </param>
        /// <param name="HiRelThresh"> Edge thinning high relative 
        /// threshold </param>
        /// <param name="LowRelThresh">Edge thinning low relative 
        /// threshold </param>
        /// <returns> Canny edge array </returns>
        public static bool[,] Image<T>(T[,] arr, double HiRelThresh, 
            double LowRelThresh)
        {
            Tuple<bool[,], double[,], int[,]> output = ShortProcess(arr, HiRelThresh, LowRelThresh);

            return output.Item1;
        }

    }
}
