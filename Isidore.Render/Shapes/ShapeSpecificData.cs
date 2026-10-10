using Isidore.Maths;

namespace Isidore.Render
{
    /// <summary>
    /// Intersect data specific to Shapes
    /// </summary>
    public class ShapeSpecificData:BodySpecificData
    {
        #region Fields & Properties

        /// <summary>
        /// Surface Normal at intersection
        /// </summary>
        public Normal SurfaceNormal;

        /// <summary>
        /// Cosine of the incident angle (radians)
        /// </summary>
        public double CosIncAng;

        /// <summary>
        /// U barycentric coordinates at intersection
        /// </summary>
        public double U;

        /// <summary>
        /// V barycentric coordinates at intersection
        /// </summary>
        public double V;

        #endregion Fields & Properties
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="surfaceNormal"> Surface normal at the intersect 
        /// point </param>
        /// <param name="cosIncAng"> Cosine of the angle of incidence between
        /// the ray vector and surface normal at the intersect point </param>
        /// <param name="u"> U barycentric coordinate at the intersect 
        /// point </param>
        /// <param name="v"> V barycentric coordinates at the intersect 
        /// point </param>
        public ShapeSpecificData(Normal surfaceNormal = null, 
            double cosIncAng = double.NaN, double u = double.NaN,
            double v = double.NaN)
        {
            SurfaceNormal = surfaceNormal ?? Normal.NaN();
            CosIncAng = cosIncAng;
            U = u;
            V = v;
        }

        #endregion Constructors
        #region Methods

        /// <summary>
        /// Deep-copy (Non-referenced) clone
        /// </summary>
        /// <returns> Cloned copy </returns>
        new public ShapeSpecificData Clone()
        {
            return (ShapeSpecificData)CloneImp();
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
            ShapeSpecificData newCopy = (ShapeSpecificData)MemberwiseClone();

            // Deep copy
            DeepCopyOverride(ref newCopy);
            return newCopy;
        }

        /// <summary>
        /// Implements deep copies of members that would
        /// otherwise be shallow copied.
        /// </summary>
        /// <param name="copy"> Clone copy </param>
        protected virtual void DeepCopyOverride(ref ShapeSpecificData copy)
        {
            if (SurfaceNormal != null)
                copy.SurfaceNormal = SurfaceNormal.Clone();
        }

        #endregion Methods
    }
}
