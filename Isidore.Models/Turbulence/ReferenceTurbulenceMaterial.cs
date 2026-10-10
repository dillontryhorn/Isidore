using System.Collections.Generic;
using Isidore.Maths;
using Isidore.Render;

namespace Isidore.Models
{


    /// <summary>
    /// ProceduralTurbulence is a material that supports ReferenceTurbulence
    /// </summary>
    public class ReferenceTurbulenceMaterial : Material
    {
        #region Fields & Properties

        /// <summary>
        /// Reference point turbulence 
        /// </summary>
        public ReferencePointTurbulence ReferenceTurbulence { get; set; }


        #endregion Fields & Properties
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="referenceTurbulence"> Reference point 
        /// turbulence instance </param>
        public ReferenceTurbulenceMaterial(
            ReferencePointTurbulence referenceTurbulence)
        {
            ReferenceTurbulence = referenceTurbulence;
        }

        #endregion Constructors
        #region Methods

        /// <summary>
        /// Determines how the intersection data is altered by the material
        /// </summary>
        /// <param name="ray"> Render ray instance </param>
        /// <returns> Indicates material interaction  </returns>
        public override bool ProcessIntersectData(ref RenderRay ray)
        {
            // Retrieves the corresponding noise value
            double now = ray.Time;
            Point pt = ray.IntersectData.IntersectPt;
            double val = ReferenceTurbulence.GetVal(pt, now);

            // Converts the noise value into a scalar and adds it to the 
            // property array
            Scalar thisVal = new Scalar(val);
            ray.IntersectData.Properties.Add(thisVal);

            // returns interaction notification
            return true;
        }

        /// <summary>
        /// Clones this instance by performing a deep copy
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        new public ReferenceTurbulenceMaterial Clone()
        {
            return (ReferenceTurbulenceMaterial)CloneImp();
        }

        /// <summary>
        /// Deep-copy clones this instance
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        protected override Material CloneImp()
        {
            // Shallow copies from base
            ReferenceTurbulenceMaterial newCopy = (ReferenceTurbulenceMaterial)base.CloneImp();

            // Can't really deep copy these turbulence cells yet
            newCopy.ReferenceTurbulence = ReferenceTurbulence;

            return newCopy;
        }

        #endregion Methods
    }
}
