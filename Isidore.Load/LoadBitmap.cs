using System;
using System.Drawing;
using System.IO;

namespace Isidore.Load
{
    /// <summary>
    /// A toolbox for loading data from files into memory
    /// </summary>
    public partial class Load
    {
        /// <summary>
        /// Loads an bitmap as a 2D color array.  
        /// FileName should include any path information
        /// </summary>
        /// <param name="FileName"> File location name </param>
        /// <returns> 2D color array </returns>
        public static Color[,] Bitmap(string FileName)
        {
            if (!File.Exists(FileName))
            {
                Console.WriteLine("{0} does not exist.", FileName);
                return null;
            }

            // Retrieves bitmap and extract image size
            using (Bitmap rawImg = new Bitmap(FileName))
            {
                // This copy avoids a dependency on ImgProcess.
                int width = rawImg.Width;
                int height = rawImg.Height;
                Color[,] cImg = new Color[width, height];
                for (int k1 = 0; k1 < width; k1++)
                    for (int k2 = 0; k2 < height; k2++)
                        cImg[k1, k2] = rawImg.GetPixel(k1, k2);
                return cImg;
            }
        }
    }
}
