using Isidore.Maths;

namespace Isidore.Render
{
    /// <summary>
    /// IncidentData is a class used for recording ray/object intersection information.
    /// </summary>
    public class IncidentData
    {
        # region Fields & Properties

        ///// <summary>
        ///// Surfaces intersected
        ///// </summary>
        //public Surfaces BlockSurfaces;

        /// <summary>
        /// Reports if the source has line-of-sight with the intersection 
        /// point
        /// </summary>
        public bool LOS;

        /// <summary>
        /// Travel distance to intersection point
        /// </summary>
        public double Travel;

        /// <summary>
        /// Source/Intersect point propagation vector
        /// </summary>
        public Vector PropVec;

        /// <summary>
        /// Reference to the incident source
        /// </summary>
        public Source Source;

        /// <summary>
        /// Reference to the shape intersected
        /// </summary>
        public Properties Properties = new Properties();

        #endregion Fields & Properties
        #region Constructors

        /// <summary>
        /// Initializes incident data for a ray/source intersection.
        /// </summary>
        /// <param name="los">
        /// Indicates whether the source has line of sight to the intersection.
        /// </param>
        /// <param name="travel">Travel distance to the intersection.</param>
        /// <param name="propVec">
        /// Propagation vector from the source to the intersection.
        /// </param>
        /// <param name="source">Source associated with the incident data.</param>
        public IncidentData(bool los = false,
            double travel = double.PositiveInfinity,
            Vector propVec = null, Source source = null)
        {
            LOS = los;
            Travel = travel;
            PropVec = propVec ?? Vector.Unit(3, 2);
            Source = source;
        }

        # endregion Constructors
        # region Methods

        //public void Transform(Matrix4x4 m)
        //{
        //    IncidentVec.Transform(m);
        //}

        # endregion Methods
    }
}
