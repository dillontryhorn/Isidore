using System;
//using Isidore.Projectors;
using Isidore.Maths;

namespace Isidore.Render
{
    /// <summary>
    /// Represent a CGI infinite plane.  The texture information is scaled
    /// using key frames where the U,V lengths are the distance spanned
    /// before the coordinates repeat
    /// </summary>
    public class Plane : Shape
    {
        #region Fields & Properties

        /// <summary>
        /// Point for locating the plane in local space (Not D of Ax+By+Cz+D=0).
        /// This is also the origin for texturing.
        /// </summary>
        public Point LocalPoint
        {
            get { return localPoint; }
            set
            {
                localPoint = value;
                AdvanceToTime(CurrentTime, true);
            }
        }
        private Point localPoint;

        /// <summary>
        /// Plane normal (A,B, and C in Ax+By+Cz+D=0) in local space
        /// </summary>
        public Normal LocalNormal
        {
            get { return localNormal; }
            set
            {
                localNormal = value;
                AdvanceToTime(CurrentTime, true);
            }
        }
        private Normal localNormal;

        /// <summary>
        ///  D of Ax+By+Cz+D=0 in local space
        /// </summary>
        public double LocalD { get { return localD; } }
        private double localD;

        /// <summary>
        /// Up vector of the plane in local space used to texture mapping.
        /// When set, the input is treated as an approximation and an
        /// orthogonal vector is calculated.
        /// </summary>
        public Vector LocalUp
        {
            get { return localUp; }
            set
            {
                localUp = localNormal.Orthogonal(value);
                AdvanceToTime(CurrentTime, true);
            }
        }
        private Vector localUp;

        /// <summary>
        /// Point for locating the plane at the current time in 
        /// global space (Not D of Ax+By+Cz+D=0)
        /// </summary>
        public Point Point
        {
            get { return globalPoint; }
        }
        private Point globalPoint;

        /// <summary>
        /// Plane normal (A,B, and C in Ax+By+Cz+D=0) in at the
        /// current time in global space
        /// </summary>
        public Normal Normal
        {
            get { return globalNormal; }
        }
        private Normal globalNormal;

        /// <summary>
        ///  D of Ax+By+Cz+D=0 at the current time in global space
        /// </summary>
        public double D { get { return globalD; } }
        private double globalD;

        /// <summary>
        /// Up vector of the plane at the current time in global space
        /// used to texture mapping.  When set, the input is treated
        /// as an approximation and an orthogonal vector is calculated.
        /// </summary>
        public Vector Up
        {
            get { return globalUp; }
        }
        private Vector globalUp;

        /// <summary>
        /// Plane U coordinate length before wrapping.
        /// Uses a key frame so the length can be animated
        /// </summary>
        public Maths.KeyFrame<double> KeyUlength;

        /// <summary>
        /// Plane's U coordinate in global space. It's value is determined via
        /// key frame. Changing this value will rescale the entire key frame
        /// as well.
        /// </summary>
        public double Ulength
        {
            get { return KeyUlength.CurrentValue; }
            set // Not sure if this makes sense
            {
                double factor = value / KeyUlength.CurrentValue;
                KeyUlength.Scale(factor);
                if (!double.IsNaN(CurrentTime))
                    AdvanceToTime(CurrentTime, true);
            }
        }

        /// <summary>
        /// Plane V coordinate length before wrapping.
        /// Uses a key frame so the height can be animated
        /// </summary>
        public Maths.KeyFrame<double> KeyVlength;

        /// <summary>
        /// Plane's U coordinate in global space. It's value is determined via 
        /// key frame. Changing this value will rescale the entire key frame
        /// as well.
        /// </summary>
        public double Vlength
        {
            get { return KeyVlength.CurrentValue; }
            set // Not sure if this makes sense
            {
                double factor = value / KeyVlength.CurrentValue;
                KeyVlength.Scale(factor);
                if (!double.IsNaN(CurrentTime))
                    AdvanceToTime(CurrentTime, true);
            }
        }

        //These are private variables for setting the current time parameters
        // Animated U coordinate length at current time
        private double currUlen;
        // Animated V coordinate length at current time
        private double currVlen;
        // Index of the major axis
        private int majorAxis;
        // Projected vectors (2D space)
        private Vector A, AB, AD, ABpDC, ADpBC;
        // U,V determinants
        private double denU, denV;

        #endregion Fields & Properties
        #region Constructors

        /// <summary>
        /// Plane constructor where the U,V lengths are set via 
        /// key frame.  All parameters are referenced except up
        /// which is used as an approximation to find the true
        /// orthogonal vector to the surface normal.
        /// </summary>
        /// <param name="Point"> Plane's center point </param>
        /// <param name="SurfaceNormal"> Plane's surface normal </param>
        /// <param name="UpDirection"> Plane's up direction 
        /// (approximate) </param>
        /// <param name="KeyUlength"> Planes's animated U coordinate 
        /// length </param>
        /// <param name="KeyVlength"> Plane's animated V coordinate
        /// length </param>
        /// <param name="IntersectBack"> Permits back face 
        /// intersects </param>
        public Plane(Point Point, Normal SurfaceNormal,
            Vector UpDirection, KeyFrame<double> KeyUlength,
            KeyFrame<double> KeyVlength, bool IntersectBack = true)
        {
            // sets local plane
            localPoint = Point ?? Point.Zero();
            localNormal = SurfaceNormal ?? new Normal(0, 0, -1);
            localD = Maths.Plane.findD(localNormal, localPoint);

            // Texturing info
            Vector thisUp = UpDirection ?? new Vector(0, 1, 0);
            // This makes sure the up direction is orthogonal to the normal
            localUp = localNormal.Orthogonal(thisUp);
            this.KeyUlength = KeyUlength ?? new KeyFrame<double>(1.0, 0);
            this.KeyVlength = KeyVlength ?? new KeyFrame<double>(1.0, 0);

            IntersectBackFaces = IntersectBack;
        }

        /// <summary>
        /// Plane constructor where the U,V lengths are hard set.
        /// All parameters are referenced except up
        /// which is used as an approximation to find the true
        /// orthogonal vector to the surface normal.
        /// </summary>
        /// <param name="Point"> Plane's center point </param>
        /// <param name="SurfaceNormal"> Plane's surface normal </param>
        /// <param name="UpDirection"> Plane's up direction 
        /// (approximate) </param>
        /// <param name="Ulength"> Planes's constant U coordinate 
        /// length </param>
        /// <param name="Vlength"> Plane's constant V coordinate
        /// length </param>
        public Plane(Point Point = null, Normal SurfaceNormal = null,
            Vector UpDirection = null, double Ulength = 1, double Vlength = 1) 
            : this(Point, SurfaceNormal, UpDirection,
                new KeyFrame<double>(Ulength, 0),
                new KeyFrame<double>(Vlength, 0))
        { }

        #endregion Constructors
        #region Methods

        /// <summary>
        /// Sets this instance's state vectors to time "now"
        /// </summary>
        /// <param name="now"> Time to set this instance to </param>
        /// <param name="force"> Forces AdvanceToTime to run even if the time is the same </param>
        override public void AdvanceToTime(double now, bool force = false)
        {
            // Checks to see if time is different
            if (!force && now == CurrentTime)
                return;

            // Updates transform time line via virtual definition in Item class
            base.AdvanceToTime(now, force);

            // Updates global plane values
            globalPoint = localPoint.CopyTransform(TransformTimeLine.CurrentValue);
            globalNormal = localNormal.CopyTransform(TransformTimeLine.CurrentValue);
            globalNormal.Normalize();
            globalD = Maths.Plane.findD(globalNormal, globalPoint);

            // And texturing information
            globalUp = localUp.CopyTransform(TransformTimeLine.CurrentValue);
            currUlen = KeyUlength.InterpolateToTime(now);
            currVlen = KeyVlength.InterpolateToTime(now);

            // U,V coordinatesMajor axis if the surface normal
            majorAxis = globalNormal.MajorAxis();
            A = (Vector)globalPoint.CopyReduceComponent(majorAxis);


            // Junk
            AB = -currUlen * globalNormal.Cross(globalUp);
            AB.ReduceComponent(majorAxis);
            AD = currVlen * globalUp.CopyReduceComponent(majorAxis);
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
        /// <returns> Determinate of a and b </returns>
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

            // Uses Maths's plane intersection
            Tuple<double, double> intData = Isidore.Maths.Plane.RayIntersection(
                globalNormal, globalD, (Maths.Ray)ray, IntersectBackFaces);
            double t = intData.Item1;

            // Checks the distance, if longer or negative returns as a miss
            if (double.IsNaN(t) || t < ray.MinimumTravel || 
                t > ray.MaximumTravel ||
                t > ray.IntersectData.Travel) return false;

            // Corrects for back face angle of incidence 
            double cosIncAng = (intData.Item2 < 0) ? -intData.Item2 : 
                intData.Item2;

            // At this point, the plane is the closest intersect
            // So populates the Intersect data instance
            Point intPt = ray.Origin + (Point)ray.Dir * t;

            // Calculates the UV
            // Note that CalculateUV is superseded by an active alpha flag
            double U = double.NaN, V = double.NaN;
            if (UseAlpha || CalculateUV)
            {
                // Finds closest planar axis (A 2D vector)
                Vector M = ((Vector)intPt).CopyReduceComponent(majorAxis);
                Vector AM = M - A;

                // U,V coordinates in distance
                U = Det(AM.Comp, ADpBC.Comp) / denU;
                V = -Det(AM.Comp, ABpDC.Comp) / denV;

                // Wraps coordinates
                U -= Math.Floor(U);
                V -= Math.Floor(V);
            }

            if (!getAlpha(U, V))
                return false;

            // Intersect data
            ShapeSpecificData sData = new ShapeSpecificData(
                globalNormal, cosIncAng, U, V);
            IntersectData iData = new IntersectData(true, t, intPt, this,
                sData);
            
            // Replaces current intersection data with this one
            ray.IntersectData = iData;
            return true;
        }

        /// <summary>
        /// Clones this instance by performing a deep copy
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        new public Plane Clone()
        {
            return (Plane)CloneImp();
        }

        /// <summary>
        /// Deep-copy clones this instance
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        protected override Item CloneImp()
        {
            Plane newCopy = (Plane)MemberwiseClone();

            // Deep copy
            DeepCopyOverride(ref newCopy);

            newCopy.AdvanceToTime(CurrentTime, true);

            return newCopy;
        }

        /// <summary>
        /// Implements deep copies of members that would
        /// otherwise be shallow copied.
        /// </summary>
        /// <param name="copy"> Clone copy </param>
        protected void DeepCopyOverride(ref Plane copy)
        {
            // Base copy
            Shape baseCast = copy; // This is a shallow copy
            DeepCopyOverride(ref baseCast);

            //private Point localPoint;
            copy.localPoint = localPoint.Clone();

            //private Normal localNormal;
            copy.localNormal = localNormal.Clone();

            //private Vector localUp;
            copy.localUp = localUp.Clone();

            //public Maths.KeyFrame<double> KeyUlength;
            copy.KeyUlength = KeyUlength.Clone();

            //public Maths.KeyFrame<double> KeyVlength;
            copy.KeyVlength = KeyVlength.Clone();
        }

        #endregion Methods
    }
}
