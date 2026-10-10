using System;
using System.Linq;
using Isidore.Maths;

// This is a largely conventional implementation of improved Perlin noise 
// (2002) including a hash table.  See Texture & Modeling, pg. 337

namespace Isidore.Render
{
    /// <summary>
    /// PerlinNoise is a child of NoiseFunction that implements the improved
    /// Perlin noise model (2002) with an exception.  This version does not 
    /// limit the permutation table to 256 but instead allows the user to 
    /// specify the base-2-power size.  Perlin noise is limited to a maximum 
    /// of three dimensions.
    /// </summary>
    public class PerlinNoiseFunction : NoiseFunction
    {
        #region Fields & Properties

        /// <summary>
        /// Permuted lookup table
        /// </summary>
        public int[] LUT { get { return lut; } }
        /// <summary>
        /// Permuted lookup table
        /// </summary>
        private int[] lut;

        /// <summary>
        /// The length of the table (Saves redundant calculations)
        /// </summary>
        private int tableSize;

        /// <summary>
        /// Zero index table length
        /// </summary>
        private int lim;

        /// <summary>
        /// Smoothstep polynomial used for interpolation
        /// </summary>
        public SmoothStep Polynomial { get { return polynomial; } }

        /// <summary>
        /// Smoothstep polynomial
        /// </summary>
        private SmoothStep polynomial;

        /// <summary>
        /// Scales the noise values to fit a standard normal distribution
        /// </summary>
        public bool StandardNormal;

        #endregion Fields & Properties
        #region Constructors

        /// <summary>
        /// Constructor for a realization of Perlin noise
        /// </summary>
        /// <param name="rng"> Random realization </param>
        /// <param name="tablePower"> Exponent power of two  for the table 
        /// size (8 = 256 element table) </param>
        /// <param name="standardNormal"> Applies a factor to fit the noise
        /// to a standard normal distribution </param>
        /// <param name="S"> Smooth step order (0-6, 2 = improved) </param>
        public PerlinNoiseFunction(Random rng, int tablePower, 
            bool standardNormal, int S)
        {
            // Sets the smoothstep polynomial
            polynomial = new SmoothStep(S);

            // Determines the table size
            tableSize = 1 << tablePower;
            lim = tableSize - 1;

            // The table needs to be twice for Grad
            System.Collections.Generic.IEnumerable<int> hLUT = Arr.Permutation(tableSize, rng);
            lut = hLUT.Concat(hLUT).ToArray();

            // Flags the scaling factor
            StandardNormal = standardNormal;
        }

        /// <summary>
        /// Constructor for a realization of Perlin noise
        /// </summary>
        /// <param name="randomSeed"> Random realization seed </param>
        /// <param name="tablePower"> Exponent power of two  for the table 
        /// size (default = 8 (256 element table)) </param>
        /// <param name="standardNormal"> Applies a factor to fit the noise
        /// to a standard normal distribution </param>
        /// <param name="S"> Smooth step order (0-6, 2 = improved) </param>
        public PerlinNoiseFunction(int? randomSeed = null, int tablePower = 8,
            bool standardNormal = false, int S = 2) : this((randomSeed == null) ? 
                new Random() : new Random((int)randomSeed), tablePower, 
                standardNormal, S)
        {
        }

        #endregion Constructors
        #region Methods

        /// <summary>
        /// Returns the noise value associated with the given coordinates.
        /// </summary>
        /// <param name="coord"> Noise coordinates </param>
        /// <returns> Noise value and components at the given 
        /// coordinates</returns>
        public override double GetVal(Point coord)
        {
            // Limits coordinates to three dimensions
            double noise = 0;
            if (coord.Comp.Length < 4)
            {
                noise = CalcNoise3D(coord);
                // Value calculated from PerlinNoiseStandardFactorTest.cs
                if (StandardNormal)
                    noise *= 3.901151;
            }
            if (coord.Comp.Length == 4)
            {
                noise = CalcNoise4D(coord);
                // Value calculated from PerlinNoiseStandardFactorTest.cs
                if (StandardNormal)
                    noise *= 3.611488;
            }
            if (coord.Comp.Length > 4)
                throw new System.ArgumentException(
                    "Perlin Noise extend only to four dimensions", "coord");

            return noise;
        }

        /// <summary>
        /// This is the main Perlin noise function
        /// </summary>
        /// <param name="coord"> Noise coordinates </param>
        /// <returns> Noise value at the given coordinates </returns>
        private double CalcNoise3D(Point coord)
        {
            // Parses data
            double x = (coord.Comp.Length > 0) ? coord.Comp[0] : 0;
            double y = (coord.Comp.Length > 1) ? coord.Comp[1] : 0;
            double z = (coord.Comp.Length > 2) ? coord.Comp[2] : 0;

            // Coordinate whole values
            long ix = (long)Math.Floor(x);
            long iy = (long)Math.Floor(y);
            long iz = (long)Math.Floor(z);

            // Coordinate fractional values 
            double dx = x - ix;
            double dy = y - iy;
            double dz = z - iz;

            // Maps coordinates to the wrapped tables
            ix &= tableSize - 1;
            iy &= tableSize - 1;
            iz &= tableSize - 1;

            // Hash coordinates for the 8 cube corners
            long p0 = lut[ix] + iy;
            long p1 = lut[ix + 1] + iy;
            long p00 = lut[p0] + iz;
            long p01 = lut[p0 + 1] + iz;
            long p10 = lut[p1] + iz;
            long p11 = lut[p1 + 1] + iz;

            // Compute gradient weights
            double g000 = Grad3D(lut[p00], dx, dy, dz);
            double g100 = Grad3D(lut[p10], dx - 1, dy, dz);
            double g010 = Grad3D(lut[p01], dx, dy - 1, dz);
            double g110 = Grad3D(lut[p11], dx - 1, dy - 1, dz);
            double g001 = Grad3D(lut[p00 + 1], dx, dy, dz - 1);
            double g101 = Grad3D(lut[p10 + 1], dx - 1, dy, dz - 1);
            double g011 = Grad3D(lut[p01 + 1], dx, dy - 1, dz - 1);
            double g111 = Grad3D(lut[p11 + 1], dx - 1, dy - 1, dz - 1);

            // Fade (pull weights)
            double fx = polynomial.Evaluate(dx);
            double fy = polynomial.Evaluate(dy);
            double fz = polynomial.Evaluate(dz);

            // Compute trilinear interpolation of weights
            // 4 on X axis
            double x00 = Lerp(fx, g000, g100);
            double x10 = Lerp(fx, g010, g110);
            double x01 = Lerp(fx, g001, g101);
            double x11 = Lerp(fx, g011, g111);
            // 2 on Y axis
            double y0 = Lerp(fy, x00, x10);
            double y1 = Lerp(fy, x01, x11);
            // 1 on Z axis
            double val = Lerp(fz, y0, y1);

            return val;
        }

        /// <summary>
        /// This is the 3D gradient algorithm used with the weights to 
        /// determine the interpolated value of a given coordinate
        /// </summary>
        /// <param name="hash"> Hash coordinate </param>
        /// <param name="x"> First coordinate in the cube </param>
        /// <param name="y"> Second coordinate in the cube </param>
        /// <param name="z"> Third coordinate in the cube </param>
        /// <returns> Local gradient at the coordinate </returns>
        private double Grad3D(int hash, double x, double y, double z)
        {
            // Converts the hash one of 16 gradient directions (0-15)
            int h = hash & 15;

            //// Coverts the hash to one of three orientations (0-2)
            double a = h < 8 || h == 12 || h == 13 ? x : y;
            double b = h < 4 || h == 12 || h == 13 ? y : z;
            // Reverse gradient direction based on the hash
            double g0 = (h & 1) > 0 ? -a : a;
            double g1 = (h & 2) > 0 ? -b : b;

            // Combines the component gradients 
            double grad = g0 + g1;

            return grad;
        }

        /// <summary>
        /// This is the main Perlin noise function
        /// </summary>
        /// <param name="coord"> Noise coordinates </param>
        /// <returns> Noise value at the given coordinates </returns>
        private double CalcNoise4D(Point coord)
        {
            // Parses data
            double x = (coord.Comp.Length > 0) ? coord.Comp[0] : 0;
            double y = (coord.Comp.Length > 1) ? coord.Comp[1] : 0;
            double z = (coord.Comp.Length > 2) ? coord.Comp[2] : 0;
            double w = (coord.Comp.Length > 3) ? coord.Comp[3] : 0;

            // Hypercube containing the point
            long ix = (long)Math.Floor(x);
            long iy = (long)Math.Floor(y);
            long iz = (long)Math.Floor(z);
            long iw = (long)Math.Floor(w);

            // Point location within the hypercube 
            double dx = x - ix;
            double dy = y - iy;
            double dz = z - iz;
            double dw = w - iw;

            // Maps the point to the permuted table
            ix &= lim;
            iy &= lim;
            iz &= lim;
            iw &= lim;

            // Hash coordinates for the 16 hypercube corners (i.e. four axes)
            long p0 = lut[ix] + iy;
            long p1 = lut[ix + 1] + iy;
            long p00 = lut[p0] + iz;
            long p01 = lut[p0 + 1] + iz;
            long p10 = lut[p1] + iz;
            long p11 = lut[p1 + 1] + iz;
            long p000 = lut[p00] + iw;
            long p001 = lut[p00 + 1] + iw;
            long p010 = lut[p01] + iw;
            long p011 = lut[p01 + 1] + iw;
            long p100 = lut[p10] + iw;
            long p101 = lut[p10 + 1] + iw;
            long p110 = lut[p11] + iw;
            long p111 = lut[p11 + 1] + iw;

            // Each node's gradient
            // 16 nodes/gradients
            double g0000 = Grad4D(lut[p000], dx, dy, dz, dw);
            double g1000 = Grad4D(lut[p100], dx - 1, dy, dz, dw);
            double g0100 = Grad4D(lut[p010], dx, dy - 1, dz, dw);
            double g1100 = Grad4D(lut[p110], dx - 1, dy - 1, dz, dw);
            double g0010 = Grad4D(lut[p001], dx, dy, dz - 1, dw);
            double g1010 = Grad4D(lut[p101], dx - 1, dy, dz - 1, dw);
            double g0110 = Grad4D(lut[p011], dx, dy - 1, dz - 1, dw);
            double g1110 = Grad4D(lut[p111], dx - 1, dy - 1, dz - 1, dw);
            double g0001 = Grad4D(lut[p000 + 1], dx, dy, dz, dw - 1);
            double g1001 = Grad4D(lut[p100 + 1], dx - 1, dy, dz, dw - 1);
            double g0101 = Grad4D(lut[p010 + 1], dx, dy - 1, dz, dw - 1);
            double g1101 = Grad4D(lut[p110 + 1], dx - 1, dy - 1, dz, dw - 1);
            double g0011 = Grad4D(lut[p001 + 1], dx, dy, dz - 1, dw - 1);
            double g1011 = Grad4D(lut[p101 + 1], dx - 1, dy, dz - 1, dw - 1);
            double g0111 = Grad4D(lut[p011 + 1], dx, dy - 1, dz - 1, dw - 1);
            double g1111 = Grad4D(lut[p111 + 1], dx - 1, dy - 1, dz - 1, dw - 1);

            // Fade (pull weights)
            double fx = polynomial.Evaluate(dx);
            double fy = polynomial.Evaluate(dy);
            double fz = polynomial.Evaluate(dz);
            double fw = polynomial.Evaluate(dw);

            // Interpolates the gradients
            // 8 on X axis
            double x000 = Lerp(fx, g0000, g1000);
            double x100 = Lerp(fx, g0100, g1100);
            double x010 = Lerp(fx, g0010, g1010);
            double x110 = Lerp(fx, g0110, g1110);
            double x001 = Lerp(fx, g0001, g1001);
            double x101 = Lerp(fx, g0101, g1101);
            double x011 = Lerp(fx, g0011, g1011);
            double x111 = Lerp(fx, g0111, g1111);
            // 4 on Y axis
            double y00 = Lerp(fy, x000, x100);
            double y10 = Lerp(fy, x010, x110);
            double y01 = Lerp(fy, x001, x101);
            double y11 = Lerp(fy, x011, x111);
            // 2 on Z axis
            double z0 = Lerp(fz, y00, y10);
            double z1 = Lerp(fz, y01, y11);
            // 1 on W axis
            double val = Lerp(fw, z0, z1);

            return val;
        }

        /// <summary>
        /// This is the 4D gradient algorithm used with the weights to 
        /// determine the interpolated value of a given coordinate
        /// </summary>
        /// <param name="hash"> Hash coordinate </param>
        /// <param name="x"> First coordinate in the hypercube </param>
        /// <param name="y"> Second coordinate in the hypercube </param>
        /// <param name="z"> Third coordinate in the hypercube </param>
        /// <param name="w"> Forth coordinate in the hypercube </param>
        /// <returns> Local gradient at the coordinate </returns>
        private double Grad4D(int hash, double x, double y, double z, double w)
        {
            // Converts the hash one of 32 gradient directions (0-31)
            int h = hash & 31;

            // Coverts the hash to one of four orientations (0-3)
            int hSwitch = h >> 3;
            // hSwitch = 0
            double a = x;
            double b = y;
            double c = z;
            // hSwitch = 1-3
            switch (hSwitch)
            {
                case 1:
                    a = w;
                    b = x;
                    c = y;
                    break;
                case 2:
                    a = z;
                    b = w;
                    c = x;
                    break;
                case 3:
                    a = y;
                    b = z;
                    c = w;
                    break;
            }

            // Reverse gradient direction based on the hash
            double g0 = (h & 4) == 0 ? -a : a;
            double g1 = (h & 2) == 0 ? -b : b;
            double g2 = (h & 1) == 0 ? -c : c;

            // Combines the component gradients &
            double grad = g0 + g1 + g2;

            return grad;
        }

        /// <summary>
        /// Perlin noise linear interpolator
        /// </summary>
        /// <param name="t"> Distance between v1 and v2 </param>
        /// <param name="v1"> First point  </param>
        /// <param name="v2"> Second point </param>
        /// <returns></returns>
        private static double Lerp(double t, double v1, double v2)
        {
            return v1 + t * (v2 - v1);
        }

        /// <summary>
        /// Clones this copy by performing a deep copy
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        new public PerlinNoiseFunction Clone()
        {
            return (PerlinNoiseFunction)CloneImp();
        }

        /// <summary>
        /// Deep-copy clone of this instance
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        protected override NoiseFunction CloneImp()
        {
            // Shallow copies from base
            PerlinNoiseFunction newCopy = base.CloneImp() as PerlinNoiseFunction;

            // Deep-copies all data this is referenced by default
            newCopy.lut = lut.Clone() as int[];
            newCopy.polynomial = polynomial.Clone();
            //newCopy.SmoothStep = SmoothStep.Clone() as Func<double, double>;

            return newCopy;
        }
        #endregion Methods
    }
}
