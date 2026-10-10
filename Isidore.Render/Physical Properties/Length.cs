namespace Isidore.Render
{
    /// <summary>
    /// Length property
    /// </summary>
    public class Length : Property
    {
        /// <summary>
        /// Single value temperature
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Length constructor
        /// </summary>
        /// <param name="length"> Length value </param>
        /// <param name="units"> Length units </param>
        public Length(double length = 1, string units = "meters")
        {
            Value = length;
            Units = units;
        }

        /// <summary>
        /// Clones this instance by performing a deep copy
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        new public Length Clone()
        {
            return CloneImp() as Length;
        }

        /// <summary>
        /// Deep-copy clones this instance
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        protected override Property CloneImp()
        {
            Length newCopy = base.CloneImp() as Length;

            return newCopy;
        }
    }
}
