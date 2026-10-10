namespace Isidore.Render
{
    /// <summary>
    /// Intersect data specific to Volumetrics
    /// </summary>
    public class VolumeSpecificData:BodySpecificData
    {
        #region Fields & Properties

        /// <summary>
        /// Indices of 
        /// </summary>
        public int[] InIndex;

        /// <summary>
        /// Indices of volumetric instance intersection
        /// </summary>
        public int[] IntersectIndex;

        #endregion Fields & Properties
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="inIndex"> Indices of volumetric instance the ray is
        /// propagating through </param>
        /// <param name="intersectIndex"> Indices of volumetric instance the
        /// ray is intersecting </param>
        public VolumeSpecificData(int[] inIndex = null, 
            int[] intersectIndex = null)
        {
            InIndex = inIndex;
            IntersectIndex = intersectIndex;
        }

        ///// <summary>
        ///// Constructor
        ///// </summary>
        ///// <param name="volumeTravel"> Travel distance through the 
        ///// volumetric </param>
        ///// <param name="farIntersectPt"> Intersect point on the volumetric's
        ///// far boundary </param>
        ///// <param name="index"> Indices of volumetric instance 
        ///// intersection </param>
        //public VolumeSpecificData(double volumeTravel =
        //    double.PositiveInfinity, Point farIntersectPt = null,
        //    int[] index = null)
        //{
        //    VolumeTravel = volumeTravel;
        //    FarIntersectPt = farIntersectPt ?? Point.NaN();
        //    Index = index;
        //}

        #endregion Constructors
        #region Methods
        /// <summary>
        /// Deep-copy (Non-referenced) clone
        /// </summary>
        /// <returns> Cloned copy </returns>
        new public VolumeSpecificData Clone()
        {
            return (VolumeSpecificData)CloneImp();
        }

        /// <summary>
        /// Clone implementation. Uses MemberwiseClone to clone, and 
        /// inheriting classes will implement the cloning of
        /// specific data types 
        /// </summary>
        /// <returns> Clone copy </returns>
        protected override BodySpecificData CloneImp()
        {
            // Shallow copy
            VolumeSpecificData newCopy = (VolumeSpecificData)MemberwiseClone();

            // Deep copy
            DeepCopyOverride(ref newCopy);

            return newCopy;
        }

        /// <summary>
        /// Implements deep copies of members that would
        /// otherwise be shallow copied.
        /// </summary>
        /// <param name="copy"> Clone copy </param>
        protected virtual void DeepCopyOverride(ref VolumeSpecificData copy)
        {

            //if (FarIntersectPt != null)
            //    copy.FarIntersectPt = FarIntersectPt.Clone();

            if (InIndex != null)
                copy.InIndex = (int[])InIndex.Clone();
            if (IntersectIndex != null)
                copy.IntersectIndex = (int[])IntersectIndex.Clone();
        }

        #endregion Methods
    }
}
