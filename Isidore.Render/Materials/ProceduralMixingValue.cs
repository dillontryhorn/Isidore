using System;
using System.Collections.Generic;
using Isidore.Maths;

namespace Isidore.Render
{
    /// <summary>
    /// Enumeration of the different procedural value interpolation options
    /// </summary>
    public enum ProcMix
    {
        /// <summary>
        /// Mixes via remapping texture coordinates
        /// </summary>
        Remap,

        /// <summary>
        /// Mixes by integrating the mixing and procedural noises
        /// </summary>
        Integrate
    };

    /// <summary>
    /// ProceduralValue is a material that only supports procedural noise 
    /// functions.  It interpolates over a list of ProceduralPoints 
    /// that is treated as a poly-line with the noise interpolated over the 
    /// closest segment.  If casted rays are allowed, they will start at the
    /// intersection point with the identical direction as its parent.
    /// </summary>
    public class ProceduralMixingValue : ProceduralValue
    {
        #region Fields & Properties

        /// <summary>
        /// Noise to use for perturbation
        /// </summary>
        public Noise perturbNoise { get; set; }

        /// <summary>
        /// Perturbation noise velocity vector
        /// </summary>
        public Vector perturbVel { get; set; }

        /// <summary>
        /// Denominator of the fraction of 
        /// the segment length covered by the two smoothing functions in the
        /// perturbation region (i.e. 2 = 1/2 of the segment)
        /// </summary>
        public double perturbSmoothFrac { get; set; }

        /// <summary>
        /// Smoothstep Hermite polynomial applied to the perturbations
        /// </summary>
        public SmoothStep perturbPoly { get; set; }

        /// <summary>
        /// Value mixing method
        /// </summary>
        public ProcMix mixMeth { get; set; }

        #endregion Fields & Properties
        #region Constructors

        /// <summary>
        /// Constructor.  The noise function is referenced (Not cloned)
        /// </summary>
        /// <param name="noise"> Procedural noise function used to provide a value 
        /// for the material </param>
        /// <param name="procPts"> Procedural point list that provides 
        /// parameters for the noise function </param>
        /// <param name="perturbNoise"> Procedural noise used for perturbing
        /// turbulence </param>
        /// <param name="polyOrder"> Smoothstep Hermite polynomial order 
        /// (0-6, 0=linear) </param>
        /// <param name="interp"> Interpolates noise parameters or 
        /// values </param>
        /// <param name="perturbSmoothFrac"> Denominator of the fraction of 
        /// the segment length covered by the two smoothing functions in the
        /// perturbation region (i.e. 2 = 1/2 of the segment) </param>
        /// <param name="perturbPolyOrder"> The order of the smoothstep 
        /// polynomial applied to the perturbations </param>
        /// <param name="mixMeth"> Value mixing method </param>
        /// <param name="perturbVel"> Perturbation velocity </param>
        /// <param name="anchorTextureToBody"> Flag for anchoring the texture
        /// to the body local coordinates instead of the global 
        /// coordinates </param>
        public ProceduralMixingValue(Noise noise,
            List<ProceduralPoint> procPts, Noise perturbNoise,
            int polyOrder = 0, ProcInterp interp = ProcInterp.Params,
            double perturbSmoothFrac = 2, int perturbPolyOrder = 2,
            ProcMix mixMeth = ProcMix.Remap, Vector perturbVel = null, 
            bool anchorTextureToBody = true) : base(noise, procPts, polyOrder,
                interp, anchorTextureToBody)
        {
            this.perturbNoise = perturbNoise;
            this.perturbSmoothFrac = perturbSmoothFrac;
            perturbPoly = new SmoothStep(perturbPolyOrder);
            this.mixMeth = mixMeth;
            this.perturbVel = perturbVel ?? Vector.Zero();
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
            // The single point case is the same between mixing & not
            if (procPts.Count == 1)
                return base.ProcessIntersectData(ref ray);

            // Finds the base value & associated data
            Point iPt = ray.IntersectData.IntersectPt;
            ProceduralPoint pt0, pt1;
            double unitDist, smoothDist;
            Vector minDistDir;
            double val = ProcessData(iPt, ray.IntersectData.Body.Local2World,
                ray.Time, out pt0, out pt1, out unitDist, out smoothDist,
                out minDistDir);

            // If the unit distance is between 0-1 then calculates 
            // the value with a turbulence spatial offset
            if (unitDist > 0 && unitDist < 1)
            {
                // Perturbation vector
                Vector pVec = PerturbVector(iPt, minDistDir, ray.Time);

                // Smooth step scale based on the unit distance (0-1-0)
                double smoothScale = unitPlacement(unitDist);

                // Applies mixing
                Point pPt = iPt.Clone(); 
                switch (mixMeth)
                {
                    case ProcMix.Remap:
                        // Smooths the vector
                        pPt += smoothScale * (Point)pVec;

                        // Turbulence associated with the mapped point
                        val = ProcessData(pPt,
                            ray.IntersectData.Body.Local2World, ray.Time,
                            out pt0, out pt1, out unitDist, out smoothDist,
                            out minDistDir);
                        break;

                    case ProcMix.Integrate:
                        // New noise location
                        pPt += (Point)pVec;

                        // Perturbed value
                        double valP = ProcessData(pPt,
                            ray.IntersectData.Body.Local2World, ray.Time,
                            out pt0, out pt1, out unitDist, out smoothDist,
                            out minDistDir);

                        // Integrates the perturb and procedural value
                        val = val * (1 - smoothScale) + valP * smoothScale;
                        break;
                }
            }

            // Converts the noise value into a scalar and adds it to the 
            // property array
            Scalar thisVal = new Scalar(val);
            ray.IntersectData.Properties.Add(thisVal);

            // returns interaction notification
            return true;
        }

        /// <summary>
        /// Finds the perturbation vector associated with an intersection 
        /// point and the perturbation procedural texture
        /// </summary>
        /// <param name="iPt"> Intersection point </param>
        /// <param name="segDir"> Line segment direction </param>
        /// <param name="time"> Render time (used with the velocity) </param>
        /// <returns> Perturbed point </returns>
        public Vector PerturbVector(Point iPt, Vector segDir, double time)
        {
            // Calculates the total shift vector
            Vector sVec = perturbNoise.shift + perturbVel * time;

            // Finds the texture value associated with the shifted location
            double perturbMag = perturbNoise.GetVal(iPt, sVec);

            // Finds the perturbation offset vector
            Vector pVec = perturbMag * segDir;

            return pVec;
        }

        /// <summary>
        /// Finds the perturbation point associated with an intersection point
        /// and the perturbation procedural texture
        /// </summary>
        /// <param name="iPt"> Intersection point </param>
        /// <param name="unitDist"> Unit distance from the first point in 
        /// the line segment </param>
        /// <param name="segDir"> Line segment direction </param>
        /// <param name="time"> Render time (used with the velocity) </param>
        /// <returns> Perturbed point </returns>
        public Point PerturbPoint(Point iPt, double unitDist, Vector segDir, 
            double time)
        {
            // Calculates the total shift vector
            Vector sVec = perturbNoise.shift + perturbVel * time;

            // Finds the texture value associated with the shifted location
            double perturbMag = perturbNoise.GetVal(iPt, sVec);

            // Finds the perturbation offset vector
            Vector perturbOffset = perturbMag * segDir;

            // Scales the perturbation value based on its location 
            // along in the vector
            double unitPlace = unitPlacement(unitDist);

            // Finds the perturbation offset vector
            Vector smoothOffset = perturbOffset * unitPlace;

            // Perturbed point
            Point pPt = iPt + (Point)perturbOffset;

            Point sPt = iPt + (Point)smoothOffset;

            return pPt;

            //// Calculates the total shift vector
            //var sVec = perturbNoise.shift + perturbVel * time;

            //// Finds the texture value associated with the shifted location
            //var perturbVal = perturbNoise.GetVal(iPt, sVec);

            //// Scales the perturbation value based on its location 
            //// along in the vector
            //var unitPlace = unitPlacement(unitDist);
            //var perturbMag = perturbVal * unitPlace;

            //// Finds the perturbation offset vector
            //var perturbOffset = perturbMag * segDir;

            //// Perturbed point
            //var pPt = iPt + (Point)perturbOffset;

            //return pPt; 
        }

        /// <summary>
        /// Using a unit distance, calculates the placement that is zero at
        /// each end and one at the center using a smoothstep function
        /// </summary>
        /// <param name="unitDist"> Unit distance (0-1) </param>
        /// <returns> Unit placement </returns>
        public double unitPlacement(double unitDist)
        {
            // Finds the unit distance from the segment center
            double placeHalf = 2.0 * (0.5 - Math.Abs(unitDist - 0.5));

            // limits that smoothed distance to a fraction of the length
            placeHalf *= perturbSmoothFrac;

            // Applies the smoothstep and clamps the values to 0-1 
            double place = perturbPoly.Evaluate(placeHalf);

            return place;
        }

        /// <summary>
        /// Clones this instance by performing a deep copy
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        new public ProceduralMixingValue Clone()
        {
            return CloneImp() as ProceduralMixingValue;
        }

        /// <summary>
        /// Deep-copy clones this instance
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        protected override Material CloneImp()
        {
            // Shallow copies from base
            ProceduralMixingValue newCopy = (ProceduralMixingValue)base.CloneImp();

            // Deep-copies all data this is referenced by default
            if (perturbNoise != null)
                newCopy.perturbNoise = perturbNoise.Clone();
            if (perturbVel != null)
                newCopy.perturbVel = perturbVel.Clone();
            if (perturbPoly != null)
                newCopy.perturbPoly = perturbPoly.Clone();

            return newCopy;
        }

        #endregion Methods
    }
}
