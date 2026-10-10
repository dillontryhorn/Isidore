using System;
using System.Collections.Generic;
using Isidore.Maths;

// Noise function generate the synthetic values for procedural texturing
// By using an OOP approach, we are able to keep a general solution while
// maintaining individual noise realizations.

namespace Isidore.Render
{
    /// <summary>
    /// The FrequencyNoise class is used to apply a frequency scaling 
    /// component to noise objects.  As with noise, it is mainly used
    /// as a base class for other frequency noise models.
    /// </summary>
    public class FrequencyNoise : Noise
    {
        #region Fields & Properties

        /// <summary>
        /// Minimum noise frequency (Must be 0 &lt; x).  This value will be
        /// set to ensure an integer scale value 
        /// </summary>
        public double minFreq
        {
            get { return minfreq; }
            set
            {
                if (!(value > 0) || double.IsInfinity(value))
                    throw new ArgumentException(
                        "Frequency must be greater than 0", "minFreq");
                else if (value > maxFreq)
                    throw new ArgumentException(
                        "Minimum frequency must at most equal to or " +
                        "less than the maximum", "minFreq");
                else
                    minfreq = value;

                // Anchors the minimum to an integer of the lacunarity
                // (This revised number will always be smaller than
                // the original)
                if (minfreq != maxfreq)
                {
                    double exp = Math.Log(minfreq) / Math.Log(lacunarity);
                    double anchored = Math.Pow(lacunarity, Math.Floor(exp));
                    if (anchored > 0)
                        minfreq = anchored;
                }

                // Updates the frequency sampling
                CalcFrequencies();
            }
        }
        private double minfreq;

        /// <summary>
        /// Maximum noise frequency (Must be 0 &lt; x)
        /// </summary>
        public double maxFreq
        {
            get { return maxfreq; }
            set
            {
                if (!(value > 0) || double.IsInfinity(value))
                    throw new ArgumentException(
                        "Frequency must be greater than 0", "maxFreq");
                else if (value < minFreq)
                    throw new ArgumentException(
                        "Maximum frequency must at least equal to or " +
                        "greater than the minimum", "maxFreq");
                else
                    maxfreq = value;

                // Updates the frequency sampling (Only if minfreq is set)
                if(minfreq > 0)
                    CalcFrequencies();
            }
        }
        private double maxfreq;

        /// <summary>
        /// The lacunarity, i.e. the texturing spacing between frequencies
        /// as described by Voss, "Random fractal forgeries" 1985 
        /// (Normally 0 &lt; 1/x &lt;= 1)
        /// </summary>
        public double Lacunarity
        {
            get { return lacunarity; }
            set
            {
                if (!(value > 1) || double.IsInfinity(value))
                    throw new ArgumentException(
                        "Lacunarity must be greater than 1", "Lacunarity");
                lacunarity = value;
                // Resets the mean frequencies
                minFreq = minfreq;

                // Updates the frequency sampling
                CalcFrequencies();
            }
        }
        private double lacunarity;

        /// <summary>
        /// Noise frequency sampling spectrum
        /// </summary>
        public double[] Frequency
        {
            get { return freqs; }
        }
        private double[] freqs;

        #endregion Fields & Properties
        #region Constructor

        /// <summary>
        /// Constructor for a basic noise object for accessing a noise function
        /// </summary>
        /// <param name="noiseFunc"> The noise function to access </param>
        /// <param name="minFreq"> Minimum frequency used in noise 
        /// creation </param>
        /// <param name="maxFreq"> Maximum frequency used in noise 
        /// creation </param>
        /// <param name="lacunarity"> The texture spacing between frequencies
        /// for each step up in frequency (Normally 0 &lt; x &lt;= 1) </param>
        /// <param name="shift"> Additional spatial offset applied to the 
        /// noise coordinates (Default is for three dimension.) </param>
        /// <param name="multiplier"> Factor multiplied to the noise 
        /// (Prior to offset) </param>
        /// <param name="offset"> Offset added to the noise (After 
        /// multiplication) </param>
        /// <param name="distFunc"> Distribution function (After all other
        /// steps) </param>
        public FrequencyNoise(NoiseFunction noiseFunc = null,
            double minFreq = 1, double maxFreq = 512,
            double lacunarity = 2.0, Vector shift = null, double multiplier = 1.0,
            double offset = 0.0, Func<double, double> distFunc = null) :
            base(noiseFunc, shift, multiplier, offset, distFunc)
        {
            if (!(lacunarity > 1) || double.IsInfinity(lacunarity))
                throw new ArgumentException(
                    "Lacunarity must be finite and greater than 1", "lacunarity");
            this.lacunarity = lacunarity;
            this.maxFreq = maxFreq;
            this.minFreq = minFreq;
        }

        /// <summary>
        /// Constructor for a basic noise object for accessing a noise function
        /// </summary>
        /// <param name="noiseFunc"> The noise function to access </param>
        /// <param name="minFreq"> Minimum frequency used in noise 
        /// creation </param>
        /// <param name="maxFreq"> Maximum frequency used in noise 
        /// creation </param>
        /// <param name="lacunarity"> The texture spacing between frequencies
        /// for each step up in frequency (Normally 0 &lt; x &lt;= 1) </param>
        /// <param name="noiseParams"> The noise parameters </param>
        public FrequencyNoise(NoiseFunction noiseFunc, double minFreq,
            double maxFreq, double lacunarity, NoiseParameters noiseParams) :
            this(noiseFunc, minFreq, maxFreq, lacunarity, noiseParams.shift,
                noiseParams.multiplier, noiseParams.offset, noiseParams.distFunc)
        {
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
            double[] noise = GetComponents(coord);
            double val = 0;
            for (int idx = 0; idx < noise.Length; idx++)
                val += noise[idx];

            return val;
        }

        /// <summary>
        /// Returns an array containing the component noise values  
        /// for the given coordinates
        /// </summary>
        /// <param name="coord"> Noise coordinates </param>
        /// <returns> Component noise array </returns>
        public virtual double[] GetComponents(Point coord)
        {
            Point ssCoord = coord.Clone();

            // Scales the noise coordinates to the minimum frequency
            for (int idx = 0; idx < ssCoord.Comp.Length; idx++)
                ssCoord.Comp[idx] *= minfreq;

            // Cycles through octaves
            double[] noises = new double[freqs.Length];
            for (int fidx = 0; fidx < freqs.Length; fidx++)
            {
                double inoise = noiseFunc.GetVal(ssCoord);

                // Records data
                noises[fidx] = inoise;

                // scales the coordinates for the next frequency
                for (int idx = 0; idx < ssCoord.Comp.Length; idx++)
                    ssCoord.Comp[idx] *= lacunarity;
            }

            return noises;
        }

        /// <summary>
        /// Returns a 2D array containing the component noise values  
        /// for the array of coordinate points
        /// </summary>
        /// <param name="coord"> Noise coordinates </param>
        /// <returns> Noise array with dim1=points, dim2=components </returns>
        public virtual double[,] GetComponents(Point[] coord)
        {
            double[,] vals = new double[coord.Length, Frequency.Length];

            // Each point
            for (int idx = 0; idx < coord.Length; idx++)
            {
                // Retrieves noise
                double[] val = GetComponents(coord[idx]);

                // Assigns noise values
                for(int fidx=0; fidx<val.Length;fidx++)
                    vals[idx,fidx] = val[fidx];
            }

            return vals;
        }

        /// <summary>
        /// Calculates the frequencies bound by minfreq, maxfreq, and lacunarity
        /// </summary>
        private void CalcFrequencies()
        {
            List<double> freqsList = new List<double>();
            for (double freq = minfreq; freq <= maxfreq;)
            {
                freqsList.Add(freq);
                double next = freq * lacunarity;
                if (next <= freq)
                    throw new ArgumentException("Lacunarity must advance the frequency sampling.");
                freq = next;
            }
            freqs = freqsList.ToArray();
        }
        
        /// <summary>
        /// Deep-copy (Non-referenced) clone
        /// </summary>
        /// <returns> Cloned copy </returns>
        new public FrequencyNoise Clone()
        {
            return CloneImp() as FrequencyNoise;
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
            FrequencyNoise newCopy = base.CloneImp() as FrequencyNoise;

            // Deep copy
            newCopy.freqs = (double[])freqs.Clone();

            return newCopy;
        }

        #endregion Methods
    }
}
