using System;
using Isidore.Maths;

namespace Isidore.Render
{
    /// <summary>
    /// Represents a spherical quadric shape.  Sphere differs for other shapes
    /// in that only its position is scaled by a transformation matrix.
    /// The radius is scaled via its own key frame.  This is very helpful
    /// in many computational physics applications where the radius 
    /// represents an boundary.
    /// </summary>
    public class Sphere : Shape
    {
        #region Fields & Properties

        /// <summary>
        /// Provides ray/sphere intersection algorithm
        /// </summary>
        protected Maths.Sphere sphere;

        /// <summary>
        /// Sphere's center or radius, effectively the transformation
        /// matrix translation
        /// </summary>
        protected Point center;

        /// <summary>
        /// Sphere's center of radius at this time.
        /// If set, the point is cloned
        /// </summary>
        public Point Center
        {
            // Returns the current
            get { return sphere.Center; }
            // Updates the center and sphere  fields
            set
            {
                center = value.Clone(); // Deep copy
                // Could have done this manually, but this looks nice
                AdvanceToTime(CurrentTime, true);
            }
        }

        /// <summary>
        /// Sphere's polar axis in local space
        /// </summary>
        public Vector LocalPole;

        /// <summary>
        /// Sphere's polar axis in global space
        /// </summary>
        private Vector globalPole;

        /// <summary>
        /// Sphere's polar axis in global space
        /// </summary>
        public Vector Pole
        {
            get { return globalPole; }
        }

        /// <summary>
        /// Sphere's equatorial axis in local space
        /// </summary>
        public Vector LocalEquator;

        /// <summary>
        /// Sphere's equatorial axis in global space
        /// </summary>
        private Vector globalEquator;

        /// <summary>
        /// Sphere's equatorial axis in global space
        /// </summary>
        public Vector Equator
        {
            get { return globalEquator; }
        }

        /// <summary>
        /// Sphere's radius.  
        /// Uses a key frame so the size can be animated
        /// </summary>
        public Maths.KeyFrame<double> radius;

        /// <summary>
        /// Sphere's radius in global space. It's value is determined via 
        /// key frame. Changing this value will rescale the entire key frame
        /// as well.
        /// </summary>
        public double Radius
        {
            get { return radius.CurrentValue; }
            set // Not sure if this makes sense
            {
                double factor = value / radius.CurrentValue;
                radius.Scale(factor);
                if (!double.IsNaN(CurrentTime))
                    AdvanceToTime(CurrentTime, true);
            }
        }

        # endregion Fields & Properties
        #region Constructors

        /// <summary>
        /// Sphere constructor where the radius is set via key frame.
        /// All parameters are referenced.
        /// </summary>
        /// <param name="Center"> Sphere's center of radius </param>
        /// <param name="Radius"> Sphere's animated radius </param>
        /// <param name="Pole">  Sphere's pole vector </param>
        /// <param name="Equator">  Sphere's equator vector </param>
        /// <param name="IntersectBack"> Permits back face 
        /// intersects </param>
        public Sphere(Point Center, KeyFrame<double> Radius, Vector Pole, 
            Vector Equator, bool IntersectBack)
        {
            // Will not work with .Clone(), since it doesn't exist in a null
            center = Center ?? Point.Zero(3);
            radius = Radius ?? new KeyFrame<double>(1.0, 0.0);
            LocalPole = Pole ?? Vector.Unit(3, 1);
            LocalEquator = Equator ?? Vector.Unit(3, 0);
            IntersectBackFaces = IntersectBack;
        }

        /// <summary>
        /// Sphere constructor where the radius is hard set.
        /// All parameters are referenced.
        /// </summary>
        /// <param name="Center"> Sphere's center of radius </param>
        /// <param name="Radius"> Sphere's constant radius </param>
        /// <param name="Pole"> Sphere's pole vector </param>
        /// <param name="Equator"> Sphere's equator vector </param>
        /// <param name="IntersectBack"> Permits back face 
        /// intersects </param>
        public Sphere(Point Center = null, double Radius = 1.0, 
            Vector Pole = null, Vector Equator = null, 
            bool IntersectBack = true): this(Center, 
                new KeyFrame<double>(Radius, 0.0), Pole, Equator,
                IntersectBack)
        {    
        }

        #endregion Constructors
        # region Methods

        /// <summary>
        /// Sets this instance's state vectors to time "now"
        /// </summary>
        /// <param name="now"> Time to set this instance to </param>
        /// <param name="force"> Forces AdvanceToTime to run even if the time
        /// is the same </param>
        override public void AdvanceToTime(double now = 0, bool force = false)
        {
            // Checks to see if time is different
            if (!force && now == CurrentTime)
                return;

            // Updates transform time line via virtual definition in Shape class
            base.AdvanceToTime(now, force);

            // Finds the center, pole, equator, and radius for the current time
            globalEquator = LocalEquator.
                CopyTransform(TransformTimeLine.CurrentValue);
            globalPole = LocalPole.
                CopyTransform(TransformTimeLine.CurrentValue);
            globalEquator.Normalize();
            globalPole.Normalize();
            Point currCenter = center.
                CopyTransform(TransformTimeLine.CurrentValue);
            double currRad = radius.InterpolateToTime(now);

            // Updates the current copy (in global space)
            sphere = new Maths.Sphere(currCenter, currRad);
        }

        /// <summary>
        /// Performs an intersection check and operation with a ray.
        /// Sphere operates in global space.
        /// </summary>
        /// <param name="ray"> Ray to evaluate for intersection </param>
        /// <returns> Intersect flag (true = intersection) </returns>
        override public bool Intersect(ref RenderRay ray)
        {
            // If the shape isn't on, it should count as a miss
            if (!On)
                return false;

            // Ray/Sphere intersection
            // The sphere class in Isidore.Maths operates in world space 
            // and returns two intersection points (Near & Far)
            // Items: 1) propagation distance, 2) cosine angle  of incidence,
            // 3) intersection point, 4) surface normal
            Tuple<double[], double[], Point[], Normal[]> intRay = sphere.RayIntersection(ray);

            // Intersection data
            IntersectData iData = new IntersectData();

            // Cycles through both intersections (Spheres have two intersections)
            bool noInt = true; // Flag for no intersection
            int idx = 0;
            while(noInt & idx<2)
            {
                // There's a hit if the travel is greater than the minimum travel distance
                // But less than the current travel
                if(intRay.Item1[idx] > ray.MinimumTravel &&
                    intRay.Item1[idx] < ray.IntersectData.Travel &&
                    intRay.Item1[idx] <= ray.MaximumTravel)
                {
                    // If there's a valid, closer hit, then the UV coordinate is found
                    // Note that CalculateUV is superseded by an active alpha flag
                    double U = double.NaN, V = double.NaN;
                    if (UseAlpha || CalculateUV)
                    {
                        // Finds the intersect normal in lengths of radius
                        Vector Norm = (Vector)(intRay.Item3[idx] - sphere.Center);
                        Norm /= Radius;
                        // V akin to latitude
                        double v = Math.Acos(Math.Max(-1.0,
                            Math.Min(1.0, -Norm.Dot(Pole))));
                        // atan2 also defines a finite longitude at the poles,
                        // where dividing by sin(latitude) is undefined.
                        double u = Math.Atan2(Pole.Cross(Equator).Dot(Norm),
                            Equator.Dot(Norm)) / (2.0 * Math.PI);
                        v /= Math.PI;
                        if (u <= 0)
                            u += 1;
                        U = u;
                        V = v;
                    }

                    // Uses the UV coordinates to see if the alpha value
                    // Finds the alpha value from the UV coordinates
                    bool alphaHit = getAlpha(U, V);

                    // If these's an alpha flag, then the target has been hit
                    if (alphaHit)
                    {
                        // Checks if we've hit a back face, if so, checks back face tag
                        if (intRay.Item2[idx] >= 0 || (IntersectBackFaces))
                        {
                            // Create an intersect data instance
                            ShapeSpecificData sData = new ShapeSpecificData(
                                intRay.Item4[idx], intRay.Item2[idx], U, V);
                            iData = new IntersectData(true, intRay.Item1[idx],
                                intRay.Item3[idx], this, sData);

                            noInt = false; // Indicates intersection, aborts search
                        }
                    }
                }
                idx++;
            }

            // If there's been an intersection, replaces the ray's intersection data
            if (iData.Hit)
            {
                ray.IntersectData = iData;
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Clones this instance by performing a deep copy
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        new public Sphere Clone()
        {
            return CloneImp() as Sphere;
        }

        /// <summary>
        /// Deep-copy clones this instance
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        protected override Item CloneImp()
        {
            Sphere newCopy = (Sphere)MemberwiseClone();

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
        protected void DeepCopyOverride(ref Sphere copy)
        {
            // Base copy
            Shape baseCast = copy; // This is a shallow copy
            DeepCopyOverride(ref baseCast);

            //protected Maths.Sphere sphere;
            if (sphere != null)
                copy.sphere = sphere.Clone();

            //protected Point center;
            copy.center = center.Clone();

            //public Vector LocalPole;
            copy.LocalPole = LocalPole.Clone();

            //public Vector LocalEquator;
            copy.LocalEquator = LocalEquator.Clone();

            //public Maths.KeyFrame<double> radius;
            copy.radius = radius.Clone();
        }

        # endregion Methods
    }
}
