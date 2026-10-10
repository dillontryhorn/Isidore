using System;
using System.Collections.Generic;
using System.Linq;

namespace Isidore.Maths
{

    /// <summary>
    /// Point is a location in space determined by its components.
    /// </summary>
    public class Point
    {
        #region Fields & Properties
        /// <summary>
        /// Point position component array
        /// </summary>
        public double[] Comp { get; set; }
        /// <summary>
        /// Positional weighting factor
        /// </summary>
        public double w { get; set; }

        #endregion Fields & Properties
        #region Constructors

        /// <summary>
        /// Class constructor.  If left empty a 3D point at the origin is 
        /// created.
        /// </summary>
        /// <param name="position"> Point position </param>
        /// <param name="w"> Point weighting parameter 
        /// (Almost always 1.0) </param>
        public Point(double[] position = null, double w = 1.0)
        {
            Comp = position ?? new double[3];
            this.w = w;
        }

        /// <summary>
        /// Constructor of a point in three dimensional space
        /// </summary>
        /// <param name="p0"> First component position </param>
        /// <param name="p1"> Second component position </param>
        /// <param name="p2"> Third component position </param>
        public Point(double p0, double p1, double p2) :
            this(new double[] { p0, p1, p2 })
        { }

        /// <summary>
        /// Copy Constructor
        /// </summary>
        /// <param name="pt0"> Point to copy </param>
        public Point(Point pt0) : this(pt0.Comp) { w = pt0.w; }

        /// <summary>
        /// Constructor for a point located at the origin of N dimensional 
        /// space
        /// </summary>
        /// <param name="N"> Number of dimensions in which the point 
        /// exists </param>
        public Point(int N)
        {
            Comp = new double[N];
            w = 1.0;
        }

        #endregion Constructors
        #region Operators

        /// <summary>
        /// Adds two Points
        /// </summary>
        /// <param name="p0"> First point </param>
        /// <param name="p1"> Second point </param>
        /// <returns> Point sum </returns>
        static public Point operator +(Point p0, Point p1)
        {
            Point pt = new Point(Maths.Operator.Add(p0.Comp, p1.Comp));
            return pt;
        }

        /// <summary>
        /// Adds a Point and a component array
        /// </summary>
        /// <param name="p0"> First point </param>
        /// <param name="p1"> Second component  </param>
        /// <returns> Point sum </returns>
        static public Point operator +(Point p0, double[] p1)
        {
            Point pt = new Point(Maths.Operator.Add(p0.Comp, p1));
            return pt;
        }

        /// <summary>
        /// Adds a scalar to a Point
        /// </summary>
        /// <param name="p0"> Point </param>
        /// <param name="t"> scalar additive </param>
        /// <returns> Point sum </returns>
        static public Point operator +(Point p0, double t)
        {
            Point pt = new Point(Maths.Operator.Add(p0.Comp, t));
            return pt;
        }

        /// <summary>
        /// Subtracts two Points
        /// </summary>
        /// <param name="p0"> First point </param>
        /// <param name="p1"> Second point </param>
        /// <returns> Point difference </returns>
        static public Point operator -(Point p0, Point p1)
        {
            Point pt = new Point(Maths.Operator.Subtract(p0.Comp, p1.Comp));
            return pt;
        }

        /// <summary>
        /// Subtracts a component array from a Point
        /// </summary>
        /// <param name="p0"> First point </param>
        /// <param name="p1"> Second component array </param>
        /// <returns> Point difference </returns>
        static public Point operator -(Point p0, double[] p1)
        {
            Point pt = new Point(Maths.Operator.Subtract(p0.Comp, p1));
            return pt;
        }

        /// <summary>
        /// Subtracts a component array from a Point
        /// </summary>
        /// <param name="p0"> First component array </param>
        /// <param name="p1"> Second point </param>
        /// <returns> Point difference </returns>
        static public Point operator -(double[] p0, Point p1)
        {
            Point pt = new Point(Maths.Operator.Subtract(p0, p1.Comp));
            return pt;
        }

        /// <summary>
        /// Subtracts a scalar from a Point
        /// </summary>
        /// <param name="p0"> Point </param>
        /// <param name="t"> scalar difference </param>
        /// <returns> Point difference </returns>
        static public Point operator -(Point p0, double t)
        {
            Point pt = new Point(Maths.Operator.Subtract(p0.Comp, t));
            return pt;
        }

        /// <summary>
        /// Multiplies two Points
        /// </summary>
        /// <param name="p0"> First point </param>
        /// <param name="p1"> Second point </param>
        /// <returns> Point product </returns>
        static public Point operator *(Point p0, Point p1)
        {
            Point pt = new Point(Maths.Operator.Multiply(p0.Comp, p1.Comp));
            return pt;
        }

        /// <summary>
        /// Multiplies a Point by a component array
        /// </summary>
        /// <param name="p0"> First point </param>
        /// <param name="p1"> Second component array </param>
        /// <returns> Point product </returns>
        static public Point operator *(Point p0, double[] p1)
        {
            Point pt = new Point(Maths.Operator.Multiply(p0.Comp, p1));
            return pt;
        }

        /// <summary>
        /// Multiplies a point by a scalar
        /// </summary>
        /// <param name="p0"> Point </param>
        /// <param name="t"> scalar multiplicative </param>
        /// <returns> Point product </returns>
        static public Point operator *(Point p0, double t)
        {
            Point pt = new Point(Maths.Operator.Multiply(p0.Comp, t));
            return pt;
        }

        /// <summary>
        /// Multiplies a point by a scalar
        /// </summary>
        /// <param name="t"> scalar multiplicative </param>
        /// <param name="p0"> Point </param>
        /// <returns> Point product </returns>
        static public Point operator *(double t, Point p0)
        {
            return p0 * t;
        }

        /// <summary>
        /// Divides two Points
        /// </summary>
        /// <param name="p0"> First point </param>
        /// <param name="p1"> Second point </param>
        /// <returns> Point divisor </returns>
        static public Point operator /(Point p0, Point p1)
        {
            Point pt = new Point(Maths.Operator.Divide(p0.Comp, p1.Comp));
            return pt;
        }

        /// <summary>
        /// Divides a Point by a component array
        /// </summary>
        /// <param name="p0"> First point </param>
        /// <param name="p1"> Second component array </param>
        /// <returns> Point divisor </returns>
        static public Point operator /(Point p0, double[] p1)
        {
            Point pt = new Point(Maths.Operator.Divide(p0.Comp, p1));
            return pt;
        }

        /// <summary>
        /// Divides a Point by a component array
        /// </summary>
        /// <param name="p0"> First component array </param>
        /// <param name="p1"> Second  point </param>
        /// <returns> Point divisor </returns>
        static public Point operator /(double[] p0, Point p1)
        {
            Point pt = new Point(Maths.Operator.Divide(p0, p1.Comp));
            return pt;
        }

        /// <summary>
        /// Divides a scalar by a point.  The resulting point will have 
        /// every component dimension as the product of the scalar 
        /// divided by the points' components
        /// </summary>
        /// <param name="s0"> scalar </param>
        /// <param name="p1"> Point denominator </param>
        /// <returns> Point </returns>
        static public Point operator /(double s0, Point p1)
        {
            Point pt = new Point(Maths.Operator.Divide(s0, p1.Comp));
            return pt;
        }

        /// <summary>
        /// Divides a Point by a scalar
        /// </summary>
        /// <param name="p0"> Point denominator </param>
        /// <param name="s1"> scalar </param>
        /// <returns> Point division </returns>
        static public Point operator /(Point p0, double s1)
        {
            Point pt = new Point(Maths.Operator.Divide(p0.Comp, s1));
            return pt;
        }

        /// <summary>
        /// Equivalency check operator
        /// </summary>
        /// <param name="p0"> First point </param>
        /// <param name="p1"> Second point </param>
        /// <returns> Equivalence </returns>
        static public bool operator ==(Point p0, Point p1)
        {
            // If both are null, or both are same instance, returns true
            if (System.Object.ReferenceEquals(p0, p1))
                return true;

            // If one is null, but not both, return false
            if (((object)p0 == null) || ((object)p1 == null))
                return false;

            // If the point components are a different size, returns false
            if (p0.Comp.Length != p1.Comp.Length)
                return false;

            // Checks each value in each dimension
            for (int idx = 0; idx < p0.Comp.Length; idx++)
                if (p0.Comp[idx] != p1.Comp[idx])
                    return false;

            // At this point, they match
            return true;
        }

        /// <summary>
        /// Non-equivalency check operator
        /// </summary>
        /// <param name="p0"> First point </param>
        /// <param name="p1"> Second point </param>
        /// <returns> Non-equivalence </returns>
        static public bool operator !=(Point p0, Point p1)
        {
            return !(p0 == p1);
        }

        # endregion Operators
        # region Explicit Operators

        /// <summary>
        /// Casts a point to a vector by copying the values from 
        /// Comp to Comp.
        /// </summary>
        /// <param name="pt"> Point to cast </param>
        /// <returns> Vector with components = position </returns>
        public static explicit operator Vector(Point pt)
        {
            Vector v = new Vector(pt.Comp);
            return v;
        }

        /// <summary>
        /// Casts a point to a normal by copying the values from 
        /// Comp to Comp.
        /// </summary>
        /// <param name="pt"> Point to cast </param>
        /// <returns> Normal with components = position </returns>
        public static explicit operator Normal(Point pt)
        {
            Normal v = new Normal(pt.Comp);
            return v;
        }

        #endregion Explicit Operators
        #region Methods

        /// <summary>
        /// Removes the dimensional component at indexToRemove
        /// for this point.
        /// </summary>
        /// <param name="indexToRemove"> Index of component to 
        /// remove </param>
        public void ReduceComponent(int indexToRemove)
        {
            Comp = Comp.Where((source, index) => index != indexToRemove)
                .ToArray();
        }

        /// <summary>
        /// Copies this point with the dimensional component at 
        /// indexToRemove removed.
        /// </summary>
        /// <param name="indexToRemove"> Index of component to 
        /// remove </param>
        /// <returns> Reduced point </returns>
        public Point CopyReduceComponent(int indexToRemove)
        {
            Point newPt = Clone();
            newPt.ReduceComponent(indexToRemove);
            return newPt;
        }

        /// <summary>
        /// Sets the point position using an double array
        /// </summary>
        /// <param name="posArray"> Cartesian coordinate 
        /// array </param>
        public void setPos(double[] posArray)
        {
            Comp = (double[])posArray.Clone();
        }

        /// <summary>
        /// Sets each coordinate to the lower of two points
        /// </summary>
        /// <param name="pt"> Point for comparison </param>
        public void LowerBound(Point pt)
        {
            for (int idx = 0; idx < Comp.Length; idx++)
                Comp[idx] = (Comp[idx] > pt.Comp[idx]) ? pt.Comp[idx] : 
                    Comp[idx];
        }

        /// <summary>
        /// Sets each coordinate to the higher of two points
        /// </summary>
        /// <param name="pt"> Point for comparison </param>
        public void UpperBound(Point pt)
        {
            for (int idx = 0; idx < this.Comp.Length; idx++)
                Comp[idx] = (Comp[idx] < pt.Comp[idx]) ? pt.Comp[idx] : 
                    Comp[idx];
        }

        /// <summary>
        /// Euclidean distance squared between two points
        /// </summary>
        /// <param name="pt"> Second point </param>
        /// <returns> Distance squared between the points </returns>
        public double DistSquared(Point pt)
        {
            Point sep = this - pt;
            double dist = 0;
            for (int idx = 0; idx < sep.Comp.Length; idx++)
                dist += sep.Comp[idx] * sep.Comp[idx];
            return dist;
        }

        /// <summary>
        /// Euclidean distance between two points
        /// </summary>
        /// <param name="pt"> Second point </param>
        /// <returns> Distance between the points </returns>
        public double Distance(Point pt)
        {
            return Math.Sqrt(DistSquared(pt));
        }

        /// <summary>
        /// Returns a clone of this point by performing a deep copy.
        /// </summary>
        /// <returns> Point's clone </returns>
        public Point Clone()
        {
            // Performs a deep copy
            return new Point(Comp.Clone() as double[], w);

            // MemberwiseClone() performs a shallow copy, so values are
            // referenced (very bad)
            //return this.MemberwiseClone() as Point;
        }

        /// <summary>
        /// Transforms the point using a square matrix
        /// </summary>
        /// <param name="trans"> Transformation instance </param>
        /// <param name="inverse"> Switch for using the inverse 
        /// transform </param>
        public void Transform(Transform trans, bool inverse = false)
        {
            // retrieves the appropriate matrix
            double[,] m;
            if (inverse)
                m = trans.iM;
            else
                m = trans.M;

            int len = m.GetLength(0)-1; // Matrix length minus weight
            double[] tvec = (double[])Comp.Clone();

            // Positional value
            for (int idx0 = 0; idx0 < len; idx0++)
            {
                Comp[idx0] = 0;
                for (int idx1 = 0; idx1 < len; idx1++)
                    Comp[idx0] += tvec[idx1] * m[idx0, idx1];
                Comp[idx0] += m[idx0, len]; // Adds weighting
            }

            // Weighting value
            w = 0.0;
            for (int idx = 0; idx < len; idx++)
                w += tvec[idx] * m[len, idx];
            w += m[len, len];
            if (w != 1)
                for (int idx = 0; idx < len; idx++)
                    Comp[idx] /= w;
        }

        /// <summary>
        /// Applied a transform matrix to a copy of this point
        /// </summary>
        /// <param name="trans"> Transform instance </param>
        /// <param name="inverse"> Switch for using the inverse 
        /// transform </param>
        /// <returns> Copy of this point in m-space </returns>
        public Point CopyTransform(Transform trans, bool inverse = false)
        {
            Point newPt = Clone();
            newPt.Transform(trans, inverse);
            return newPt;
        }

        #endregion Methods
        #region Static Methods

        /// <summary>
        /// Returns a new point located at the origin of N dimensional 
        /// space.  Default is 3D.
        /// </summary>
        /// <param name="N"> Number of dimensions the point exists 
        /// in </param>
        /// <returns> Point instance </returns>
        static public Point Zero(int N = 3)
        {
            return new Point(N);
        }

        /// <summary>
        /// Returns a new point located at NaN of N dimensional space.
        /// Default is 3D.
        /// </summary>
        /// <param name="N"> Number of dimensions the point exists 
        /// in </param>
        /// <returns> Point instance </returns>
        static public Point NaN(int N = 3)
        {
            return new Point(Maths.Distribution.Uniform(N, double.NaN));
        }

        /// <summary>
        /// Returns a new point located at infinity of N dimensional space.
        /// Default is 3D.
        /// </summary>
        /// <param name="N"> Number of dimensions the point exists 
        /// in </param>
        /// <returns> Point instance </returns>
        static public Point PositiveInfinity(int N = 3)
        {
            return new Point(Maths.Distribution.Uniform(N, 
                double.PositiveInfinity));
        }

        /// <summary>
        /// Returns a new point located at -infinity of N dimensional 
        /// space. Default is 3D.
        /// </summary>
        /// <param name="N"> Number of dimensions the point exists 
        /// in </param>
        /// <returns> Point instance </returns>
        static public Point NegativeInfinity(int N = 3)
        {
            return new Point(Maths.Distribution.Uniform(N, 
                double.NegativeInfinity));
        }

        /// <summary>
        /// For a two dimensional NxM array of coordinates returns an array of
        /// N points of dimension M.
        /// </summary>
        /// <param name="coords"> NxM array of coordinates </param>
        /// <returns> A N array of point with coordinates of M 
        /// dimensions </returns>
        static public Point[] Array(double[,] coords)
        {
            // The first dimension is the points
            int plen = coords.GetLength(0);
            // The second dimension is the points coordinates
            int clen = coords.GetLength(1);

            // Generates point array
            Point[] pts = new Point[plen];
            for (int pidx = 0; pidx < plen; pidx++)
            {
                // Extracts the point coordinates
                double[] coord = new double[clen];
                for (int cidx = 0; cidx < clen; cidx++)
                    coord[cidx] = coords[pidx, cidx];
                // Makes each point instance
                pts[pidx] = new Point(coord);
            }

            return pts;
        }


        #endregion Static Methods
        #region Overrides

        /// <summary>
        /// Check for equivalence
        /// </summary>
        /// <param name="obj"> Object (Likely a vector) </param>
        /// <returns> Boolean equivalence </returns>
        public override bool Equals(System.Object obj)
        {
            return Equals(obj as Point);
        }

        /// <summary>
        /// Check for equivalence
        /// </summary>
        /// <param name="p"> Point to compare </param>
        /// <returns> Boolean equivalence </returns>
        public bool Equals(Point p)
        {
            if (ReferenceEquals(this, p))
                return true;
            // If v is null, return false
            if ((object)p == null)
                return false;

            // If the point components are a different size, returns false
            if (Comp.Length != p.Comp.Length)
                return false;

            // Checks each value in each dimension
            for (int idx = 0; idx < Comp.Length; idx++)
                if (Comp[idx] != p.Comp[idx])
                    return false;

            // At this point, it's a match
            return true;
        }

        /// <summary>
        /// Returns the object's hash code. Used with 
        /// Dictionaries and Equals
        /// </summary>
        /// <returns> Object's hash code </returns>
        public override int GetHashCode()
        {
            // Match coordinate equality, which intentionally ignores w.
            unchecked
            {
                int hash = 17;
                foreach (double coordinate in Comp)
                    hash = hash * 31 + coordinate.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Returns string that represents the object.  
        /// An override of Object.ToString()
        /// </summary>
        /// <returns> Object's string </returns>
        public override string ToString()
        {
            return base.ToString();
        }

        #endregion Overrides
    }

    /// <summary>
    /// List of Points
    /// </summary>
    public class Points : List<Point>
    { }
}
