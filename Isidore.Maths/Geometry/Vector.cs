using System;
using System.Linq;

namespace Isidore.Maths
{
    /// <summary>
    /// A physical space vector array for precise geometric applications.
    /// </summary>
    public class Vector
    {
        # region Fields & Properties

        /// <summary>
        /// Components (Of any length)
        /// </summary>
        public double[] Comp { get; set; }

        # endregion Fields & Properties

        # region Constructors

        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="components"> Vector components </param>
        public Vector(double[] components)
        {
            Comp = components;
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="v0"> Vector to copy </param>
        public Vector(Vector v0) : this(v0.Comp) { }

        /// <summary>
        /// Casts a point into a vector
        /// </summary>
        /// <param name="pt"> Point to cast </param>
        public Vector(Point pt) : this(pt.Comp) { }

        /// <summary>
        /// Constructor for creating a vector of zero length spanning 
        /// N dimensional space
        /// </summary>
        /// <param name="N"> Number of dimensions the vector 
        /// spans </param>
        public Vector(int N = 3)
        {
            this.Comp = new double[N];
        }

        /// <summary>
        /// Constructor using three coordinates to make a 3D vector
        /// </summary>
        /// <param name="d0"> Coordinate in dimension 0 </param>
        /// <param name="d1"> Coordinate in dimension 1 </param>
        /// <param name="d2"> Coordinate in dimension 2 </param>
        public Vector(double d0, double d1, double d2) : 
            this(new double[] { d0, d1, d2 }) { }

        # endregion Constructors

        # region Operators

        /// <summary>
        /// Adds two vectors
        /// </summary>
        /// <param name="v0"> first vector </param>
        /// <param name="v1"> second vector </param>
        /// <returns> Vector sum </returns>
        static public Vector operator +(Vector v0, Vector v1)
        {
            return new Vector(Maths.Operator.Add(v0.Comp, v1.Comp));
        }

        /// <summary>
        /// Adds a scalar to a vector
        /// </summary>
        /// <param name="v0"> Vector </param>
        /// <param name="t"> Scalar value </param>
        /// <returns> Vector sum </returns>
        static public Vector operator +(Vector v0, double t)
        {
            Vector v = new Vector(Maths.Operator.Add(v0.Comp, t));
            return v;
        }

        /// <summary>
        /// Adds a scalar to a vector
        /// </summary>
        /// <param name="t"> Scalar value </param>
        /// <param name="v0"> Vector </param>
        /// <returns> Vector sum </returns>
        static public Vector operator +(double t, Vector v0)
        {
            Vector v = new Vector(Maths.Operator.Add(v0.Comp, t));
            return v;
        }


        /// <summary>
        /// Subtracts two values
        /// </summary>
        /// <param name="v0"> First value </param>
        /// <param name="v1"> Second value </param>
        /// <returns> Vector difference </returns>
        static public Vector operator -(Vector v0, Vector v1)
        {
            return new Vector(Maths.Operator.Subtract(v0.Comp, v1.Comp));
        }

        /// <summary>
        /// Multiplies two vectors
        /// </summary>
        /// <param name="v0"> First vector </param>
        /// <param name="v1"> Second vector </param>
        /// <returns> Vector product </returns>
        static public Vector operator *(Vector v0, Vector v1)
        {
            return new Vector(Maths.Operator.Multiply(v0.Comp, v1.Comp));
        }

        /// <summary>
        /// Multiplies a vector by a scalar
        /// </summary>
        /// <param name="t"> Scalar value </param>
        /// <param name="v0"> Vector </param>
        /// <returns> Vector product </returns>
        static public Vector operator *(double t, Vector v0)
        {
            return new Vector(Maths.Operator.Multiply(v0.Comp, t));
        }

        /// <summary>
        /// Multiplies a vector by a scalar
        /// </summary>
        /// <param name="v0"> Vector </param>
        /// <param name="t"> Scalar value </param>
        /// <returns> Vector product </returns>
        static public Vector operator *(Vector v0, double t)
        {
            return t * v0;
        }

        /// <summary>
        /// Divides a vector by another vector
        /// </summary>
        /// <param name="v0"> First vector </param>
        /// <param name="v1"> Second vector </param>
        /// <returns> Product of vector division </returns>
        static public Vector operator /(Vector v0, Vector v1)
        {
            return new Vector(Maths.Operator.Divide(v0.Comp, v1.Comp));
        }

        /// <summary>
        /// Divides a scalar by a vector.  Each component in the returned
        /// vector will be the product of the scalar divided by each vector
        /// component.
        /// </summary>
        /// <param name="s0"> Scalar value </param>
        /// <param name="v1"> Vector </param>
        /// <returns> Vector product of the scalar divided by the 
        /// vector </returns>
        static public Vector operator /(double s0, Vector v1)
        {
            return new Vector(Maths.Operator.Divide(s0, v1.Comp));
        }

        /// <summary>
        /// Divides a vector by a scalar value
        /// </summary>
        /// <param name="v0"> Vector </param>
        /// <param name="s1"> Scalar value </param>
        /// <returns> Vector division </returns>
        static public Vector operator /(Vector v0, double s1)
        {
            return new Vector(Maths.Operator.Divide(v0.Comp, s1));
        }

        /// <summary>
        /// Evaluates equivalence of two Vectors
        /// </summary>
        /// <param name="v0"> First Vector </param>
        /// <param name="v1"> Second Vector </param>
        /// <returns> Boolean equivalence </returns>
        static public bool operator ==(Vector v0, Vector v1)
        {
            // If both are null, or both are same instance, returns true
            if (System.Object.ReferenceEquals(v0, v1))
                return true;

            // If one is null, but not both, return false
            if (((object)v0 == null) || ((object)v1 == null))
                return false;

            // If the point components are a different size, returns false
            if (v0.Comp.Length != v1.Comp.Length)
                return false;
            //if (Maths.Compare.Size<double, double>(v0.Comp, v1.Comp)>0)
                //return false;

            // Checks each value in each dimension
            for (int idx = 0; idx < v0.Comp.Length; idx++)
                if (v0.Comp[idx] != v1.Comp[idx])
                    return false;
                //if (!Operator.Equal(v0.Comp[idx], v1.Comp[idx]))
                //    return false;

            // At this point, they match
            return true;
        }

        /// <summary>
        /// Evaluates difference of two Vectors
        /// </summary>
        /// <param name="v0"> First Vector </param>
        /// <param name="v1"> Second Vector </param>
        /// <returns> Boolean difference </returns>
        static public bool operator !=(Vector v0, Vector v1)
        {
            return !(v0 == v1);
        }

        # endregion

        # region Explicit Operators

        /// <summary>
        /// Casts a vector to a point by copying the values from 
        /// Comp to Comp.
        /// </summary>
        /// <param name="v"> Vector to cast </param>
        /// <returns> Point with position = components </returns>
        public static explicit operator Point(Vector v)
        {
            Point pt = new Point(v.Comp);
            return pt;
        }

        #endregion Operators
        #region Methods

        /// <summary>
        /// Removes the dimensional component at indexToRemove
        /// for this vector.
        /// </summary>
        /// <param name="indexToRemove"> Index of component to 
        /// remove </param>
        public void ReduceComponent(int indexToRemove)
        {
            Comp = Comp.Where(
                (source, index) => index != indexToRemove).ToArray();
        }

        /// <summary>
        /// Copies this vector with the dimensional component at 
        /// indexToRemove removed.
        /// </summary>
        /// <param name="indexToRemove"> Index of component to 
        /// remove </param>
        /// <returns> Reduced point </returns>
        public Vector CopyReduceComponent(int indexToRemove)
        {
            Vector newVec = Clone();
            newVec.ReduceComponent(indexToRemove);
            return newVec;
        }

        /// <summary>
        /// Sets the vector components to vecArr
        /// </summary>
        /// <param name="vecArr"> Vector component </param>
        public void setComp(double[] vecArr)
        {
            Comp = (double[])vecArr.Clone();
        }

        /// <summary>
        /// Returns the vector's squared magnitude
        /// </summary>
        /// <returns> Vector's squared magnitude </returns>
        public double MagSq()
        {
            double mag2 = 0;
            for (int idx = 0; idx < this.Comp.Length; idx++)
                mag2 += this.Comp[idx] * this.Comp[idx];
            return mag2;
        }

        /// <summary>
        /// Returns the vector's magnitude
        /// </summary>
        /// <returns> Vector's magnitude </returns>
        public double Mag()
        {
            return Math.Sqrt(this.MagSq());
        }

        /// <summary>
        /// Returns a vector whose components are the square of 
        /// this vector.
        /// </summary>
        /// <returns> v^2 </returns>
        public Vector Sq()
        {
            double[] ret = new double[this.Comp.Length];
            for (int idx = 0; idx < this.Comp.Length; idx++)
                ret[idx] = this.Comp[idx] * this.Comp[idx];
            return new Vector(ret);
        }

        /// <summary>
        /// Returns a vector whose components are the square root of 
        /// this vector.
        /// </summary>
        /// <returns> v^(1/2) </returns>
        public Vector Sqrt()
        {
            double[] ret = new double[this.Comp.Length];
            for (int idx = 0; idx < this.Comp.Length; idx++)
                ret[idx] = Math.Sqrt(this.Comp[idx]);
            return new Vector(ret);
        }

        /// <summary>
        /// Returns the copy of this vector that has been normalized.
        /// </summary>
        /// <returns> Normalized Vector</returns>
        public Vector CopyNormalize()
        {
            double mag = this.Mag();
            Vector rvec = new Vector(this) / mag; // Makes a clone
            return rvec;
        }

        /// <summary>
        /// Normalizes this Vector
        /// </summary>
        public void Normalize()
        {
            double mag = this.Mag();
            for (int idx = 0; idx < this.Comp.Length; idx++)
                this.Comp[idx] /= mag;
        }

        /// <summary>
        /// Returns the dot product of this and another vector
        /// </summary>
        /// <param name="v1"> Second vector </param>
        /// <returns> Dot product </returns>
        public double Dot(Vector v1)
        {
            return Dot(this, v1);
        }

        /// <summary>
        /// Returns the cross product of this and another vector
        /// </summary>
        /// <param name="v1"> Second vector </param>
        /// <returns> Cross product </returns>
        public Vector Cross(Vector v1)
        {
            return Cross(this, v1);
        }

        /// <summary>
        /// Given approximate orthogonal vector, returns the true
        /// orthogonal vector to this vector
        /// </summary>
        /// <param name="v1approx"> Orthogonal approximation </param>
        /// <returns> True orthogonal vector </returns>
        public Vector Orthogonal(Vector v1approx)
        {
            Vector v2 = v1approx.Cross(this);
            v2.Normalize();
            return Cross(v2) as Vector;
        }

        /// <summary>
        /// Returns the minimum value for each dimension of two vectors
        /// </summary>
        /// <param name="v0"> First vector </param>
        /// <param name="v1"> Second vector </param>
        /// <returns> Minimum vector </returns>
        public Vector Min(Vector v0, Vector v1)
        {
            Vector v = v0.Clone();
            for (int idx = 0; idx < v0.Comp.Length; idx++)
                v.Comp[idx] = (v.Comp[idx] > v1.Comp[idx]) ? 
                    v1.Comp[idx] : v.Comp[idx];
            return v;
        }

        /// <summary>
        /// Returns the minimum value for each dimension of this and 
        /// another vector
        /// </summary>
        /// <param name="v1"> Second vector </param>
        /// <returns> Minimum vector </returns>
        public Vector Min(Vector v1)
        {
            return Min(this, v1);
        }

        /// <summary>
        /// Returns the maximum value for each dimension of two vectors
        /// </summary>
        /// <param name="v0"> First vector </param>
        /// <param name="v1"> Second vector </param>
        /// <returns> Maximum vector </returns>
        public Vector Max(Vector v0, Vector v1)
        {
            Vector v = v0.Clone();
            for (int idx = 0; idx < v0.Comp.Length; idx++)
                v.Comp[idx] = (v.Comp[idx] < v1.Comp[idx]) ? 
                    v1.Comp[idx] : v.Comp[idx];
            return v;
        }

        /// <summary>
        /// Returns the maximum value for each dimension of this and 
        /// another vector
        /// </summary>
        /// <param name="v1"> Second vector </param>
        /// <returns> Maximum vector </returns>
        public Vector Max(Vector v1)
        {
            return Max(this, v1);
        }

        /// <summary>
        /// Applies the transformation matrix to the vector.  
        /// The matrix is a member of Transform.
        /// </summary>
        /// <param name="trans"> Transformation instance </param>
        /// <param name="inverse"> Switch for using the inverse 
        /// transform </param>
        public virtual void Transform(Transform trans, bool inverse = false)
        {
            // retrieves the appropriate matrix
            double[,] m;
            if(inverse)
                m = trans.iM;
            else
                m = trans.M;

            double[] tvec = (double[])Comp.Clone();
            int len = m.GetLength(0) - 1; // Matrix length minus weight

            // Composite vector
            for (int idx0 = 0; idx0 < len; idx0++)
            {
                Comp[idx0] = 0;
                for (int idx1 = 0; idx1 < len; idx1++)
                    Comp[idx0] += tvec[idx1] * m[idx0, idx1];
            }
        }

        /// <summary>
        /// Applies the transformation matrix to a copy of this vector.  
        /// The matrix is a member of Transform.
        /// </summary>
        /// <param name="trans"> Transform instance </param>
        /// <param name="inverse"> Switch for using the 
        /// inverse transform </param>
        /// <returns> A copy of this vector in m-space </returns> 
        public Vector CopyTransform(Transform trans, bool inverse = false)
        {
            Vector newVec = Clone();
            newVec.Transform(trans, inverse);
            return newVec;
        }

        /// <summary>
        /// Specular reflection vector
        /// </summary>
        /// <param name="Norm"> Vector representing the 
        /// surface normal </param>
        /// <returns> Reflected vector </returns>
        public Vector Reflected(Vector Norm)
        {
            // Real-time rendering, pg. 230, Intro to ray-tracing
            double factor = 2 * Norm.Dot(this);
            return this - factor * Norm;
        }

        /// <summary>
        /// Find the largest, or major, axis component of the vector
        /// </summary>
        /// <returns> The axis of greatest magnitude. </returns>
        public int MajorAxis()
        {
            int maj = 0; // Major axis index
            double val = Math.Abs(Comp[0]); // Major axis length
            for (int idx = 1; idx < Comp.Length; idx++)
            {
                double thisVal = Math.Abs(Comp[idx]);
                // If this length is greater than the previous major axis,
                // replaces values
                if (thisVal > val)
                {
                    val = thisVal;
                    maj = idx;
                }
            }
            return maj;
        }

        /// <summary>
        /// Returns a clone of this vector by performing a deep copy.
        /// </summary>
        /// <returns> Vector's clone </returns>
        public Vector Clone()
        {
            // Performs a deep copy
            return new Vector(Comp.Clone() as double[]);
        }

        # endregion Methods

        # region Static Methods

        /// <summary>
        /// Returns the cross product of two vectors in three dimension
        /// </summary>
        /// <param name="v0"> First vector </param>
        /// <param name="v1"> Second vector </param>
        /// <returns> Cross product </returns>
        static public Vector Cross(Vector v0, Vector v1)
        {
            return new Vector(new double[]{v0.Comp[1] * v1.Comp[2] - 
                v0.Comp[2] * v1.Comp[1], v0.Comp[2] * v1.Comp[0] - 
                v0.Comp[0] * v1.Comp[2], v0.Comp[0] * v1.Comp[1] - 
                v0.Comp[1] * v1.Comp[0]});
        }

        /// <summary>
        /// Returns the dot product of two vectors
        /// </summary>
        /// <param name="v0"> First vector </param>
        /// <param name="v1"> Second vector </param>
        /// <returns> Dot product</returns>
        static public double Dot(Vector v0, Vector v1)
        {
            double prod = 0;
            for (int idx = 0; idx < v0.Comp.Length; idx++)
                prod += v0.Comp[idx] * v1.Comp[idx];
            return prod;
        }

        /// <summary>
        /// Given a vector and an approximate orthogonal vector, 
        /// returns the true orthogonal vector
        /// </summary>
        /// <param name="v0"> "Normal" vector </param>
        /// <param name="v1approx"> Orthogonal approximation </param>
        /// <returns> True orthogonal vector </returns>
        static public Vector Orthogonal(Vector v0, Vector v1approx)
        {
            Vector v2 = v1approx.Cross(v0);
            v2.Normalize();
            return v0.Cross(v2) as Vector;
        }

        /// <summary>
        /// Returns a new vector of zero length in N dimensional space.
        /// </summary>
        /// <param name="N"> Number of dimensions the vector 
        /// crosses </param>
        /// <returns> Vector instance </returns>
        static public Vector Zero(int N = 3)
        {
            return new Vector(new double[N]);
        }

        /// <summary>
        /// Returns a new vector with NaN lengths in N 
        /// dimensional space.
        /// </summary>
        /// <param name="N"> Number of dimensions the 
        /// vector crosses </param>
        /// <returns> Vector instance </returns>
        static public Vector NaN(int N = 3)
        {
            return new Vector(
                Maths.Distribution.Uniform(N, double.NaN));
        }

        /// <summary>
        ///  Returns a new vector with infinite lengths in 
        ///  N dimensional space.
        /// </summary>
        /// <param name="N"> Number of dimensions the 
        /// vector crosses </param>
        /// <returns> Vector instance </returns>
        static public Vector PositiveInfinity(int N = 3)
        {
            return new Vector(Maths.Distribution.Uniform(N, 
                double.PositiveInfinity));
        }

        /// <summary>
        /// Returns a new vector with negative infinite lengths 
        /// in N dimensional space.
        /// </summary>
        /// <param name="N"> Number of dimensions the vector 
        /// crosses </param>
        /// <returns> Vector instance </returns>
        static public Vector NegativeInfinity(int N = 3)
        {
            return new Vector(Maths.Distribution.Uniform(N, 
                double.NegativeInfinity));
        }

        /// <summary>
        /// Return a unit vector aligned to a provided axis
        /// </summary>
        /// <param name="N"> Number of dimensions the vector 
        /// crosses </param>
        /// <param name="Axis"> Axis the vector is aligned to </param>
        /// <returns> The axis-aligned unit vector </returns>
        static public Vector Unit(int N = 3, int Axis = 0)
        {
            Vector v = new Vector(new double[N]);
            v.Comp[Axis] = 1;
            return v;
        }

        # endregion Static Methods

        # region Overrides

        /// <summary>
        /// Check for equivalence
        /// </summary>
        /// <param name="obj"> Object (Likely a vector) </param>
        /// <returns> Boolean equivalence </returns>
        public override bool Equals(System.Object obj)
        {
            return Equals(obj as Vector);
        }

        /// <summary>
        /// Check for equivalence
        /// </summary>
        /// <param name="v"> Vector to compare </param>
        /// <returns> Boolean equivalence </returns>
        public bool Equals(Vector v)
        {
            if (ReferenceEquals(this, v))
                return true;
            // If v is null, return false
            if ((object)v == null)
                return false;

            // If the point components are a different size, 
            // returns false
            if (this.Comp.Length != v.Comp.Length)
                return false;

            // Checks each value in each dimension
            for (int idx = 0; idx < this.Comp.Length; idx++)
                if (this.Comp[idx] != v.Comp[idx])
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
            unchecked
            {
                int hash = 17;
                foreach (double coordinate in Comp)
                    hash = hash * 31 + coordinate.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Returns the object's name
        /// </summary>
        /// <returns> Object's name </returns>
        public override string ToString()
        {
            return base.ToString();
        }

        # endregion Overrides
    }
}
