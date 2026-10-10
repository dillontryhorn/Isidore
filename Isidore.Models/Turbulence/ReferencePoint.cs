using System.Collections.Generic;
using Isidore.Maths;

namespace Isidore.Models
{
    /// <summary>
    /// ReferencePoint is a point that contains a list of references
    /// to other points.  It is useful in physical applications
    /// </summary>
    public class ReferencePoint : Point
    {
        /// <summary>
        /// The category description this point belongs to.
        /// This can be used by other classes as a processing flag
        /// </summary>
        public enum Category
        {
            /// <summary>
            /// Default category label
            /// </summary>
            Default,

            /// <summary>
            /// Interpolation category label
            /// </summary>
            Interpolation
        }

        #region Fields & Properties

        /// <summary>
        /// Points referenced by this point
        /// </summary>
        public List<Point> ReferencePoints { get; set; }

        /// <summary>
        /// The name of the category this point belongs to
        /// </summary>
        public Category CategoryLabel { get; set; }

        #endregion Fields & Properties
        #region Constructors

        /// <summary>
        /// Class constructor.  If left empty a 3D point at the origin is 
        /// created.
        /// </summary>
        /// <param name="position"> Point position </param>
        /// <param name="referencePoints"> Points referenced by this 
        /// <param name="categoryLabel"> Category this point belongs to </param>
        /// point </param>
        public ReferencePoint(double[] position = null,
            List<Point> referencePoints = null, 
            Category categoryLabel = Category.Default) : base(position)
        {
            ReferencePoints = referencePoints ?? new List<Point>();
            CategoryLabel = categoryLabel;
        }

        /// <summary>
        /// Constructor of a reference point in three dimensional space
        /// </summary>
        /// <param name="p0"> First component position </param>
        /// <param name="p1"> Second component position </param>
        /// <param name="p2"> Third component position </param>
        /// <param name="referencePoints"> Points referenced by this 
        /// point </param>
        public ReferencePoint(double p0, double p1, double p2,
            List<Point> referencePoints = null) :
            base(new double[] { p0, p1, p2 })
        {
            ReferencePoints = referencePoints ?? new List<Point>();
        }

        /// <summary>
        /// Copy Constructor
        /// </summary>
        /// <param name="pt0"> Point to copy </param>
        /// <param name="referencePoints"> Points referenced by this 
        /// point </param>
        public ReferencePoint(Point pt0,
            List<Point> referencePoints = null) : base(pt0.Comp)
        {
            ReferencePoints = referencePoints ?? new List<Point>();
        }

        /// <summary>
        /// Constructor for a point located at the origin of N dimensional 
        /// space
        /// </summary>
        /// <param name="N"> Number of dimensions in which the point 
        /// exists </param>
        /// <param name="referencePoints"> Points referenced by this 
        /// point </param>
        public ReferencePoint(int N, List<Point> referencePoints = null) :
            base(N)
        {
            ReferencePoints = referencePoints ?? new List<Point>();
        }

        #endregion Constructors
        #region Methods

        /// <summary>
        /// Clones this point's coordinates and reference list. The referenced
        /// points remain shared so the reference graph is preserved.
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        new public ReferencePoint Clone()
        {
            return CloneImp() as ReferencePoint;
        }

        /// <summary>
        /// Copies the point's coordinates and reference list while preserving
        /// its runtime type and references to other points.
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        protected Point CloneImp()
        {
            ReferencePoint newCopy = (ReferencePoint)MemberwiseClone();
            newCopy.Comp = (double[])Comp.Clone();
            newCopy.ReferencePoints = new List<Point>(ReferencePoints);

            return newCopy;
        }

        #endregion Methods
    }
}
