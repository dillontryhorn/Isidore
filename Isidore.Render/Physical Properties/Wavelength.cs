namespace Isidore.Render
{
    /// <summary>
    /// Wavelength property class
    /// </summary>
    public class Wavelength : Property
    {
        #region Fields & Properties

        /// <summary>
        /// All represented wavelengths
        /// </summary>
        public double[] Value { get; set; }

        #endregion Fields & Properties
        #region Constructors

        /// <summary>
        /// Wavelength constructor. The input array is referenced.
        /// </summary>
        /// <param name="wavelength"> Spectral wavelength value array </param>
        /// <param name="units"> Wavelength units </param>
        public Wavelength(double[] wavelength, string units = "meters")
        {
            Value = wavelength;
            Units = units;
        }

        /// <summary>
        /// Wavelength constructor using a single value
        /// </summary>
        /// <param name="wavelength"> Single wavelength to assign </param>
        /// <param name="units"> Wavelength units </param>
        public Wavelength(double wavelength = 1.0e-6, string units = "meters") :
            this(new double[] { wavelength }, units)
        { }

        /// <summary>
        /// Wavelength constructor that references another wavelength instance
        /// </summary>
        /// <param name="wavelength"> Wavelength instance </param>
        public Wavelength(Wavelength wavelength) :
            this(wavelength.Value, wavelength.Units)
        { }


        #endregion Constructor
        #region Methods

        /// <summary>
        /// Clones this instance by performing a deep copy
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        new public Wavelength Clone()
        {
            return (Wavelength)CloneImp();
        }

        /// <summary>
        /// Deep-copy clones this instance
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        protected override Property CloneImp()
        {
            // Shallow copies from base
            Wavelength newCopy = (Wavelength)base.CloneImp();

            // Deep-copies all data that is referenced by default
            if(Value != null)
            newCopy.Value = (double[])Value.Clone();

            return newCopy;
        }

        #endregion Methods
    }
}
