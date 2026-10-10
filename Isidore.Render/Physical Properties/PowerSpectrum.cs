//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

namespace Isidore.Render
{
    /// <summary>
    /// A general power spectrum
    /// </summary>
    public class PowerSpectrum : Property
    {
        #region Fields & Properties

        /// <summary>
        /// Power spectrum
        /// </summary>
        public Spectrum<double, double> Value { get; set; }

        /// <summary>
        /// Spectrum frequency
        /// </summary>
        public double[] Frequency { get { return Value.Sample; } }

        /// <summary>
        /// Spectrum power
        /// </summary>
        public double[] Power { get { return Value.Value; } }

        #endregion Fields & Properties
        #region Constructors

        /// <summary>
        /// Constructor (Input is not cloned)
        /// </summary>
        /// <param name="powerSpectrum"> Power spectrum </param>
        /// <param name="units"> Power spectrum units (Sample, Value) </param>
        public PowerSpectrum(Spectrum<double, double> powerSpectrum,
            string units = "Hz, Magnitude (unitless)")
        {
            Value = powerSpectrum;
            Units = units;
        }

        /// <summary>
        /// Constructor using frequency and power arrays
        /// </summary>
        /// <param name="frequency"> Frequency of each value </param>
        /// <param name="power"> Power at each frequency </param>
        /// <param name="units"> Power spectrum  units (Sample, Value) </param>
        public PowerSpectrum(double[] frequency, double[] power,
            string units = "Hz, Magnitude (unitless)") :
            this(new Spectrum<double, double>(frequency, power), units)
        { }

        /// <summary>
        /// Constructor that references (points to) another 
        /// PowerSpectrum instance
        /// </summary>
        /// <param name="PowerSpectrum"> PowerSpectrum to point to </param>
        public PowerSpectrum(PowerSpectrum PowerSpectrum) :
            this(PowerSpectrum.Value, PowerSpectrum.Units)
        { }

        #endregion Constructors
        #region Methods

        /// <summary>
        /// Clones this instance by performing a deep copy
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        new public PowerSpectrum Clone()
        {
            return (PowerSpectrum)CloneImp();
        }

        /// <summary>
        /// Deep-copy clones this instance
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        protected override Property CloneImp()
        {
            // Shallow copies from base
            PowerSpectrum newCopy = (PowerSpectrum)base.CloneImp();

            // Deep-copies all data this is referenced by default
            if (Value != null)
                newCopy.Value = (Spectrum<double, double>)Value.Clone();

            return newCopy;
        }
        #endregion Methods
    }
}
