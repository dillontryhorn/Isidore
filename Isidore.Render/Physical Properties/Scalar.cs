namespace Isidore.Render
{
    /// <summary>
    /// The Scalar property is a development tool that returns a single scalar
    /// value and does not present a physical property. Hence it is scalar in
    /// a computing sense and not a physics one
    /// </summary>
    public class Scalar : Property
    {
        /// <summary>
        /// Single value temperature
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Scalar constructor
        /// </summary>
        /// <param name="value"> scalar value </param>
        public Scalar(double value = 0)
        {
            Value = value;
            Units = "none";
        }

        /// <summary>
        /// Clones this instance by performing a deep copy
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        new public Scalar Clone()
        {
            return (Scalar)CloneImp();
        }

        /// <summary>
        /// Deep-copy clones this instance
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        protected override Property CloneImp()
        {
            Scalar newCopy = (Scalar)base.CloneImp();

            return newCopy;
        }
    }
}
