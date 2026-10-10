namespace Isidore.Render
{
    /// <summary>
    /// Single irradiance property for all wavelengths
    /// </summary>
    public class Irradiance : Property
    {
        /// <summary>
        /// Single value irradiance
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Irradiance constructor
        /// </summary>
        /// <param name="irradiance"> Irradiance value </param>
        /// <param name="units"> Irradiance data units</param>
        public Irradiance(double irradiance = 300, string units = "watts/cm^2")
        {
            Value = irradiance;
            Units = units;
        }

        /// <summary>
        /// Clones this instance by performing a deep copy
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        new public Irradiance Clone()
        {
            return (Irradiance)CloneImp();
        }

        /// <summary>
        /// Deep-copy clones this instance
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        protected override Property CloneImp()
        {
            Irradiance newCopy = (Irradiance)base.CloneImp();

            return newCopy;
        }
    }

    /// <summary>
    /// Linewidth irradiance spectrum 
    /// </summary>
    public class SpectralIrradiance : Irradiance
    {
        #region Fields & Properties

        /// <summary>
        /// Spectral irradiance
        /// </summary>
        new public Spectrum<double, double> Value { get; set; }

        /// <summary>
        /// Spectral reflectance wavelength sampling
        /// </summary>
        public double[] Wavelength { get { return Value.Sample; } }

        /// <summary>
        /// Spectral irradiance at every wavelength sample
        /// </summary>
        public double[] Irradiance { get { return Value.Value; } }

        #endregion Fields & Properties
        #region Constructors

        /// <summary>
        /// Constructor (Input is not cloned)
        /// </summary>
        /// <param name="irradiance"> spectral irradiance value </param>
        /// <param name="units"> spectral irradiance units (Sample, Value) </param>
        public SpectralIrradiance(Spectrum<double, double> irradiance = null, 
            string units = "meters, watts/cm^2")
        {
            Value = irradiance;
            Units = units;
        }

        /// <summary>
        /// Constructor using wavelength and reflectance ratio arrays
        /// </summary>
        /// <param name="wavelength"> Wavelength dependence of each irradiance value </param>
        /// <param name="irradiance"> Irradiance at each wavelength </param>
        /// <param name="units"> spectral irradiance units (Sample, Value) </param>
        public SpectralIrradiance(double[] wavelength, double[] irradiance,
            string units = "meters, watts/cm^2") 
        {
            Value = new Spectrum<double, double>
                (wavelength, irradiance);
            Units = units;
        }

        #endregion Constructors
        #region Methods

        /// <summary>
        /// Clones this instance by performing a deep copy
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        new public SpectralIrradiance Clone()
        {
            return (SpectralIrradiance)CloneImp();
        }

        /// <summary>
        /// Deep-copy clones this instance
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        protected override Property CloneImp()
        {
            // Shallow copies from base
            SpectralIrradiance newCopy = (SpectralIrradiance)base.CloneImp();

            // Deep-copies all data this is referenced by default
            if (Value != null)
                newCopy.Value = (Spectrum<double, double>)Value.Clone();

            return newCopy;
        }
        #endregion Methods
    }
}
