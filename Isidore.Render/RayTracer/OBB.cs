using System;
//using Isidore.Projectors;
using Isidore.Maths;

namespace Isidore.Render
{
    /// <summary>
    /// Represent an oriented bounding box (OBB).
    /// Note that this item cannot be spatially
    /// transformed.  Once created, it cannot be changed.
    /// This item is not a Body child but is an AABB child.
    /// </summary>
    public class OBB : AABB
    {
        // Real-time rendering: pg 729-730, 742-744

        #region Fields & Properties

        /// <summary>
        /// Normalized side directions of the box
        /// </summary>
        public Vector[] SideDir { get { return sideDir; } }
        /// <summary>
        /// Normalized side directions of the box
        /// </summary>
        protected Vector[] sideDir;

        #endregion Fields & Properties
        #region Constructors

        /// <summary>
        /// Constructor.  All inputs are references, not cloned.
        /// </summary>
        /// <param name="CenterPoint"> Box center point </param>
        /// <param name="SideDirections"> Box side Directions </param>
        /// <param name="SideLengths"> Lengths of each side </param>
        public OBB(Point CenterPoint = null, Vector[] SideDirections = null,
            double[] SideLengths = null):base(CenterPoint, SideLengths)
        {
            sideDir = SideDirections ?? new Vector[]{ Vector.Unit(3,0),
                Vector.Unit(3,1), Vector.Unit(3,2)};
        }

        #endregion Constructors
        #region Methods

        /// <summary>
        /// Performs an intersection test between a ray and this box.
        /// </summary>
        /// <param name="ray"> Ray to check for intersection </param>
        /// <returns> Tuple containing: 1) Intersection flag, 2) intersection
        /// point </returns>
        new public Tuple<bool, double[]> Intersect(Ray ray)
        {
            return Intersect(ray, centerPoint, sideDir, halfLength);
        }

        /// <summary>
        /// Performs an intersection test between a ray an a conventional
        /// OBB definition of a center point, side direction and half lengths
        /// </summary>
        /// <param name="ray"> Ray to intersect </param>
        /// <param name="centerPoint"> Box center point </param>
        /// <param name="sideDir"> Box side direction </param>
        /// <param name="halfLength"> Box half length in each axis </param>
        /// <returns> Tuple containing: 1) Intersection flag, 2) intersection
        /// point </returns>
        public static Tuple<bool, double[]> Intersect(Ray ray, 
            Point centerPoint, Vector[] sideDir, double[] halfLength)
        {
            double[] t = new double[] { double.NegativeInfinity,
                double.PositiveInfinity };

            Point dPoint = centerPoint - ray.Origin;

            // For each voxel
            for (int idx = 0; idx < ray.Origin.Comp.Length; idx++)
            {
                // Orientation relevant data
                double dotPoint = sideDir[idx].Dot((Vector)dPoint);
                double dotDir = sideDir[idx].Dot(ray.Dir);
                
                if(!InterAxis(centerPoint.Comp[idx], halfLength[idx], 
                    dotPoint, dotDir, ref t))
                    return new Tuple<bool, double[]>(false, t);
            }

            return new Tuple<bool, double[]>(true, t);
        }



        /// <summary>
        /// Clones this instance by performing a deep copy
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        new public OBB Clone()
        {
            return (OBB)CloneImp();
        }

        /// <summary>
        /// Deep-copy clones this instance
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        protected override Item CloneImp()
        {
            OBB newCopy = (OBB)MemberwiseClone();

            // Deep copy
            DeepCopyOverride(ref newCopy);

            newCopy.AdvanceToTime(CurrentTime, true);

            return newCopy;
        }

        /// <summary>
        /// Implements deep copies of members that would
        /// otherwise be shallow copied.
        /// </summary>
        /// <param name="copy"> Clone copy </param>
        protected void DeepCopyOverride(ref OBB copy)
        {
            // Base copy
            AABB baseCast = copy; // This is a shallow copy
            DeepCopyOverride(ref baseCast);

            // private Vector[] sidesDir;
            copy.sideDir = (Vector[])sideDir.Clone();
            for (int idx = 0; idx < sideDir.Length; idx++)
                copy.sideDir[idx] = sideDir[idx].Clone();
        }

        #endregion Methods
    }
}
