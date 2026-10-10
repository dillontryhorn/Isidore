//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

namespace Isidore.Render
{
    /// <summary>
    /// Refractive index spectrum property for a span of wavelengths.
    /// An index of 1.0 is vacuum propagation
    /// </summary>
    public class RefractiveIndex : Property
    {
        #region Fields & Properties

        /// <summary>
        /// Spectral refractive index
        /// </summary>
        public Spectrum<double, double> Value { get; set; }

        /// <summary>
        /// Spectral reflectance wavelength sampling
        /// </summary>
        public double[] Wavelength { get { return Value.Sample; } }

        /// <summary>
        /// Spectral refractive index at every wavelength sample
        /// </summary>
        public double[] Coefficient { get { return Value.Value; } }

        #endregion Fields & Properties
        #region Constructors

        /// <summary>
        /// Constructor (Input is not cloned)
        /// </summary>
        /// <param name="refractiveIndex"> Refractive index value </param>
        /// <param name="units"> Refractive index units (Sample, Value) </param>
        public RefractiveIndex(Spectrum<double, double> refractiveIndex,
            string units = "meters, unitless")
        {
            Value = refractiveIndex;
            Units = units;
        }

        /// <summary>
        /// Constructor using wavelength and reflectance ratio arrays
        /// </summary>
        /// <param name="wavelength"> Wavelength dependence of each irradiance value </param>
        /// <param name="reflectance"> Reflectance coefficient at each wavelength </param>
        /// <param name="units"> spectral refractive index units (Sample, Value) </param>
        public RefractiveIndex(double[] wavelength, double[] reflectance,
            string units = "meters, unitless") :
            this(new Spectrum<double, double>(wavelength, reflectance), units)
        { }

        /// <summary>
        /// Constructor that references (points) to another refractive index
        /// </summary>
        /// <param name="refractiveIndex"> Refractive index instance to reference </param>
        public RefractiveIndex(RefractiveIndex refractiveIndex) :
            this(refractiveIndex.Value, refractiveIndex.Units)
        { }

        /// <summary>
        /// Assigns a refractive index to a single wavelength.
        /// </summary>
        /// <param name="wavelength"> Wavelength of the refractive index</param>
        /// <param name="refractiveIndex"> Refractive Index coefficient value </param>
        public RefractiveIndex(double wavelength, double refractiveIndex) :
            this(new double[] { wavelength }, new double[] { refractiveIndex })
        { }

        /// <summary>
        /// Single value refractive index for all wavelengths.
        /// Default value is Vacuum (n=1.0)
        /// </summary>
        /// <param name="refractiveIndex"> Refractive index coefficient value </param>
        public RefractiveIndex(double refractiveIndex = 1.0) :
            this(new double[] { 0.0 }, new double[] { refractiveIndex })
        { }

        #endregion Constructors

        /// <summary>
        /// Clones this instance by performing a deep copy
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        new public RefractiveIndex Clone()
        {
            return (RefractiveIndex)CloneImp();
        }

        /// <summary>
        /// Deep-copy clones this instance
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        protected override Property CloneImp()
        {
            RefractiveIndex newCopy = (RefractiveIndex)base.CloneImp();

            // Deep Copies value
            newCopy.Value = Value.Clone() as Spectrum<double, double>;

            return newCopy;
        }
    }
}
