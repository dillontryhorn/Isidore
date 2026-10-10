using System;
using Isidore.Maths;

namespace Isidore.Render
{
    /// <summary>
    /// Reflector provides mono-static gray-body reflector material property
    /// where each wavelength has a specific constant.  Reflectances are
    /// interpolated if the spectral and ray wavelengths aren't equivalent
    /// </summary>
    public class Reflector : Material
    {
        #region Fields & Properties

        /// <summary>
        /// Reflectance of this reflector material.
        /// </summary>
        public Reflectance Reflectance { get; set; }

        /// <summary>
        /// A scalar map for determining how to scale the reflectance.
        /// This is useful as a stencil.
        /// </summary>
        public Texture Scalar = null;


        #endregion Fields & Properties
        #region Constructors

        /// <summary>
        /// Lambertian reflector constructor.  Reflectance is referenced
        /// (Not cloned)
        /// </summary>
        /// <param name="reflectance"> Reflectance spectrum </param>
        public Reflector(Reflectance reflectance)
        {
            Reflectance = reflectance;
        }

        /// <summary>
        /// Single value Lambertian reflector
        /// </summary>
        /// <param name="reflectance"> Single reflectance coefficient </param>
        public Reflector(double reflectance = 1.0 / Math.PI) :
            this(new Reflectance(reflectance))
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

            // Extracts wavelengths from properties list (if present)
            double[] wlens = new double[] { 0.0 };
            for (int idx = 0; idx < ray.Properties.Count; idx++)
                if (ray.Properties[idx].GetType() == typeof(Wavelength))
                {
                    Wavelength thisWlen = (Wavelength)ray.Properties[idx];
                    wlens = thisWlen.Value;
                }

            // Checks that the cosine of incidence is positive

            // Interpolated reflectance array
            double[] reflect = Interpolate.Linear(wlens, Reflectance.Wavelength,
                Reflectance.Coefficient);
            double cosIncAng = sData.CosIncAng;
            double[] reflectance = Operator.Multiply(cosIncAng, reflect);

            // Texture scaled reflectance array
            if (Scalar != null)
            {
                double scaleFac = Scalar.GetVal(sData.U, sData.V);
                reflectance = Operator.Multiply(scaleFac, reflectance);
            }

            // Creates a reflectance property and adds it to the ray
            Reflectance thisReflect = new Reflectance(wlens, reflectance, 
                Reflectance.Units);
            ray.IntersectData.Properties.Add(thisReflect);

            // returns interaction notification
            return true;
        }

        ///// <summary>
        ///// Scales the reflectance by the value extracted by the Scalar member
        ///// </summary>
        ///// <param name="reflectMat"> Reflection map </param>
        //public void ScaleData(ref Material reflectMat)
        //{
        //    // Scale factor
        //    throw new NotImplementedException();
        //    //double scale = Scalar.GetVal(ray.IntersectData.UV);
        //}

        /// <summary>
        /// Clones this instance by performing a deep copy
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        new public Reflector Clone()
        {
            return (Reflector)CloneImp();
        }

        /// <summary>
        /// Deep-copy clones this instance
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        protected override Material CloneImp()
        {
            // Shallow copies from base
            Reflector newCopy = (Reflector)base.CloneImp();

            // Deep-copies all data this is referenced by default
            if (Reflectance != null)
                newCopy.Reflectance = Reflectance.Clone();

            // public Texture<double> Scalar = null;
            if (Scalar != null)
                newCopy.Scalar = Scalar.Clone();

            return newCopy;
        }
        #endregion Methods
    }
}
