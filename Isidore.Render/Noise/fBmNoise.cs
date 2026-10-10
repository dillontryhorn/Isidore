using System;
using Isidore.Maths;

// This fBm is a conventional implementation found in the text
// Texture and Modeling, pg. 437, modified to accept frequency ranges
namespace Isidore.Render
{
    /// <summary>
    /// PerlinfBmNoise is a child of PerlinNoise that simulates
    /// fractional Brownian motion
    /// </summary>
    public class fBmNoise : FrequencyNoise
    {
        #region Fields & Properties        

        /// <summary>
        /// The fBm Hurst exponent
        /// </summary>
        public double Hurst { get; set; }

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
        /// <param name="Hurst"> The fBm Hurst exponent </param>
        /// <param name="lacunarity"> The texture spacing between frequencies
        /// for each step up in frequency (Normally 0 &lt; x &lt;= 1) </param>
        /// <param name="shift"> Additional spatial offset applied to the 
        /// noise coordinates</param>
        /// <param name="multiplier"> Factor multiplied to the noise 
        /// (Prior to offset) </param>
        /// <param name="offset"> Offset added to the noise (After 
        /// multiplication) </param>
        public fBmNoise(NoiseFunction noiseFunc = null,
            double minFreq = 1, double maxFreq = 512, double Hurst = 0.75,
            double lacunarity = 2.0, Vector shift = null,
            double multiplier = 1.0, double offset = 0.0)
            : base(noiseFunc, minFreq, maxFreq, lacunarity, shift, multiplier,
                  offset)
        {
            this.Hurst = Hurst;
        }

        /// <summary>
        /// Constructor for a fractional Brownian motion realization
        /// </summary>
        /// <param name="noiseFunc"> The noise function to access </param>
        /// <param name="minFreq"> Minimum frequency used in noise 
        /// creation </param>
        /// <param name="maxFreq"> Maximum frequency used in noise 
        /// creation </param>
        /// <param name="Hurst"> The fBm Hurst exponent </param>
        /// <param name="lacunarity"> The texture spacing between frequencies
        /// for each step up in frequency (Normally 0 &lt; x &lt;= 1) </param>
        /// <param name="noiseParams"> The noise parameters </param>
        public fBmNoise(NoiseFunction noiseFunc, double minFreq,
            double maxFreq, double Hurst, double lacunarity,
            NoiseParameters noiseParams) : this(noiseFunc, minFreq, maxFreq, 
                Hurst, lacunarity, noiseParams.shift, noiseParams.multiplier, 
                noiseParams.offset)
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
            return GetBaseVal(coord, Hurst);
        }

        /// <summary>
        /// Returns the noise value associated with the given coordinates.
        /// </summary>
        /// <param name="coord"> Noise coordinates </param>
        /// <param name="Hurst"> Hurst exponent </param>
        /// <returns> Noise value and components at the given 
        /// coordinates</returns>
        public virtual double GetBaseVal(Point coord, double Hurst)
        {
            // Calculates frequency noise components
            double[] noises = GetComponents(coord);

            // Cycles through octaves
            double noise = 0;
            for (int idx = 0; idx < Frequency.Length; idx++)
            {
                // Anchors the frequency noise to a power
                double power = Math.Pow(Frequency[idx], -Hurst);
                double inoise = noises[idx] * power;

                // Adds the frequency noise to the summed noise
                noise += inoise;
            }

            return noise;
        }

        /// <summary>
        /// Clones this copy by performing a deep copy
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        new public fBmNoise Clone()
        {
            return CloneImp() as fBmNoise;
        }

        /// <summary>
        /// Deep-copy clone of this instance
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        protected override Noise CloneImp()
        {
            // Shallow copies from base
            fBmNoise newCopy = base.CloneImp() as fBmNoise;

            // Deep-copies all data this is referenced by default

            return newCopy;
        }

        #endregion Methods
    }
}
