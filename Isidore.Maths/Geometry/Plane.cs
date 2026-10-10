using System;

namespace Isidore.Maths
{
    /// <summary>
    /// A mathematical definition of a plane
    /// </summary>
    public class Plane
    {
        # region Fields & Properties

        /// <summary>
        /// Point for locating the plane in space (Not D of Ax+By+Cz+D=0)
        /// </summary>
        private Point point;
        /// <summary>
        /// Point for locating the plane in space (Not D of Ax+By+Cz+D=0)
        /// Planer normal (A, B, and C)
        /// </summary>
        private Normal normal;
        /// <summary>
        ///  D of Ax+By+Cz+D=0
        /// </summary>
        private double d;

        /// <summary>
        /// Point for locating the plane in space (Not D of Ax+By+Cz+D=0).
        /// If set, the point is cloned.
        /// </summary>
        public Point Point
        {
            get { return point; }
            set { point = value.Clone(); findD(); }
        }
        /// <summary>
        /// Point for locating the plane in space (Not D of Ax+By+Cz+D=0)
        /// Planer normal (A, B, and C). If set, the normal is cloned.
        /// </summary>
        public Normal Normal
        {
            get { return normal; }
            set { normal = value.Clone(); findD(); }
        }
        /// <summary>
        ///  D of Ax+By+Cz+D=0
        /// </summary>
        public double D { get { return d; } }

        # endregion Fields & Properties
        # region Constructors

        /// <summary>
        /// Constructor.  The planar normal is automatically normalized.
        /// </summary>
        /// <param name="Point"> Planar anchor point </param>
        /// <param name="Normal"> Planar normal </param>
        public Plane(Point Point, Normal Normal)
        {
            if(Point.Comp.Length != Normal.Comp.Length)
                throw new System.ArgumentException("Plane point and " +
                    "normal must have the same number of dimensions");
            point = Point.Clone();
            normal = Normal.Clone();
            findD();
        }

        #endregion Constructors
        #region Methods
        /// <summary>
        /// Finds the D of Ax+By+Cz+D=0 from the anchor point and surface 
        /// normal.  The surface normal is automatically normalized.
        /// </summary>
        /// <param name="normal"> Planer normal (A, B, and C) </param>
        /// <param name="point"> Point for locating the plane in space 
        /// (Not D of Ax+By+Cz+D=0) </param>
        /// <returns> The D of Ax+By+Cz+D=0 </returns>
        public static double findD(Normal normal, Point point)
        {
            normal.Normalize(); // Makes sure we have a unit vector normal
            double d = 0;
            for (int idx = 0; idx < normal.Comp.Length; idx++)
                d -= point.Comp[idx] * normal.Comp[idx];
            return d;
        }

        /// <summary>
        /// Finds the D of Ax+By+Cz+D=0 from the anchor point and surface 
        /// normal.  The surface normal is automatically normalized.
        /// </summary>
        private void findD()
        {
            d = findD(normal, point);
        }

        /// <summary>
        /// Finds the sign dependent distance from a point to a plane.
        /// Negative distances indicate the point is on the 
        /// opposite the side containing the normal 
        /// </summary>
        /// <param name="Plane"> Plane </param>
        /// <param name="Point"> Point </param>
        /// <returns> Distance from point to plane. </returns>
        public static double DistanceToPoint(Plane Plane, Point Point)
        {
            double dist = Plane.Normal.Dot((Normal)Point) + Plane.D;
            return dist;
        }

        /// <summary>
        /// Finds the sign dependent distance from a point to a plane.
        /// Negative distances indicate the point is on the 
        /// opposite the side containing the normal 
        /// </summary>
        /// <param name="Point"> Point </param>
        /// <returns> Distance from point to plane. </returns>
        public double DistanceToPoint(Point Point)
        {
            return DistanceToPoint(this, Point);
        }

        /// <summary>
        /// Finds the travel distance for a ray to intersect this plane
        /// </summary>
        /// <param name="ray"> Ray to intersect </param>
        /// <param name="backFaceIntersects"> Permits back face 
        /// intersects </param>
        /// <returns> Tuple containing the propagation distance, cosine of 
        /// angle of incidence, intersection point, and the surface 
        /// normal </returns>
        public Tuple<double, double, Point, Normal> RayIntersection(
            Ray ray, bool backFaceIntersects = true)
        {
            return RayIntersection(this, ray, backFaceIntersects);
        }

        /// <summary>
        /// Finds the travel distance for a ray to intersect a plane
        /// </summary>
        /// <param name="Plane"> Plane to be intersected </param>
        /// <param name="ray"> Ray to intersect </param>
        /// <param name="backFaceIntersects"> Permits back face 
        /// intersects </param>
        /// <returns> Tuple containing the propagation distance, cosine of 
        /// angle of incidence, intersection point, and the surface 
        /// normal </returns>
        public static Tuple<double, double, Point, Normal> RayIntersection
            (Plane Plane, Ray ray, bool backFaceIntersects = true)
        {
            // An Intro. to Ray Tracing, Glassner, pg 51
            Tuple<double, double> iData = RayIntersection(Plane.Normal, Plane.D, ray,
                backFaceIntersects);

            // Intersection point
            Point intPt;

            // if the travel distance is NaN, then the ray missed the plane
            // or hit the back face (If not allowed)
            if (double.IsNaN(iData.Item1))
                intPt = Point.NaN(Plane.Point.Comp.Length);
            // Infinite intersection point    
            else if(double.IsPositiveInfinity(iData.Item1))
                intPt = Point.PositiveInfinity(Plane.Point.Comp.Length);
            // Hit in real space
            else
                intPt = ray.Propagate(iData.Item1);

            return new Tuple<double, double, Point, Normal>(iData.Item1, 
                iData.Item2, intPt, Plane.Normal.Clone());
        }

        /// <summary>
        /// Calculates the intersection distance between a ray and plane,
        /// If NaN, then the ray missed the plane
        /// </summary>
        /// <param name="ABC"> Plane surface normal </param>
        /// <param name="D"> D of Ax + By + Cy + D = 0 </param>
        /// <param name="ray"> Ray to intersect </param>
        /// <param name="backFaceIntersects"> Permits back face 
        /// intersects </param>
        /// <returns> Tuple containing the propagation distance, 
        /// cosine incidence angle </returns>
        public static Tuple<double, double> RayIntersection(Normal ABC, 
            double D, Ray ray, bool backFaceIntersects = true)
        {
            // An Intro. to Ray Tracing, Glassner, pg 51
            // with modifications
            // Incidence angle cosine
            double cosIncAng = -ABC.Dot(ray.Dir);

            // Back face check (also triggers if the plane is behind the ray
            // location and interface
            if (!backFaceIntersects && cosIncAng < 0)
                return new Tuple<double, double>(double.NaN, double.NaN);

            // Runs parallel to the plane
            if (cosIncAng == 0)
                return new Tuple<double, double>(double.PositiveInfinity,
                    cosIncAng);

            // Distance to intersect
            double t = (ABC.Dot(new Normal(ray.Origin.Comp)) + D) / 
                cosIncAng;
            if(t <= 0)
                return new Tuple<double, double>(double.NaN, cosIncAng);
            else
                return new Tuple<double, double>(t, cosIncAng);
        }

        /// <summary>
        /// Applies the transformation class to this plane.  
        /// </summary>
        /// <param name="trans"> Transformation instance </param>
        /// <param name="inverse"> Switch for using the inverse 
        /// transform </param>
        public void Transform(Transform trans, bool inverse = false)
        {
            point.Transform(trans, inverse);
            normal.Transform(trans, inverse);
            findD();
        }

        /// <summary>
        /// Applies the transformation matrix to a copy of this ray.
        /// The matrix is a member of Transform (trans.M or trans.IM).
        /// </summary>
        /// <param name="trans"> Transformation instance </param>
        /// <param name="inverse"> Switch for using the inverse 
        /// transform </param>
        /// <returns> A copy of this vector in M-space </returns>
        public Plane CopyTransform(Transform trans, bool inverse = false)
        {
            Plane newPlane = Clone();
            newPlane.Transform(trans, inverse);
            return newPlane;
        }

        /// <summary>
        /// Returns a clone of this plane via deep copy
        /// </summary>
        /// <returns> The cloned copy </returns>
        public Plane Clone()
        {
            return new Plane(point, normal);
        }

        # endregion Methods
    }
}
