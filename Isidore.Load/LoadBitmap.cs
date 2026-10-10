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

            // Retrieves the bitmap and extracts its colors in bulk where possible.
            using (Bitmap rawImg = new Bitmap(FileName))
            {
                return BitmapPixels.ReadColors(rawImg);
            }
        }
    }
}
