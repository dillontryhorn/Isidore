using System;
using Isidore.Maths;

// SpectrumNoise function applies power spectral densities to noise models
namespace Isidore.Render
{
    /// <summary>
    /// The SpectrumNoise class is used to apply a power spectral density to
    /// noise levels.
    /// </summary>
    public class SpectrumNoise : Noise
    {
        #region Fields & Properties

        /// <summary>
        /// Power spectral density for setting the frequency and power for
        /// each noise level
        /// </summary>
        public PowerSpectrum powerSpectrum;


        #endregion Fields & Properties
        #region Constructor

        /// <summary>
        /// SpectrumNoise constructor
        /// </summary>
        /// <param name="noiseFunc"> The noise function to access </param>
        /// <param name="powerSpectrum"> Power spectral density to apply </param>
        /// <param name="shift"> Additional spatial offset applied to the 
        /// noise coordinates (Default is for three dimension.) </param>
        /// <param name="multiplier"> Factor multiplied to the noise 
        /// (Prior to offset) </param>
        /// <param name="offset"> Offset added to the noise (After 
        /// multiplication) </param>
        public SpectrumNoise(NoiseFunction noiseFunc = null, 
            PowerSpectrum powerSpectrum = null,
            Vector shift = null, double multiplier = 1.0, 
            double offset = 0.0) : base(noiseFunc, shift, multiplier, offset)
        {
            if (powerSpectrum == null)
            {
                int len = 20;
                double[] freq = new double[len + 1];
                double[] power = new double[len + 1];
                for(int idx = 0; idx <= len; idx++)
                {
                    freq[idx] = Math.Pow(2, idx);
                    power[idx] = Math.Pow(2, -idx / 3);
                }
                this.powerSpectrum = new PowerSpectrum(freq, power);
            }
            else
                this.powerSpectrum = powerSpectrum;
        }

        #endregion Constructor
        #region Methods

        /// <summary>
        /// Returns the noise value associated with the given coordinates,
        /// as well as the component noise used
        /// </summary>
        /// <param name="coord"> Noise coordinates </param>
        /// <returns> Noise value </returns>
        public override double GetBaseVal(Point coord)
        {
            // Calls GetComponents
            double[] noise = GetComponents(coord);

            // Multiplies each component to its matching power
            double val = 0;
            for (int idx = 0; idx < noise.Length; idx++)
                val += noise[idx] * powerSpectrum.Power[idx];

            return val;
        }

        /// <summary>
        /// Returns a list of the component noise values and corresponding 
        /// frequencies for the given coordinates
        /// </summary>
        /// <param name="coord"> Noise coordinates </param>
        /// <returns> Tuple containing the frequency and noise lists </returns>
        public virtual double[] GetComponents(Point coord)
        {
            // Noise components
            double[] noise = new double[powerSpectrum.Frequency.Length];

            // Steps through each frequency
            Point ssCoord = new Point(coord.Comp.Length);
            for(int idx=0; idx<noise.Length; idx++)
            {
                // Scales the noise coordinates to the matching frequency
                for (int iidx = 0; iidx < coord.Comp.Length; iidx++)
                    ssCoord.Comp[iidx] = coord.Comp[iidx] * 
                        powerSpectrum.Frequency[idx];

                // Retrieves noise
                noise[idx] = noiseFunc.GetVal(ssCoord);
            }

            return noise;
        }
        
        /// <summary>
        /// Deep-copy (Non-referenced) clone
        /// </summary>
        /// <returns> Cloned copy </returns>
        new public SpectrumNoise Clone()
        {
            return CloneImp() as SpectrumNoise;
        }

        /// <summary>
        /// Clone implementation. Uses MemberwiseClone to clone, and 
        /// inheriting classes will implement the cloning of
        /// specific data types 
        /// </summary>
        /// <returns> Clone copy </returns>
        protected override Noise CloneImp()
        {
            // Shallow copies from base
            SpectrumNoise newCopy = base.CloneImp() as SpectrumNoise;

            // Deep copy
            newCopy.powerSpectrum = powerSpectrum.Clone();

            return newCopy;
        }

        #endregion Methods
    }
}
