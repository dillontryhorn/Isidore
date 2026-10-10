using System;
using Isidore.Maths;

namespace Isidore.Render
{
    /// <summary>
    /// Transparency provides an transparent bulk material using
    /// the RefractiveIndex physical property.  Refractive indices are
    /// interpolated if the spectral and ray wavelengths aren't equivalent.
    /// Reflected and refracted vectors are calculated using Whitted's method.
    /// </summary>
    public class Transparency: Material
    {
        #region Fields & Properties

        /// <summary>
        /// speed of light (need to create a physical constant feature in Maths)
        /// </summary>
        private double c = 299792458;

        /// <summary>
        /// Refractive indices of this transparent material.
        /// </summary>
        public RefractiveIndex RefractiveIndex { get; set; }

        /// <summary>
        /// Flag to cast a reflected ray in addition to the transmitted ray
        /// </summary>
        public bool CastReflectedRays { get; set; }

        #endregion Fields & Properties
        #region Constructors

        /// <summary>
        /// Transparent media constructor.  Refractive index is referenced
        /// (Not cloned).
        /// </summary>
        /// <param name="refractiveIndex"> Refractive index spectrum </param>
        public Transparency(RefractiveIndex refractiveIndex)
        {
            RefractiveIndex = refractiveIndex;
        }

        /// <summary>
        /// Single refractive index transparent media constructor.
        /// </summary>
        /// <param name="n"> Refractive index coefficient </param>
        public Transparency(double n = 1.52) : this(new RefractiveIndex(n)) { }

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

            // This saves some typing
            IntersectData iData = ray.IntersectData;

            // Tags the refractive index and wavelength from properties
            // Both will be replaced in the refractive rays
            int nTag = -1; // Tags the refractive index location
            int wTag = -1; // Tags the wavelength location
            // Cycles through Properties list
            for (int idx = 0; idx < ray.Properties.Count; idx++)
                if (ray.Properties[idx].GetType() == typeof(RefractiveIndex))
                    nTag = idx;
                else if(ray.Properties[idx].GetType() == typeof(Wavelength))
                    wTag = idx;

            // Constructs the n1 refractive index 
            RefractiveIndex N1 = new RefractiveIndex();
            if (nTag > -1)
            {
                N1 = (RefractiveIndex)ray.Properties[nTag];
                if (wTag > -1)
                {
                    Wavelength thisWlen = (Wavelength)ray.Properties[wTag];
                    N1 = new RefractiveIndex(thisWlen.Value,
                        Interpolate.Linear(thisWlen.Value, N1.Wavelength,
                            N1.Coefficient), N1.Units);
                }
            }
            else if (wTag > -1)
            {
                Wavelength thisWlen = (Wavelength)ray.Properties[wTag];
                N1 = new RefractiveIndex(thisWlen.Value, 
                    Maths.Distribution.Uniform(thisWlen.Value.Length, 1.0));
            }

            // Extracts the wavelengths and refractive indices
            double[] wlens = N1.Wavelength;
            double[] n1 = N1.Coefficient;

            // n2 material refractive index for every wavelength entry
            double[] n2 = Interpolate.Linear(wlens, RefractiveIndex.Wavelength,
                RefractiveIndex.Coefficient);

            // Adds cast rays to Render
            for (int idx = 0; idx < n1.Length; idx++)
            {
                // Updates the properties to include effects of the material
                // Properties Clones
                Properties reflectProp = ray.Properties.Clone();
                Properties transmitProp = ray.Properties.Clone();

                // Transmitted refractive index  for this wave
                RefractiveIndex N2 = new RefractiveIndex(wlens[idx], n2[idx]);

                // Wavelength being addressed
                Wavelength wavelength = new Wavelength(wlens[idx]);

                // Remove original spectrum properties in descending index
                // order so removing one does not shift the other index.
                for (int pidx = ray.Properties.Count - 1; pidx >= 0; pidx--)
                {
                    if (pidx == nTag || pidx == wTag)
                    {
                        transmitProp.RemoveAt(pidx);
                        reflectProp.RemoveAt(pidx);
                    }
                }

                // If there is a wavelength, removes it from the list 
                // and adds a discrete wavelength
                if(wTag > -1)
                {
                    // Adds this discrete wavelength
                    transmitProp.Add(wavelength);
                    reflectProp.Add(wavelength);
                }

                // Determines what material the ray is exiting/entering based
                // on the cosine incidence angle
                double mat1, mat2;
                Vector surfNorm;
                //double cosIncAng = sData.CosIncAng;
                Normal dNorm = sData.SurfaceNormal;
                Vector rDir = ray.Dir;

                // Can't use the surface data cosIncAng since it's
                // flipped of back face intersections
                double cosIncAng = rDir.Dot(-1.0 * dNorm);

                if (cosIncAng < 0)
                {
                    // Used for calculating transmission/reflection vectors
                    mat1 = n2[idx];
                    mat2 = 1;
                    // Adds in the refractive indices for each ray
                    transmitProp.Add(new RefractiveIndex(wlens[idx], 1.0));
                    reflectProp.Add(N2);
                    // Flips normal
                    surfNorm = -1.0 * dNorm;
                }
                else
                {
                    // Used for calculating transmission/reflection vectors
                    mat1 = n1[idx];
                    mat2 = n2[idx];
                    // Adds in the refractive indices for each ray
                    transmitProp.Add(N2);
                    reflectProp.Add(new RefractiveIndex(
                        new[] { wlens[idx] }, new[] { n1[idx] }, N1.Units));
                    surfNorm = dNorm;
                }

                // Reflection and transmission vectors
                Tuple<Vector, Vector> rtVecs = Refraction(ray.Dir, surfNorm, mat1, mat2);

                // Calculates the origin time
                double startTime = ray.Time + n1[idx] * ray.IntersectData.Travel / c;

                // Adds transmitted ray (If below the critical angle)
                if (!double.IsNaN(rtVecs.Item1.Comp[0]))
                {
                    // Makes a new child ray
                    RenderRay transmitRay = new RenderRay(
                        ray.IntersectData.IntersectPt, rtVecs.Item1, 
                        startTime, ray.Rank + 1, RayType.Transmitted,
                        transmitProp);

                    // Adds the render ray as the parent of the casted ray
                    transmitRay.ParentRay = ray;

                    // Adds the new transmitted ray
                    ray.IntersectData.CastedRays.Add(transmitRay);
                }

                // Adds reflected ray (If specified)
                if (CastReflectedRays)
                {
                    // Makes a new rays
                    RenderRay reflectRay = new RenderRay(
                        ray.IntersectData.IntersectPt, rtVecs.Item2,
                        startTime, ray.Rank + 1, RayType.Reflected,
                        reflectProp);

                    // Adds the render ray as the parent of the casted ray
                    reflectRay.ParentRay = ray;

                    // Adds new reflected and transmitted rays
                    ray.IntersectData.CastedRays.Add(reflectRay);
                }
            }

            // returns interaction notification
            return true;
        }

        /// <summary>
        /// Calculates the reflection and refraction propagation vectors using
        /// a modified Heckbert method.
        /// </summary>
        /// <param name="dir"> Ray direction </param>
        /// <param name="norm"> Surface normal </param>
        /// <param name="n1"> Exiting material index </param>
        /// <param name="n2"> Entering material index </param>
        /// <returns> The transmitted and refraction propagation vectors </returns>
        public static Tuple<Vector,Vector> Refraction(Vector dir, Vector norm, 
            double n1, double n2)
        {
            double c1 = norm.Dot(-1.0*dir);

            // Reflection
            Vector reflect = dir + 2 * c1 * norm;

            double eta = n1 / n2;
            double c2 = Math.Sqrt(1 - eta * eta * (1 - c1 * c1));

            // Transmission (c2 = NaN Total for internal reflection) 
            Vector transmit = new Vector();
            if (!double.IsNaN(c2))
                transmit = eta * dir + (eta * c1 - c2) * norm;
            else
                transmit = new Vector(new double[]{ double.NaN });

            return new Tuple<Vector, Vector>(transmit, reflect);
        }

        /// <summary>
        /// Clones this instance by performing a deep copy
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        new public Transparency Clone()
        {
            return (Transparency)CloneImp();
        }

        /// <summary>
        /// Deep-copy clones this instance
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        protected override Material CloneImp()
        {
            // Shallow copies from base
            Transparency newCopy = (Transparency)base.CloneImp();

            // Deep-copies all data this is referenced by default
            if (RefractiveIndex != null)
                newCopy.RefractiveIndex = RefractiveIndex.Clone();

            return newCopy;
        }
        #endregion Methods
    }
}
