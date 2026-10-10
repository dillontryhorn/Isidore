using System;
using Isidore.Maths;
using System.Collections.Generic;

namespace Isidore.ImgProcess
{
    /// <summary>
    /// Freeman chain contour code used for monotone compression. This is
    /// a conventional implementation except for the addition of a threshold,
    /// coordinate recording, and a pixel inside/outside feature.  Freeman 
    /// chains used the following directional convention
    /// 0	right
    /// 1	right and up
    /// 2	up
    /// 3	left and up
    /// 4	left
    /// 5	left and down
    /// 6	down
    /// 7	right and down	
    /// </summary>
    /// <typeparam name="T"> Array data type </typeparam>
	public class Contour<T>
    {
        # region Fields & Properties

        /// <summary>
		/// Contour value
		/// </summary>
		public T contVal;
		/// <summary>
		/// Initial stating position within the contour
		/// </summary>
		public int xStart, yStart;
		/// <summary>
		/// Freeman chain direction
		/// </summary>
		public int[] Chain;
		/// <summary>
		/// pixel location of each chain segment
		/// </summary>
		public int[] x, y;
        /// <summary>
        /// Maximum and minimum bounds of the contour
        /// </summary>
        public int xMin, xMax, yMin, yMax;

        /// <summary>
        /// Image size
        /// </summary>
		private int xLen, yLen;

        /// <summary>
        /// Identifies the starting pixel as less than (0) 
        /// or greater or equal to the threshold
        /// </summary>
        private bool posCont; 

        # endregion Fields & Properties

        # region Constructors

        /// <summary>
		/// Constructs a Freeman chain contour
		/// </summary>
		/// <param name="img"> Input image </param>
		/// <param name="cVal"> Contour threshold </param>
		/// <param name="X"> Initial X-axis position </param>
		/// <param name="Y"> Initial Y-axis position </param>
		public Contour(T[,] img, T cVal, int X, int Y)
		{
         
			contVal = cVal;
			xLen = img.GetLength(0);
			yLen = img.GetLength(1);
			xStart = X;
			yStart = Y;
			x = new int[] { X };
			y = new int[] { Y };
			Chain = new int[1];
            xMin = X;
            xMax = X;
            yMin = Y; 
            yMax = Y;
            posCont = Operator<T>.GreaterThanOrEqual(img[X, Y], contVal);
			Build(img);
		}

        # endregion Constructors
        # region Methods

        /// <summary>
		/// Returns the bounding X and Y values of the contour
		/// </summary>
		/// <param name="xMin"> Lower X-axis contour bound </param>
		/// <param name="xMax"> Upper X-axis contour bound </param>
		/// <param name="yMin"> Lower Y-axis contour bound </param>
		/// <param name="yMax"> Upper Y-axis contour bound </param>
		public void GetMinMax(out int xMin, out int xMax,
			 out int yMin, out int yMax)
		{
            xMin = this.xMin;
            xMax = this.xMax;
            yMin = this.yMin;
            yMax = this.yMax;
		}

		/// <summary>
		/// Reports if a pixel is inside the contour
		/// </summary>
		/// <param name="X"> Pixel X-axis location </param>
		/// <param name="Y"> Pixel Y-axis location </param>
		/// <returns> Flag whether the point is outside (0) 
        /// or inside (1) the contour </returns>
		public bool Inside(int X, int Y)
		{
			// bounding box check
            //if (X < x.Min() || Y < y.Min() || X > x.Max() || Y > y.Max())
            //    return false;
            if (X < xMin || Y < yMin || X > xMax || Y > yMax)
                return false;

			int cIdx = x.Length - 1;
			bool oddCnt = false;
			for (int idx = 0; idx < x.Length; idx++)
			{
				if (y[idx] == Y && x[idx] == X) // on the line
					return true;
				if (y[idx] < Y && y[cIdx] >= Y || y[cIdx] < Y && 
                    y[idx] >= Y)
					if (x[idx] + (Y - y[idx]) / (y[cIdx] - y[idx]) * 
                        (x[cIdx] - x[idx]) < X)
						oddCnt = !oddCnt;
				cIdx = idx;
			}
			return oddCnt;
		}

        /// <summary>
        /// Returns a mask indicated where the pixel is inside the contour
        /// </summary>
        /// <returns> A boolean mask matching the size of the constructor 
        /// image </returns>
		public bool[,] InSide()
		{
			bool[,] inGrid = new bool[xLen, yLen];
			for (int xIdx = 0; xIdx < xLen; xIdx++)
				for (int yIdx = 0; yIdx < yLen; yIdx++)
					inGrid[xIdx, yIdx] = Inside(xIdx, yIdx);
			return inGrid;
		}

        /// <summary>
        /// Searches out the shape's edge and builds the Freeman chain
        /// </summary>
        /// <param name="img"> Input image </param>
		private void Build(T[,] img)
		{
			// Shifts to edge of contour
			while (inCont(img, xStart, yStart))
				--xStart; // Moves back
			xStart++;
            xMin = xMax = xStart;
            yMin = yMax = yStart;

			// checks for more connected pixels
			int x0 = xStart;
			int y0 = yStart;
			int dir = 0;
			int x1, y1;
            // Thin contours revisit pixels, so their edge walk can be longer
            // than the image's pixel count. Store only the visited boundary.
            long lenChain = (long)xLen * yLen * 8;
            List<int> tChain = new List<int>();
            List<int> tx = new List<int>();
            List<int> ty = new List<int>();

			// Checks direction of neighboring pixel
			for (; ; )
			{
				//Finds neighbor in direction dir
				if (Probe(img, x0, y0, dir, out x1, out y1))
					break;

				// Changes direction
				dir++;

				// Full rotation & no positive probes, then lone point
				if (dir == 8)
				{
					x[0] = x0;
					y[0] = y0;
					Chain[0] = 0;
					return;
				}
			}

			// Follows the edge clockwise, builds Freeman chain
			int counter = 0;
			int dir0 = 1; // up & right
			for (; ; )
			{
				if (counter >= lenChain)
                    throw new InvalidOperationException("The contour boundary did not close.");

				// Next point
				int dir1 = Neighbor(img, x0, y0, dir0, out x1, out y1);

				// Records direction
                tx.Add(x0);
                ty.Add(y0);
                tChain.Add(dir1);
                counter++;

				// Checks if chain has closed
				if (x0 == xStart && y0 == yStart && counter > 1)
					if (dir1 == tChain[0] ||
						 Math.Abs(dir1 - tChain[0]) == 1)
					{
                        x = tx.ToArray();
                        y = ty.ToArray();
                        Chain = tChain.ToArray();
						return;
					}

				// Update current state
				x0 = x1;
				y0 = y1;
				dir0 = dir1;

                // bounding box check
                if (xMax < x1)
                    xMax = x1;
                else if (xMin > x1)
                    xMin = x1;
                if (yMax < y1)
                    yMax = y1;
                else if (yMin > y1)
                    yMin = y1;
			}
		}

        /// <summary>
        /// Probes neighboring pixel in vector x0,y0,dir
        /// </summary>
        /// <param name="img"> Input image </param>
        /// <param name="x0"> Pixel X location </param>
        /// <param name="y0"> Pixel Y location </param>
        /// <param name="dir"> Probe (Search direction) </param>
        /// <param name="x1"> Probed pixel X location </param>
        /// <param name="y1"> Probed pixel Y location </param>
        /// <returns> The pixel is within the contour </returns>
		private bool Probe(T[,] img, int x0, int y0, int dir, out int x1, 
            out int y1)
		{
			x1 = x0;
			y1 = y0;

			if (dir < 2 || dir > 6)
				++x1;
			if (dir > 2 && dir < 6)
				--x1;
			if (dir > 0 && dir < 4)
				++y1;
			if (dir > 4)
				--y1;

			return (inCont(img, x1, y1));
		}

        /// <summary>
        /// Checks if pixel [x,y] is within the contour 
        /// </summary>
        /// <param name="img"> Input image </param>
        /// <param name="X"> Pixel X location </param>
        /// <param name="Y"> Pixel Y location </param>
        /// <returns> Boolean tag if the pixel is within the 
        /// contour </returns>
		private bool inCont(T[,] img, int X, int Y)
		{
			// Outside Grid
			if (X < 0 || X >= xLen || Y < 0 || Y >= yLen)
				return false;

			if (posCont)
			{
                return Operator<T>.GreaterThanOrEqual(img[X, Y], contVal);
			}
			else
			{
                return Operator<T>.LessThan(img[X, Y], contVal);
			}
		}

        /// <summary>
        /// Finds the direction and location of the next contour pixel
        /// </summary>
        /// <param name="img"> Input image </param>
        /// <param name="x0"> Pixel X location </param>
        /// <param name="y0"> Pixel Y location </param>
        /// <param name="dir0"> Initial probe direction </param>
        /// <param name="x1"> Updated pixel X location </param>
        /// <param name="y1"> Updated pixel Y location </param>
        /// <returns> Direction of next contour pixel </returns>
		private int Neighbor(T[,] img, int x0, int y0, int dir0, 
            out int x1, out int y1)
		{
			int dir1;
			x1 = 0;
			y1 = 0;

			// Determines next direction, clockwise rotation
			if (dir0 % 2 == 0)
				dir1 = dir0 + 2;
			else
				dir1 = dir0 + 1;

			// limits rotation
			if (dir1 > 7)
				dir1 -= 8;

			// Probes neighbors
			for (int Idx = 0; Idx < 8; Idx++)
			{
				if (Probe(img, x0, y0, dir1, out x1, out y1))
					return dir1;
				else
					if (--dir1 < 0)
						dir1 += 8;
			}

			// Should never get here
			return int.MinValue;
        }

        # endregion Methods
    }
}
