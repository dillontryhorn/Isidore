namespace Isidore.Render
{
    /// <summary>
    /// Temperature property
    /// </summary>
    public class Temperature : Property
    {
        /// <summary>
        /// Single value temperature
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Temperature constructor
        /// </summary>
        /// <param name="temperature"> Temperature value </param>
        /// <param name="units"> Temperature data units</param>
        public Temperature(double temperature = 300, string units = "kelvin")
        {
            Value = temperature;
            Units = units;
        }

        /// <summary>
        /// Clones this instance by performing a deep copy
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        new public Temperature Clone()
        {
            return (Temperature)CloneImp();
        }

        /// <summary>
        /// Deep-copy clones this instance
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        protected override Property CloneImp()
        {
            Temperature newCopy = (Temperature)base.CloneImp();

            return newCopy;
        }
    }
}
