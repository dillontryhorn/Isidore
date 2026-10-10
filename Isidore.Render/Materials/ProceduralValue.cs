using System.Collections.Generic;
using Isidore.Maths;

namespace Isidore.Render
{
    /// <summary>
    /// Enumeration of the different procedural value interpolation options
    /// </summary>
    public enum ProcInterp
    {
        /// <summary>
        /// Interpolates over noise parameters
        /// </summary>
        Params,
        /// <summary>
        /// Interpolates over noise values
        /// </summary>
        Values
    };


    /// <summary>
    /// ProceduralValue is a material that only supports procedural noise 
    /// functions.  It interpolates over a list of ProceduralPoints 
    /// that is treated as a poly-line with the noise interpolated over the 
    /// closest segment.  If casted rays are allowed, they will start at the
    /// intersection point with the identical direction as its parent.
    /// </summary>
    public class ProceduralValue : Material
    {
        #region Fields & Properties

        /// <summary>
        /// The procedural noise used to calculating a value
        /// </summary>
        public Noise noise { get; set; }

        /// <summary>
        /// Smoothstep Hermite polynomial
        /// </summary>
        public SmoothStep polynomial { get; set; }

        /// <summary>
        /// Switch to interpolate either noise (0) values or (1) parameters
        /// </summary>
        public ProcInterp interp { get; set; }

        /// <summary>
        /// Anchors texture to the local coordinate system
        /// (Used for procedural textures)
        /// </summary>
        public bool anchorTextureToBody { get; set; }

        /// <summary>
        /// Procedural parameters points
        /// </summary>
        protected List<ProceduralPoint> procPts;

        /// <summary>
        /// List of procedural points that provides parameters for the noise 
        /// function
        /// </summary>
        public List<ProceduralPoint> ProceduralPoints
        {
            get { return procPts; }
            set { procPts = value; }
        }


        #endregion Fields & Properties
        #region Constructors

        /// <summary>
        /// Constructor.  The noise function is referenced (Not cloned)
        /// </summary>
        /// <param name="noise"> Procedural noise function used to provide a value 
        /// for the material </param>
        /// <param name="procPts"> Procedural point list that provides 
        /// parameters for the noise function </param>
        /// <param name="polyOrder"> Smoothstep Hermite polynomial order 
        /// (0-6, 0=linear) </param>
        /// <param name="interp"> Interpolates noise parameters or 
        /// values </param>
        /// <param name="anchorTextureToBody"> Flag for anchoring the texture
        /// to the body local coordinates instead of the global 
        /// coordinates </param>
        public ProceduralValue(Noise noise, List<ProceduralPoint> procPts,
        int polyOrder = 0, ProcInterp interp = ProcInterp.Params,
        bool anchorTextureToBody = true)
        {
            this.noise = noise;
            this.procPts = procPts;
            polynomial = new SmoothStep(polyOrder);
            this.interp = interp;
            this.anchorTextureToBody = anchorTextureToBody;
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
            double val = 0;

            // Handles the single point case
            if (procPts.Count == 1)
                val = SinglePointEval(ray);
            else
            {
                // Calls local ProcessIntersectData method
                ProceduralPoint pt0, pt1;
                double unitDist, smoothDist;
                Vector minDistDir;
                val = ProcessData(ray.IntersectData.IntersectPt, 
                    ray.IntersectData.Body.Local2World, ray.Time, out pt0, 
                    out pt1, out unitDist, out smoothDist, out minDistDir);
            }

            // Converts the noise value into a scalar and adds it to the 
            // property array
            Scalar thisVal = new Scalar(val);
            ray.IntersectData.Properties.Add(thisVal);

            // returns interaction notification
            return true;
        }

        /// <summary>
        /// Determines the pertinent procedural texturing information
        /// </summary>
        /// <param name="iPt"> Intersection ray </param>
        /// <param name="trans"> Intersecting body's transform </param>
        /// <param name="time"> Intersection time </param>
        /// <param name="pt0"> Lower bound line segment point </param>
        /// <param name="pt1"> Upper bound line segment point </param>
        /// <param name="unitDist"> Unitized distance of intersection from
        /// pt0 </param>
        /// <param name="smoothDist"> Smooth step applied to the unitized 
        /// distance </param>
        /// <param name="minDistDir"> Intersection line segment 
        /// direction </param>
        /// <returns> Procedural texture value </returns>
        public double ProcessData(Point iPt, Transform trans, double time, 
            out ProceduralPoint pt0, out ProceduralPoint pt1, 
            out double unitDist, out double smoothDist, out Vector minDistDir)
        {
            // Finds the point bounding the intersected line segment
            // and pertinent intersection information
            FindLocation(iPt, out pt0, out pt1, out unitDist, out smoothDist, 
                out minDistDir);

            // Interpolation methods
            double val = 0;
            switch (interp)
            {
                // Interpolates parameters
                case ProcInterp.Params:
                    val = InterpolateParameters(pt0, pt1, smoothDist, iPt,
                        trans, time);
                    break;

                // Interpolates values
                case ProcInterp.Values:
                    val = InterpolateValue(pt0, pt1, smoothDist, iPt, trans,
                        time);
                    break;
            }

            return val;
        }

        /// <summary>
        /// Finds the line segment intersected by a point
        /// </summary>
        /// <param name="iPt"> Intersection point </param>
        /// <param name="pt0"> Lower bound line segment point </param>
        /// <param name="pt1"> Upper bound line segment point </param>
        /// <param name="unitDist"> Unitized distance of intersection from
        /// pt0 </param>
        /// <param name="smoothDist"> Smooth step applied to the unitized 
        /// distance </param>
        /// <param name="minDistDir"> Intersection line segment 
        /// direction </param>
        protected void FindLocation(Point iPt, out ProceduralPoint pt0,
            out ProceduralPoint pt1, out double unitDist, 
            out double smoothDist, out Vector minDistDir)
        { 
            // Cycles through each line to find the closest segment
            int minDistIdx = 0; // Index of closest line
            double minDist = double.PositiveInfinity; // Closest distance
            double minDistTravel = 0; // Travel from line segment origin
            minDistDir = new Vector(iPt.Comp.Length); // Line direction
            for (int idx = 0; idx < procPts.Count - 1; idx++)
            {
                // Finds 1) the closest distance between point & line
                // 2) the point of closest approach along the line
                // 3) the travel from the first point to the closest
                // approach point
                System.Tuple<double, Point, double, Vector> distData = Distance.Point2Line(iPt, procPts[idx],
                    procPts[idx + 1]);
                // Updates the closest line segment data
                if (distData.Item1 >= 0 && distData.Item1 < minDist)
                {
                    minDistIdx = idx;
                    minDist = distData.Item1;
                    minDistTravel = distData.Item3;
                    minDistDir = distData.Item4;
                }
            }

            // Interpolates the noise values
            pt0 = procPts[minDistIdx];
            pt1 = procPts[minDistIdx + 1];

            // Finds the relative distance
            double segLen = pt0.Distance(pt1);
            unitDist = minDistTravel / segLen;

            // Weights the unit distance using Smoothstep if the order > 0
            // and there is more than one point
            smoothDist = (procPts.Count == 1 || polynomial.Order == 0)
                ? unitDist : polynomial.Evaluate(unitDist);
        }

        /// <summary>
        /// Interpolates the noise parameters of two procedural points
        /// </summary>
        /// <param name="pt0"> First procedural point </param>
        /// <param name="pt1"> Second procedural point </param>
        /// <param name="smoothDist"> Unit distance from pt0 to pt1 </param>
        /// <param name="iPt"> Intersection ray </param>
        /// <param name="trans"> Intersecting body's transform </param>
        /// <param name="time"> Intersection time </param>
        /// <returns> Interpolated noise value </returns>
        protected double InterpolateParameters(ProceduralPoint pt0,
            ProceduralPoint pt1, double smoothDist, Point iPt, 
            Transform trans, double time)
        {
            // Interpolates noise parameters
            Vector intShift = Interpolate.Linear( pt0.ProcNoiseParams.shift,
                pt1.ProcNoiseParams.shift, smoothDist);
            Vector intVel = Interpolate.Linear( pt0.ProcNoiseParams.velocity,
                pt1.ProcNoiseParams.velocity, smoothDist);
            double intMult = pt0.ProcNoiseParams.multiplier * (1 - smoothDist) + 
                pt1.ProcNoiseParams.multiplier * smoothDist;
            double intOff = pt0.ProcNoiseParams.offset * (1 - smoothDist) +
                pt1.ProcNoiseParams.offset * smoothDist;

            // If the texture is anchored to the local coordinate, 
            // casts the intersect point to the local frame 
            // of reference.
            if (anchorTextureToBody)
            {
                // Overwrites the reference point with a clone
                iPt = iPt.CopyTransform(trans, false);

                // Updates the vectors in noise space
                intShift.Transform(trans, false);
                intVel.Transform(trans, false);
            }

            // Noise coordinate
            Vector totalShift = intShift + time * intVel;

            // Retrieves the noise value at the intersection point
            double val = noise.GetVal(iPt, totalShift, intMult, intOff);

            return val;
        }

        /// <summary>
        /// Interpolates the noise values of two procedural points
        /// </summary>
        /// <param name="pt0"> First procedural point </param>
        /// <param name="pt1"> Second procedural point </param>
        /// <param name="smoothDist"> Unit distance from pt0 to pt1 </param>
        /// <param name="iPt"> Intersection ray </param>
        /// <param name="trans"> Intersecting body's transform </param>
        /// <param name="time"> Intersection time </param>
        /// <returns> Interpolated noise value </returns>
        protected double InterpolateValue(ProceduralPoint pt0, 
            ProceduralPoint pt1, double smoothDist, Point iPt,
            Transform trans, double time)
        {
            // Parameters for each procedural set
            Vector shift0 = pt0.ProcNoiseParams.shift;
            Vector shift1 = pt1.ProcNoiseParams.shift;
            Vector vel0 = pt0.ProcNoiseParams.velocity;
            Vector vel1 = pt1.ProcNoiseParams.velocity;

            // If the texture is anchored to the local coordinate, casts
            // the intersect point to the local frame of reference.
            if (anchorTextureToBody)
            {
                // Overwrites the reference point with a clone
                iPt = iPt.CopyTransform(trans, false);

                // Updates the vectors in noise space
                shift0 = shift0.CopyTransform(trans, false);
                shift1 = shift1.CopyTransform(trans, false);
                vel0 = vel0.CopyTransform(trans, false);
                vel1 = vel1.CopyTransform(trans, false);
            }

            // Noise coordinate
            Vector totalShift0 = shift0 + time * vel0;
            Vector totalShift1 = shift1 + time * vel1;

            // Retrieves the noise value at the intersection point
            double val0 = noise.GetVal(iPt, totalShift0,
                pt0.ProcNoiseParams.multiplier,
                pt0.ProcNoiseParams.offset);
            double val1 = noise.GetVal(iPt, totalShift1,
                pt1.ProcNoiseParams.multiplier,
                pt1.ProcNoiseParams.offset);

            // Interpolates the values
            double val = val0 * (1 - smoothDist) + val1 * smoothDist;

            return val;
        }

        /// <summary>
        /// Evaluates a single point
        /// </summary>
        /// <param name="ray"> intersecting ray </param>
        /// <returns> Noise value </returns>
        protected double SinglePointEval(RenderRay ray)
        {
            // Finds the intersect point
            Point iPt = ray.IntersectData.IntersectPt;

            // Values used for interpolating noise parameters
            Vector intShift = procPts[0].ProcNoiseParams.shift;
            Vector intVel = procPts[0].ProcNoiseParams.velocity;
            double intMult = procPts[0].ProcNoiseParams.multiplier;
            double intOff = procPts[0].ProcNoiseParams.offset;

            // If the texture is anchored to the local coordinate, casts
            // the intersect point to the local frame of reference.
            if (anchorTextureToBody)
            {
                Transform trans = ray.IntersectData.Body.Local2World;
                // Overwrites the reference point with a clone
                iPt = iPt.CopyTransform(trans, false);

                // Updates the vectors in noise space
                intShift = intShift.CopyTransform(trans, false);
                intVel = intVel.CopyTransform(trans, false);
            }

            // Noise coordinate
            Vector totalShift = intShift + ray.Time * intVel;

            // Retrieves the noise value at the intersection point
            double val = noise.GetVal(iPt, totalShift, intMult,
                intOff);

            return val;
        }

        /// <summary>
        /// Clones this instance by performing a deep copy
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        new public ProceduralValue Clone()
        {
            return (ProceduralValue)CloneImp();
        }

        /// <summary>
        /// Deep-copy clones this instance
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        protected override Material CloneImp()
        {
            // Shallow copies from base
            ProceduralValue newCopy = (ProceduralValue)base.CloneImp();

            // Deep-copies all data this is referenced by default
            if (noise != null)
                newCopy.noise = noise.Clone();
            newCopy.polynomial = polynomial.Clone();
            if (procPts != null)
            {
                newCopy.procPts = new List<ProceduralPoint>(procPts.Count);
                for (int idx = 0; idx < procPts.Count; idx++)
                    newCopy.procPts.Add(procPts[idx].Clone());
            }

            return newCopy;
        }

        #endregion Methods
    }

    /// <summary>
    /// ProceduralPoint provides the parameters used by a noise function
    /// </summary>
    public class ProceduralPoint : Point
    {
        #region Fields & Properties

        /// <summary>
        /// Procedural parameters
        /// </summary>
        public ProceduralNoiseParameters ProcNoiseParams { get; set; }

        #endregion Fields & Properties
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="position"> Location of the point </param>
        /// <param name="shift"> Noise shift coordinate vector </param>
        /// <param name="velocity"> Noise velocity vector</param>
        /// <param name="multiplier"> Noise variance multiplier </param>
        /// <param name="offset"> Additional offset added to the noise 
        /// (i.e. mean) </param>
        public ProceduralPoint(Point position = null, Vector velocity = null,
            Vector shift = null, double multiplier = 1, double offset = 0) :
            base(position)
        {
            ProcNoiseParams = new ProceduralNoiseParameters(velocity, shift, 
                multiplier, offset);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="position"> Location of the point </param>
        /// <param name="Params"> Procedural parameters </param>
        public ProceduralPoint(Point position, 
            ProceduralNoiseParameters Params) : base(position)
        {
            ProcNoiseParams = Params;
        }

        #endregion Constructor
        #region Methods

        /// <summary>
        /// Deep copy clone method
        /// </summary>
        /// <returns> Deep copy clone </returns>
        new public ProceduralPoint Clone()
        {
            return new ProceduralPoint(base.Clone(),
                ProcNoiseParams == null ? null : ProcNoiseParams.Clone());
        }

        #endregion Methods
    }

    /// <summary>
    /// Noise parameters and default values used by the ProceduralPoint class
    /// </summary>
    public class ProceduralNoiseParameters : NoiseParameters
    {
        #region Fields & Properties

        /// <summary>
        /// Velocity vector used to shift the noise coordinates using
        /// the ray time
        /// </summary>
        public Vector velocity { get; set; }

        #endregion Fields & Properties
        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="velocity"> Noise velocity vector</param>
        /// <param name="shift"> Noise shift coordinate vector </param>
        /// <param name="multiplier"> Noise variance multiplier </param>
        /// <param name="offset"> Additional offset added to the noise 
        /// (i.e. mean) </param>
        public ProceduralNoiseParameters(Vector velocity = null,
            Vector shift = null, double multiplier = 1.0,
            double offset = 0.0) : base(shift, multiplier, offset)
        {
            this.velocity = velocity ?? Vector.Zero();
        }

        #endregion Constructor
        #region Methods

        /// <summary>
        /// Returns a clone of this procedural parameters class via deep copy
        /// </summary>
        /// <returns> The cloned copy </returns>
        public new ProceduralNoiseParameters Clone()
        {
            ProceduralNoiseParameters newInt = base.Clone() as ProceduralNoiseParameters;
            newInt.velocity = velocity.Clone();

            return newInt;
        }

        #endregion Methods
    }
}
