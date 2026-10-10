using System;
using Isidore.Maths;

namespace Isidore.Render
{
    /// <summary>
    /// Reflective provides a completely specular reflective surface.
    /// Reflected and refracted vectors are calculated using Whitted's method.
    /// If an SpectralIrradiance instance is in the ray's properties, the 
    /// reflected irradiance is scaled by the Reflective's Reflectance.
    /// </summary>
    public class Reflective : Material
    {
        #region Fields & Properties

        /// <summary>
        /// speed of light (need to create a physical constant feature in Maths)
        /// </summary>
        private double c = 299792458;

        /// <summary>
        /// Reflectance of the reflecting material
        /// </summary>
        public Reflectance Reflectance { get; set; }

        #endregion Fields & Properties
        #region Constructors

        /// <summary>
        /// Reflectance material constructor.  Reflectance is referenced
        /// (Not cloned).
        /// </summary>
        /// <param name="reflectance"> Reflectance spectrum </param>
        public Reflective(Reflectance reflectance)
        {
            Reflectance = reflectance;
        }

        /// <summary>
        /// Single reflectance medium constructor.
        /// </summary>
        /// <param name="R"> Reflectance coefficient </param>
        public Reflective(double R = 1.0) : this(new Reflectance(R)) { }

        #endregion Constructor
        #region Methods

        /// <summary>
        /// Overrides the Material abstract class to applies this material's
        /// effect on the ray.  Returns a boolean if the interaction was
        /// successful.
        /// </summary>
        /// <param name="ray"> Ray intersecting the material </param>
        /// <returns> Indicator for successful ray/material interaction </returns>
        public override bool ProcessIntersectData(ref RenderRay ray)
        {
            // Checks that the specific data class is a Shape subclass
            ShapeSpecificData sData = ray.IntersectData.BodySpecificData as
                ShapeSpecificData;
            // If not, "as" will return a null, so this returns unaltered
            if (sData == null)
                return false;

            // Updates the properties to include effects of the material
            // Properties Clones
            Properties reflectProp = ray.Properties.Clone();

            // Tags important properties for the ray's Properties list
            int nTag = -1; // Tags the refractive index location
            int iTag = -1; // Tags the irradiance
            int siTag = -1; // Tags the spectral irradiance
            // Cycles through Properties list
            for (int idx = 0; idx < reflectProp.Count; idx++)
            {
                Type thisType = reflectProp[idx].GetType();
                if (thisType == typeof(RefractiveIndex))
                    nTag = idx;
                else if (thisType == typeof(Irradiance))
                    iTag = idx;
                else if (thisType == typeof(SpectralIrradiance))
                    siTag = idx;
            }

            // Calculates the refractive index (For speed)
            double n = 1.0;
            if (nTag > -1)
            { 
                RefractiveIndex N = (RefractiveIndex)ray.Properties[nTag];
                n = Stats.Mean(N.Coefficient);
            }

            // Scales the irradiance by the reflectance
            if (iTag > -1)
            {
                // Deep copies the irradiance
                Irradiance I = (Irradiance)reflectProp[iTag];
                // Scales the reflected irradiance
                I.Value *= Reflectance.MeanCoeff;
                // Replaces the Irradiance
                reflectProp.RemoveAt(iTag);
                reflectProp.Insert(iTag, I);
            }

            // Scxales the spectral irradiance by the reflectance
            if (siTag > -1)
            {
                // Deep copies the spectral irradiance
                SpectralIrradiance sI = (SpectralIrradiance)reflectProp[siTag];
                // Scales the reflected irradiance
                double[] scale = Interpolate.Linear(sI.Wavelength,
                    Reflectance.Wavelength, Reflectance.Coefficient);
                for (int idx = 0; idx < scale.Length; idx++)
                    sI.Value.Value[idx] *= scale[idx];
                // Replaces the Irradiance
                reflectProp.RemoveAt(siTag);
                reflectProp.Insert(siTag, sI);
            }


            // Calculates the Reflection vector
            Normal dNorm = sData.SurfaceNormal;
            Vector surfNorm = dNorm;

            // Reflection vector
            Vector rVec = Reflection(ray.Dir, surfNorm);

            // Reflected ray's origin time
            double startTime = ray.Time + n * ray.IntersectData.Travel / c;

            // Makes the reflected ray
            RenderRay reflectRay = new RenderRay(ray.IntersectData.IntersectPt,
                rVec, startTime, ray.Rank + 1, RayType.Reflected,
                reflectProp);

            // Adds the render ray as the parent of the casted rays
            reflectRay.ParentRay = ray;

            // Adds new reflected ray
            ray.IntersectData.CastedRays.Add(reflectRay);

            // returns interaction notification
            return true;
        }

        /// <summary>
        /// Calculates the reflection propagation vectors using
        /// a modified Heckbert method.
        /// </summary>
        /// <param name="dir"> Ray direction </param>
        /// <param name="norm"> Surface normal </param>
        /// <returns> Reflected propagation vectors </returns>
        public static Vector Reflection(Vector dir, Vector norm)
        {
            Vector reflect = dir - 2 * norm.Dot(dir) * norm;

            return reflect;
        }

        /// <summary>
        /// Clones this instance by performing a deep copy
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        new public Reflective Clone()
        {
            return (Reflective)CloneImp();
        }

        /// <summary>
        /// Deep-copy clones this instance
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        protected override Material CloneImp()
        {
            // Shallow copies from base
            Reflective newCopy = (Reflective)base.CloneImp();

            // Deep-copies all data this is referenced by default
            if (Reflectance != null)
                newCopy.Reflectance = Reflectance.Clone();

            return newCopy;
        }
        #endregion Methods
    }
}
