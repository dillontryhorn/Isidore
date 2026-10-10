using Isidore.Maths;
using System;

namespace Isidore.Render
{

    /// <summary>
    /// Represent a CGI billboard.  The transformation matrix
    /// does not alter the billboard width or height, these
    /// are scaled via individual key frames.
    /// </summary>
    public class Billboard: Shape
    {
        # region Fields & Properties

        /// <summary>
        /// Billboard's lower corner in global space
        /// </summary>
        private Point globalLowerCorner;

        /// <summary>
        /// Billboard lower corner, effectively the transformation
        /// matrix translation
        /// </summary>
        protected Point lowerCorner;

        /// <summary>
        /// Billboard's lower corner at this time.
        /// If set, the point is cloned
        /// </summary>
        public Point LowerCorner
        {
            get { return lowerCorner; }
            set
            {
                lowerCorner = value.Clone(); // Deep copy
                // Could have done this manually, but this looks nice
                AdvanceToTime(CurrentTime, true);
            }
        }

        /// <summary>
        /// Billboard's surface normal in local space
        /// </summary>
        public Normal LocalSurfaceNormal;

        /// <summary>
        /// Billboard's surface normal in global space
        /// </summary>
        private Normal globalSurfaceNormal;

        /// <summary>
        /// Billboard's surface normal in global space
        /// </summary>
        public Normal SurfaceNormal
        {
            get { return globalSurfaceNormal; }
        }

        /// <summary>
        /// Billboard's up direction in local space
        /// </summary>
        public Vector LocalUpDirection;

        /// <summary>
        /// Billboard's up direction in global space
        /// </summary>
        private Vector globalUpDirection;

        /// <summary>
        /// Billboard's up direction in global space
        /// </summary>
        public Vector UpDirection
        {
            get { return globalUpDirection; }
        }

        /// <summary>
        /// Billboard width.
        /// Uses a key frame so the width can be animated
        /// </summary>
        public Maths.KeyFrame<double> KeyWidths;

        /// <summary>
        /// Billboard's width in global space. It's value is determined via 
        /// key frame. Changing this value will rescale the entire key frame
        /// as well.
        /// </summary>
        public double Width
        {
            get { return KeyWidths.CurrentValue; }
            set // Not sure if this makes sense
            {
                double factor = value / KeyWidths.CurrentValue;
                KeyWidths.Scale(factor);
                if (!double.IsNaN(CurrentTime))
                    AdvanceToTime(CurrentTime, true);
            }
        }

        /// <summary>
        /// Billboard height.
        /// Uses a key frame so the height can be animated
        /// </summary>
        public Maths.KeyFrame<double> KeyHeights;

        /// <summary>
        /// Billboard's height in global space. It's value is determined via 
        /// key frame. Changing this value will rescale the entire key frame
        /// as well.
        /// </summary>
        public double Height
        {
            get { return KeyHeights.CurrentValue; }
            set // Not sure if this makes sense
            {
                double factor = value / KeyHeights.CurrentValue;
                KeyHeights.Scale(factor);
                if (!double.IsNaN(CurrentTime))
                    AdvanceToTime(CurrentTime, true);
            }
        }

        //These are private variables for setting the current time parameters
        // Animated width at current time
        private double currWidth;
        // Animated width at current time
        private double currHeight;
        // Index of the major axis
        private int majorAxis;
        // Projected vectors (2D space)
        private Vector A, AB, AD, ABpDC, ADpBC;
        // U,V determinants
        private double denU, denV;

        #endregion Fields & Properties
        #region Constructors

        /// <summary>
        /// Billboard constructor where the width and height is set via 
        /// key frame.  All parameters are referenced.
        /// </summary>
        /// <param name="LowerCorner"> Billboard's lower corner </param>
        /// <param name="SurfaceNormal"> Billboard's surface normal </param>
        /// <param name="UpDirection"> Billboard's up direction </param>
        /// <param name="KeyWidths"> Billboards's animated width </param>
        /// <param name="KeyHeights"> Billboard's animated height </param>
        public Billboard(Point LowerCorner, Normal SurfaceNormal, 
            Vector UpDirection, KeyFrame<double> KeyWidths, 
            KeyFrame<double> KeyHeights)
        {
            lowerCorner = LowerCorner ?? new Point(-0.5, -0.5, 0);
            LocalSurfaceNormal = SurfaceNormal ?? new Normal(0, 0, -1);
            LocalUpDirection = UpDirection ?? new Vector(0,1,0);
            this.KeyWidths = KeyWidths ?? new KeyFrame<double>(1.0, 0);
            this.KeyHeights = KeyHeights ?? new KeyFrame<double>(1.0, 0);
        }

        /// <summary>
        /// Billboard constructor where the width and height is hard set.
        /// All parameters are referenced.
        /// </summary>
        /// <param name="LowerCorner"> Billboard's lower corner </param>
        /// <param name="SurfaceNormal"> Billboard's surface normal </param>
        /// <param name="UpDirection"> Billboard's up direction </param>
        /// <param name="Width">Billboards's constant width</param>
        /// <param name="Height"> Billboard's constant height </param>
        public Billboard(Point LowerCorner = null, Normal SurfaceNormal = null,
            Vector UpDirection = null, double Width = 1, double Height = 1) :
            this(LowerCorner, SurfaceNormal, UpDirection,
                new KeyFrame<double>(Width, 0), 
                new KeyFrame<double>(Height, 0))
        { }

        #endregion Constructors
        #region Methods

        /// <summary>
        /// Sets this instance's state vectors to time "now"
        /// </summary>
        /// <param name="now"> Time to set this instance to </param>
        /// <param name="force"> Forces AdvanceToTime to run even if the time is the same </param>
        override public void AdvanceToTime(double now, bool force=false)
        {
            // Checks to see if time is different
            if (!force && now == CurrentTime)
                return;

            // Updates transform time line via virtual definition in Item class
            base.AdvanceToTime(now, force);

            // Updates the lower corner, surface normal, up direction
            // in global space for the current time
            globalLowerCorner = lowerCorner.
                CopyTransform(TransformTimeLine.CurrentValue);
            globalSurfaceNormal = LocalSurfaceNormal.
                CopyTransform(TransformTimeLine.CurrentValue);
            globalSurfaceNormal.Normalize();
            globalUpDirection = LocalUpDirection.
                CopyTransform(TransformTimeLine.CurrentValue);

            // Updates the current width and height
            currWidth = KeyWidths.InterpolateToTime(now);
            currHeight = KeyHeights.InterpolateToTime(now);

            // Major axis if the surface normal
            majorAxis = globalSurfaceNormal.MajorAxis();
            A = (Vector)globalLowerCorner.CopyReduceComponent(majorAxis);
            AB = -currWidth * globalSurfaceNormal.Cross(globalUpDirection);
            AB.ReduceComponent(majorAxis);
            AD = Height * globalUpDirection.CopyReduceComponent(majorAxis);
            ABpDC = 2.0 * AB;
            ADpBC = 2.0 * AD;

            // U,V Determinants
            denU = Det(AD.Comp, ABpDC.Comp);
            denV = Det(AB.Comp, ADpBC.Comp);
        }

        /// <summary>
        /// Finds the determinate of a 2x2 matrix
        /// </summary>
        /// <param name="a"> first row </param>
        /// <param name="b"> second row </param>
        /// <returns></returns>
        private double Det(double[] a, double[] b)
        {
            return a[0] * b[1] - a[1] * b[0];
        }

        /// <summary>
        /// Performs an intersection check and operation with a ray using
        /// a modified quad approach from Schlick and Subrenat GG5, pg232-237.
        /// Billboard shapes will automatically set their time to 
        /// the ray's creation time. Billboard operates in global space.
        /// </summary>
        /// <param name="ray"> Ray to evaluate for intersection </param>
        /// <returns> Intersect flag (true = intersection) </returns>
        public override bool Intersect(ref RenderRay ray)
        {
            // If the shape isn't on, it should count as a miss
            if (!On)
                return false;

            // Checks the ray's time and sets the shape to the same time
            // Probably does a similar check in each child class
            if (ray.Time != CurrentTime)
                AdvanceToTime(ray.Time);

            // Incidence angle cosine
            double cosIncAng = -ray.Dir.Dot(globalSurfaceNormal);
            if (cosIncAng == 0)
                return false;

            // Back face check
            if (!IntersectBackFaces && cosIncAng < 0)
                return false;

            // Finds travel distance to plane
            Vector AP = new Vector(ray.Origin - globalLowerCorner); // separation
            double t = AP.Dot(globalSurfaceNormal)/cosIncAng;

            // If farther than current hit, too close, or in negative space, 
            // returns hit=false
            if (double.IsNaN(t) || double.IsInfinity(t) ||
                t > ray.IntersectData.Travel || t > ray.MaximumTravel ||
                t < ray.MinimumTravel) return false;

            // Finds closest planar axis (A 2D vector)
            Vector M = (Vector)ray.Origin + ray.Dir * t;
            M.ReduceComponent(majorAxis);
            Vector AM = M - A;

            // U,V coordinates are used for calculating the hit operator
            double u = double.NaN;
            // V coordinate
            double v = -Det(AM.Comp, ABpDC.Comp) / denV;
            if (v < 0 || v >= 1)
                return false;
            // U coordinate
            u = Det(AM.Comp, ADpBC.Comp) / denU;
            if (u < 0 || u >= 1)
                return false;

            // Applies alpha mapping
            if (UseAlpha)
            { 
                // Finds the alpha value from the UV coordinates
                bool alphaHit = getAlpha(u, v);

                if (!alphaHit) return false;
            }

            // Intersect point
            Point intPt = ray.Origin + (Point)ray.Dir * t;

            // Creates new intersection data
            ShapeSpecificData sData = new ShapeSpecificData(
                globalSurfaceNormal, cosIncAng, u, v);
            IntersectData iData = new IntersectData(true, t, intPt,
                this, sData);
            ray.IntersectData = iData;

            return true;
        }

        /// <summary>
        /// Clones this instance by performing a deep copy
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        new public Billboard Clone()
        {
            return (Billboard)CloneImp();
        }

        /// <summary>
        /// Deep-copy clones this instance
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        protected override Item CloneImp()
        {
            Billboard newCopy = (Billboard)MemberwiseClone();

            // Deep copy
            DeepCopyOverride(ref newCopy);

            // If the current time has been set, then this should set
            // the interpolated members in the copy
            if (!double.IsNaN(CurrentTime))
                newCopy.AdvanceToTime(CurrentTime, true);

            return newCopy;
        }

        /// <summary>
        /// Implements deep copies of members that would
        /// otherwise be shallow copied.
        /// </summary>
        /// <param name="copy"> Clone copy </param>
        protected void DeepCopyOverride(ref Billboard copy)
        {
            // Base copy
            Shape baseCast = copy; // This is a shallow copy
            DeepCopyOverride(ref baseCast);

            //protected Point lowerCorner;
            copy.lowerCorner = lowerCorner.Clone();

            //public Normal LocalSurfaceNormal;
            copy.LocalSurfaceNormal = LocalSurfaceNormal.Clone();

            //public Vector LocalUpDirection;
            copy.LocalUpDirection = LocalUpDirection.Clone();

            //public Maths.KeyFrame<double> KeyWidths;
            copy.KeyWidths = KeyWidths.Clone();

            //public Maths.KeyFrame<double> KeyHeights;
            copy.KeyHeights = KeyHeights.Clone();
        }

        #endregion Methods
    }
}
