using System;
using Isidore.Maths;

// This Perlin turbulence is based on Texture and Modeling & PBRT, with a 
// modification from Jones, Computer Graphics, that uses frequency instead
// of octaves
namespace Isidore.Render
{
    /// <summary>
    /// PerlinTurbulenceNoise is a child of PerlinNoise that simulates
    /// a generic graphical turbulence
    /// </summary>
    public class PerlinTurbulenceNoise : FrequencyNoise
    {
        #region Fields & Properties        

        /// <summary>
        /// Controls whether the absolute values of the noise is added for each
        /// octave. Used by some turbulence models.
        /// </summary>
        public bool absoluteValueNoise { get; set; }

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
        /// <param name="absoluteValueNoise"> Flag for whether the octave
        /// noise is added (F) or its absolute values (T) </param>
        /// <param name="lacunarity"> The texture spacing between frequencies
        /// for each step up in frequency (Normally 0 &lt; x &lt;= 1) </param>
        /// <param name="shift"> Additional spatial offset applied to the 
        /// noise coordinates</param>
        /// <param name="multiplier"> Factor multiplied to the noise 
        /// (Prior to offset) </param>
        /// <param name="offset"> Offset added to the noise (After 
        /// multiplication) </param>
        public PerlinTurbulenceNoise(NoiseFunction noiseFunc = null,
            double minFreq = 1, double maxFreq = 512,
            bool absoluteValueNoise = false, double lacunarity = 2.0,
            Vector shift = null, double multiplier = 1.0, 
            double offset = 0.0) 
            : base(noiseFunc, minFreq, maxFreq,
                lacunarity, shift, multiplier, offset)
        {
            this.absoluteValueNoise = absoluteValueNoise;
        }

        /// <summary>
        /// Constructor for a fractional Brownian motion realization
        /// </summary>
        /// <param name="noiseFunc"> The noise function to access </param>
        /// <param name="minFreq"> Minimum frequency used in noise 
        /// creation </param>
        /// <param name="maxFreq"> Maximum frequency used in noise 
        /// creation </param>
        /// <param name="absoluteValueNoise"> Flag for whether the octave
        /// noise is added (F) or its absolute values (T) </param>
        /// <param name="lacunarity"> The texture spacing between frequencies
        /// for each step up in frequency (Normally 0 &lt; x &lt;= 1) </param>
        /// <param name="noiseParams"> The noise parameters </param>
        public PerlinTurbulenceNoise(NoiseFunction noiseFunc, double minFreq, 
            double maxFreq, bool absoluteValueNoise, double lacunarity,
            NoiseParameters noiseParams) : this(noiseFunc, minFreq, maxFreq,
                absoluteValueNoise, lacunarity, noiseParams.shift,
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
            // Calculates frequency noise components
            double[] noises = GetComponents(coord);

            // Cycles through octaves
            double noise = 0;
            for (int idx = 0; idx < Frequency.Length; idx++)
            {
                // Scales power
                double inoise = noises[idx] / Frequency[idx];

                // Takes the absolute value if flagged
                if (absoluteValueNoise)
                    inoise = Math.Abs(inoise);

                // Adds the frequency noise to the summed noise
                noise += inoise;
            }

            return noise;
        }

        /// <summary>
        /// Clones this copy by performing a deep copy
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        new public PerlinTurbulenceNoise Clone()
        {
            return (PerlinTurbulenceNoise)CloneImp();
        }

        /// <summary>
        /// Deep-copy clone of this instance
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        protected override Noise CloneImp()
        {
            // Shallow copies from base
            PerlinTurbulenceNoise newCopy = base.CloneImp() as PerlinTurbulenceNoise;

            // Deep-copies all data this is referenced by default

            return newCopy;
        }

        #endregion Methods
    }
}
