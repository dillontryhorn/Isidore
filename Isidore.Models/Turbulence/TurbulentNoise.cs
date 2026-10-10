using System;
using Isidore.Maths;
using Isidore.Render;

// This is applies fBm noise in a way consistent with (optical) turbulence
namespace Isidore.Models
{
    /// <summary>
    /// TurbulentNoise is a child of fBmNoise that simulates fBm
    /// noise in a method consistent with (optical) turbulence.  
    /// It assumes lacunarity=2 and a perlin gradient noise function
    /// </summary>
    public class TurbulentNoise : fBmNoise
    {
        #region Fields & Properties        

        /// <summary>
        /// Noise value mean
        /// </summary>
        public double Mean { get; set; }

        /// <summary>
        /// Noise value standard deviation
        /// </summary>
        public double STD { get; set; }

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
        /// <param name="mean"> Noise mean </param>
        /// <param name="std"> Noise standard deviation </param>
        /// <param name="noiseDist"> Noise distribution enumeration </param>
        /// <param name="noisePos"> Position in the noise lattice 
        /// corresponding to the origin location </param>
        public TurbulentNoise(PerlinNoiseFunction noiseFunc = null,
            double minFreq = 1, double maxFreq = 512, double Hurst = 0.75,
            double mean = 0.0, double std = 1.0, 
            NoiseDistribution noiseDist = 0,  Vector noisePos = null)
            : base(noiseFunc, minFreq, maxFreq, Hurst, 2, noisePos, 1, 0)
        {
            // Sets local parameters
            Mean = mean;
            STD = std;

            // Sets the noise model's distribution
            distFunc = DistFunc(noiseDist);

            // Makes certain the standard normal flag is set
            ((PerlinNoiseFunction)this.noiseFunc).StandardNormal = true;
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
        public override double GetBaseVal(Point coord, double Hurst)
        {
            // Calculates frequency noise components
            double[] noises = GetComponents(coord);

            // Cycles through octaves
            double noise = 0; // Noise value
            double fac = 0; // Standard normal scaling factor
            for (int idx = 0; idx < Frequency.Length; idx++)
            {
                // Anchors the frequency noise to a power
                double power = Math.Pow(Frequency[idx], -Hurst);
                double inoise = noises[idx] * power;

                // Adds the frequency noise to the summed noise
                noise += inoise;
                // Unit-variance octave contributions have variance power^2
                // after weighting. Normalize by the sum of these variances.
                fac += power * power;
            }

            // Scales noise value to standard normal;
            noise /= Math.Sqrt(fac);

            // Scales noise value to STD range (Duplicates Noise process)
            noise *= STD;

            // Adds mean to the value (Duplicates Noise process)
            noise += Mean;

            // Distribution handled in Noise
            return noise;
        }

        /// <summary>
        /// Clones this copy by performing a deep copy
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        new public TurbulentNoise Clone()
        {
            return CloneImp() as TurbulentNoise;
        }

        /// <summary>
        /// Deep-copy clone of this instance
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        protected override Noise CloneImp()
        {
            // Shallow copies from base
            TurbulentNoise newCopy = base.CloneImp() as TurbulentNoise;

            // Deep-copies all data this is referenced by default

            return newCopy;
        }

        #endregion Methods
    }
}
