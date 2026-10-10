using Isidore.Maths;
using System;

// This Perlin marble is based on Texture and Modeling...(Ebert), with a 
// modification from Computer Graphics...(Jones), that uses frequency instead
// of octaves, with some additional parameters for more flexibility and only
// calculates the brightness and not the color
namespace Isidore.Render
{
    /// <summary>
    /// PerlinMarbleNoise simulates a marbling pattern that is commonly used
    /// to simulate plasmas.  This implementation does not use Ebert's 2003
    /// color spline formulation nor Perlin's 1985 color interpolation but 
    /// instead returns the brightness, which is a distorted sine function
    /// spanning [-1,1)
    /// </summary>
    public class PerlinMarbleNoise:PerlinTurbulenceNoise
    {
        #region Fields & Properties

        /// <summary>
        /// Marble distortion multiplier.  Larger values have more distortion
        /// </summary>
        public double marbleMultiplier { get; set; }

        /// <summary>
        /// Spatial frequency of initial intensity oscillation
        /// </summary>
        public double frequency { get; set; }

        /// <summary>
        /// Spatial dimension to apply the marbling
        /// </summary>
        public int marbleDim { get; set; }

        #endregion Fields & Properties
        #region Constructors

        /// <summary>
        /// Constructor for a fractional Brownian motion realization
        /// </summary>
        /// <param name="noiseFunc"> The noise function to access </param>
        /// <param name="minFreq"> Minimum frequency used in noise 
        /// creation </param>
        /// <param name="maxFreq"> Maximum frequency used in noise 
        /// creation </param>
        /// <param name="marbleMultiplier"> Marble distortion factor </param>
        /// <param name="frequency"> Spatial intensity oscillation 
        /// frequency </param>
        /// <param name="marbleDim"> Dimension to apply the marbling </param>
        /// <param name="lacunarity"> The texture spacing between frequencies
        /// for each step up in frequency (Normally 0 &lt; x &lt;= 1) </param>
        /// <param name="shift"> Additional spatial offset applied to the 
        /// noise coordinates</param>
        /// <param name="multiplier"> Factor multiplied to the noise 
        /// (Prior to offset) </param>
        /// <param name="offset"> Offset added to the noise (After 
        /// multiplication) </param>
        public PerlinMarbleNoise(NoiseFunction noiseFunc = null,
            double minFreq = 1, double maxFreq = 512, 
            double marbleMultiplier = 3, double frequency = 1, 
            int marbleDim = 0, double lacunarity = 2.0, 
            Vector shift = null, double multiplier = 1.0, 
            double offset = 0.0) 
            : base(noiseFunc, minFreq, maxFreq, false, lacunarity, shift, 
                  multiplier,offset)
        {
            this.marbleMultiplier = marbleMultiplier;
            this.frequency = frequency;
            this.marbleDim = marbleDim;
        }

        /// <summary>
        /// Constructor for a fractional Brownian motion realization
        /// </summary>
        /// <param name="noiseFunc"> The noise function to access </param>
        /// <param name="minFreq"> Minimum frequency used in noise 
        /// creation </param>
        /// <param name="maxFreq"> Maximum frequency used in noise 
        /// creation </param>
        /// <param name="marbleMultiplier"> Marble distortion factor </param>
        /// <param name="frequency"> Spatial intensity oscillation 
        /// frequency </param>
        /// <param name="marbleDim"> Dimension to apply the marbling </param>
        /// <param name="lacunarity"> The texture spacing between frequencies
        /// for each step up in frequency (Normally 0 &lt; x &lt;= 1) </param>
        /// <param name="noiseParams"> The noise parameters </param>
        public PerlinMarbleNoise(NoiseFunction noiseFunc, double minFreq,
            double maxFreq, double marbleMultiplier, double frequency,
            int marbleDim, double lacunarity, NoiseParameters noiseParams) :
            this(noiseFunc, minFreq, maxFreq, marbleMultiplier, frequency,
                marbleDim, lacunarity, noiseParams.shift,
                noiseParams.multiplier, noiseParams.offset)
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
        public override double GetBaseVal(Point coord)
        {
            // Gets noise value for PerlinTurbulence
            double noiseT = base.GetBaseVal(coord);

            // Adds marbling factor and dimensional offset
            double noise = coord.Comp[marbleDim] + marbleMultiplier * noiseT;

            // Calculates the "brightness"
            double bright = Math.Sin(2 * Math.PI * frequency * noise);

            return bright;
        }

        /// <summary>
        /// Returns a Color structure using Ebert's 2003 color spline process
        /// </summary>
        /// <param name="coord"> Noise coordinates </param>
        /// <returns> Color at the given coordinates </returns>
        public System.Drawing.Color GetRGB(Point coord)
        {
            // Gets brightness value
            double bright = GetVal(coord);

            // Takes the brightness and coverts it to a color representation
            bright = Math.Sqrt((bright + 1) / 2);
            double G = 0.3 + 0.8 * bright;
            bright = Math.Sqrt(bright);
            double R = 0.3 + 0.6 * bright;
            double B = 0.6 + 0.4 * bright;

            System.Drawing.Color color = System.Drawing.Color.FromArgb((int)R, (int)G, (int)B);
            return color;
        }

        /// <summary>
        /// Clones this copy by performing a deep copy
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        new public PerlinMarbleNoise Clone()
        {
            return (PerlinMarbleNoise)CloneImp();
        }

        /// <summary>
        /// Deep-copy clone of this instance
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        protected override Noise CloneImp()
        {
            // Shallow copies from base
            PerlinMarbleNoise newCopy = base.CloneImp() as PerlinMarbleNoise;

            // Deep-copies all data this is referenced by default

            return newCopy;
        }

        #endregion Methods
    }
}
