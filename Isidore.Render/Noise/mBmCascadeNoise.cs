using System;
using Isidore.Maths;

// This multiplicative cascade mBm is found in the text 
// Texture and Modeling, pg. 440
namespace Isidore.Render
{
    /// <summary>
    /// mBmCascadeNoise is a child of fBmNoise that simulates
    /// multi-fractional Brownian motion using multiplicative cascade with 
    /// a scalar offset between frequencies
    /// </summary>
    public class mBmCascadeNoise : fBmNoise
    {
        #region Fields & Properties

        /// <summary>
        /// Multiplicative cascade offset applied to each scale 
        /// (Controls the degree of multi-fractal effect)
        /// </summary>
        public double cascadeOffset { get; set; }

        #endregion Fields & Properties
        #region Constructors

        /// <summary>
        /// Constructor for a multiplicative cascade multi-fractional Brownian
        /// motion realization
        /// </summary>
        /// <param name="noiseFunc"> The noise function to access </param>
        /// <param name="minFreq"> Minimum frequency used in noise 
        /// creation </param>
        /// <param name="maxFreq"> Maximum frequency used in noise 
        /// creation </param>
        /// <param name="Hurst"> The fBm Hurst exponent </param>
        /// <param name="lacunarity"> The texture spacing between frequencies
        /// for each step up in frequency (Normally 0 &lt; x &lt;= 1) </param>
        /// <param name="cascadeOffset"> Multiplicative cascade offset applied
        /// to each scale </param>
        /// <param name="shift"> Additional spatial offset applied to the 
        /// noise coordinates</param>
        /// <param name="multiplier"> Factor multiplied to the noise 
        /// (Prior to offset) </param>
        /// <param name="offset"> Offset added to the noise (After 
        /// multiplication) </param>
        public mBmCascadeNoise(NoiseFunction noiseFunc = null,
            double minFreq = 1, double maxFreq = 512, double Hurst = 0.75,
            double lacunarity = 2.0, double cascadeOffset = 0.8,
            Vector shift = null, double multiplier = 1.0, 
            double offset = 0.0) : base(noiseFunc, minFreq, maxFreq, Hurst,
                lacunarity, shift, multiplier, offset)
        {
            this.cascadeOffset = cascadeOffset;
        }

        /// <summary>
        /// Constructor for a multiplicative cascade multi-fractional Brownian
        /// motion realization
        /// </summary>
        /// <param name="noiseFunc"> The noise function to access </param>
        /// <param name="minFreq"> Minimum frequency used in noise 
        /// creation </param>
        /// <param name="maxFreq"> Maximum frequency used in noise 
        /// creation </param>
        /// <param name="Hurst"> The fBm Hurst exponent </param>
        /// <param name="lacunarity"> The texture spacing between frequencies
        /// for each step up in frequency (Normally 0 &lt; x &lt;= 1) </param>
        /// <param name="cascadeOffset"> Multiplicative cascade offset applied
        /// to each scale </param>
        /// <param name="noiseParams"> The noise parameters </param>
        public mBmCascadeNoise(NoiseFunction noiseFunc, double minFreq,
            double maxFreq, double Hurst, double lacunarity,
            double cascadeOffset, NoiseParameters noiseParams) : this(
                noiseFunc, minFreq, maxFreq, Hurst, lacunarity, cascadeOffset,
                noiseParams.shift, noiseParams.multiplier, noiseParams.offset)
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
            // Calculates frequency noise components
            double[] noises = GetComponents(coord);

            // Cycles through octaves
            double noise = 1;
            for (int idx = 0; idx < Frequency.Length; idx++)
            {
                // Anchors the frequency noise to a power
                double power = Math.Pow(Frequency[idx], -Hurst);
                double inoise = (noises[idx] + cascadeOffset) * power;

                // Adds the frequency noise to the summed noise
                noise *= inoise;
            }

            return noise;
        }


        /// <summary>
        /// Clones this copy by performing a deep copy
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        new public mBmCascadeNoise Clone()
        {
            return CloneImp() as mBmCascadeNoise;
        }

        /// <summary>
        /// Deep-copy clone of this instance
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        protected override Noise CloneImp()
        {
            // Shallow copies from base
            mBmCascadeNoise newCopy = base.CloneImp() as mBmCascadeNoise;

            // Deep-copies all data this is referenced by default

            return newCopy;
        }

        #endregion Methods
    }
}
