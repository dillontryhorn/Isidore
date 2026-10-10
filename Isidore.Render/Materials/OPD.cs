using System;
using Isidore.Maths;

namespace Isidore.Render
{
    /// <summary>
    /// OPD provides an optical path difference material property.
    /// It returns a length property object.
    /// </summary>
    public class OPD : Material
    {
        #region Fields & Properties

        /// <summary>
        /// Length added by this optical path length
        /// </summary>
        public Length Length { get; set; }

        /// <summary>
        /// A scalar map for determining how to scale the length.
        /// This can be used as a stencil.
        /// </summary>
        public Texture ScaleTexture = null;

        #endregion Fields & Properties
        #region Constructors

        /// <summary>
        /// Optical path difference constructor
        /// </summary>
        /// <param name="length"> Path difference </param>
        /// <param name="scaleFactor"> Scale factor texture </param>
        public OPD(Length length, Texture scaleFactor = null)
        {
            Length = length;
            ScaleTexture = scaleFactor;
        }

        /// <summary>
        /// Single value Lambertian reflector
        /// </summary>
        /// <param name="opd"> Single optical path difference </param>
        /// /// <param name="scaleFactor"> Scale factor texture </param>
        public OPD(double opd, Texture scaleFactor = null) : 
            this(new Length(opd), scaleFactor)
        { }

        #endregion Constructor
        #region Methods

        /// <summary>
        /// Finds the reflectance for the angle of incidence provided
        /// by the intersection data created from a ray/shape intersection
        /// and the wavelength property attached to the ray.  
        /// If there is no wavelength property, this material will not 
        /// interact.  This material will only interact with Shape subclasses.
        /// </summary>
        /// <param name="ray"> ray used for generating the intersect data
        /// instance</param>
        /// <returns> Indicates material interaction  </returns>
        override public bool ProcessIntersectData(ref RenderRay ray)
        {
            // Checks that the specific data class is a Shape subclass
            ShapeSpecificData sData = ray.IntersectData.BodySpecificData as 
                ShapeSpecificData;
            // If not, "as" will return a null, so this returns unaltered
            if (sData == null)
                return false;

            // This saves some typing
            IntersectData iData = ray.IntersectData;

            double length = Length.Value;

            // Texture scaled reflectance array
            if (ScaleTexture != null)
            {
                double scaleFac = ScaleTexture.GetVal(sData.U, sData.V);
                length *= scaleFac;
            }

            // Creates a length property and adds it to the ray
            Length thisLen = new Length(length, Length.Units);
            ray.IntersectData.Properties.Add(thisLen);

            // returns interaction notification
            return true;
        }

        /// <summary>
        /// Clones this instance by performing a deep copy
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        new public OPD Clone()
        {
            return CloneImp() as OPD;
        }

        /// <summary>
        /// Deep-copy clones this instance
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        protected override Material CloneImp()
        {
            // Shallow copies from base
            OPD newCopy = base.CloneImp() as OPD;

            // Deep-copies all data this is referenced by default
            if (Length != null)
                newCopy.Length = Length.Clone();

            if (ScaleTexture != null)
                newCopy.ScaleTexture = ScaleTexture.Clone();

            return newCopy;
        }
        #endregion Methods
    }
}
