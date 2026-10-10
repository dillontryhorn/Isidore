using System;
using Isidore.Maths;

namespace Isidore.ImgProcess
{
    /// <summary>
    /// Sobel edge image processing
    /// </summary>
    public class Sobel
    {
        /// <summary>
        /// First dimension kernel
        /// </summary>
        public static double[,] Sob0 = new double[,] {{-1,-2,-1},
                                                { 0, 0, 0},
                                                { 1, 2, 1}};

        /// <summary>
        /// Second dimension kernel
        /// </summary>
        public static double[,] Sob1 = new double[,] {{-1, 0, 1},
                                                {-2, 0, 2},
                                                {-1, 0, 1}};

        /// <summary>
        /// Applies Sobel edge detection to an array 
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="arr"> Array </param>
        /// <returns> 1st: edge detected image, 2nd: Dimension 0 gradient 
        /// image, 3rd: Dimension 1 gradient image. </returns>
        public static Tuple<double[,], double[,], double[,]> 
            Process<T>(T[,] arr)
        {
            Func<double, double> sqrt = x => Math.Sqrt(x);

            double[,] dArr = Operator.Convert<T, double>(arr);
            Tuple<double[,], double[,], double[,]> gpuResult;
            if (GpuSobel.TryProcess(dArr, Sob0, Sob1, out gpuResult))
                return gpuResult;
            double[,] G0 = Arr.Convolve(dArr, Sob0);
            double[,] G1 = Arr.Convolve(dArr, Sob1);
            double[,] G2 = Operator.Add(Operator.Multiply(G0, G0), 
                Operator.Multiply(G1, G1));
            double[,] G = Element.Op(sqrt, G2);
            return new Tuple<double[,], double[,], double[,]>(G, G0, G1);
        }

        /// <summary>
        /// Applies Sobel edge detection to an array and returns 
        /// the edge image
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="arr"> Array </param>
        /// <returns> The edge image </returns>
        public static double[,] Image<T>(T[,] arr)
        {
            Tuple<double[,], double[,], double[,]> output = Process(arr);
            return output.Item1;
        }
    }
}
