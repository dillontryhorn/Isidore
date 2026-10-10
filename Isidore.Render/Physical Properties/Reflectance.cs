using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

namespace Isidore.Render
{
    /// <summary>
    /// Single value reflectance property for all wavelengths.
    /// Reflectance should be used in wave optics and does not
    /// spawn children rays.
    /// </summary>
    public class Reflectance : Property
    {
        #region Fields & Properties

        /// <summary>
        /// Spectral reflectance
        /// </summary>
        public Spectrum<double, double> Value { get; set; }

        /// <summary>
        /// Spectral reflectance wavelength sampling
        /// </summary>
        public double[] Wavelength { get { return Value.Sample; } }

        /// <summary>
        /// Spectral reflectance at every wavelength sample
        /// </summary>
        public double[] Coefficient { get { return Value.Value; } }

        /// <summary>
        /// The mean spectral reflectance for all wavelengths
        /// </summary>
        public double MeanCoeff
        {
            get
            {
                // If the spectrum has just one value, returns it. Otherwise,
                // returns the mean of all coefficients
                if (Value.Value.Length == 1)
                    return Value.Value[0];
                else
                    return Maths.Stats.Mean(Value.Value);                
            }
        }

        #endregion Fields & Properties
        #region Constructors

        /// <summary>
        /// Constructor (Input is not cloned)
        /// </summary>
        /// <param name="reflectance"> spectral reflectance value </param>
        /// <param name="units"> spectral reflectance units (Sample, Value) </param>
        public Reflectance(Spectrum<double, double> reflectance,
            string units = "meters, unitless")
        {
            Value = reflectance;
            Units = units;
        }

        /// <summary>
        /// Constructor using wavelength and reflectance ratio arrays
        /// </summary>
        /// <param name="wavelength"> Wavelength dependence of each irradiance value </param>
        /// <param name="reflectance"> Reflectance coefficient at each wavelength </param>
        /// <param name="units"> spectral refractive index units (Sample, Value) </param>
        public Reflectance(double[] wavelength, double[] reflectance,
            string units = "meters, unitless") :
            this(new Spectrum<double, double>(wavelength, reflectance), units)
        { }

        /// <summary>
        /// Constructor that references (points) to another reflectance instance
        /// </summary>
        /// <param name="reflectance"> Spectral reflectance to point to </param>
        public Reflectance(Reflectance reflectance) :
            this(reflectance.Value, reflectance.Units)
        { }

        /// <summary>
        /// Single value reflectance for all wavelengths.
        /// </summary>
        /// <param name="reflectance"> Reflectance coefficient value </param>
        public Reflectance(double reflectance = 1.0 / Math.PI) :
            this(new double[] { 0 }, new double[] { reflectance })
        { }

        #endregion Constructors
        #region Methods

        /// <summary>
        /// Clones this instance by performing a deep copy
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        new public Reflectance Clone()
        {
            return (Reflectance)CloneImp();
        }

        /// <summary>
        /// Deep-copy clones this instance
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        protected override Property CloneImp()
        {
            // Shallow copies from base
            Reflectance newCopy = (Reflectance)base.CloneImp();

            // Deep-copies all data this is referenced by default
            if (Value != null)
                newCopy.Value = (Spectrum<double, double>)Value.Clone();

            return newCopy;
        }
        #endregion Methods
    }
}
