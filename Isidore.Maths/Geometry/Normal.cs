namespace Isidore.Maths
{
    /// <summary>
    /// Normal represents a surface normal and 
    /// is a child of the Vector class.
    /// It is similar to Vector except how 
    /// transformations are handled, 
    /// which are scaled according to
    /// the orientation of the surface.
    /// </summary>
    public class Normal:Vector
    {
        # region Fields & Properties

        # endregion Fields & Properties
        # region Constructors

        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="components"> Normal components </param>
        public Normal(double[] components)
        {
            Comp = components;
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="n0"> Normal to copy </param>
        public Normal(Normal n0) : this(n0.Comp) { }

        /// <summary>
        /// Casts a vector into a normal
        /// </summary>
        /// <param name="vec"> Vector to cast </param>
        public Normal(Vector vec) : this(vec.Comp) { }

        /// <summary>
        /// Constructor for creating a normal of zero length spanning N 
        /// dimensional space
        /// </summary>
        /// <param name="N"> Number of dimensions the normal spans </param>
        public Normal(int N = 3)
        {
            Comp = new double[N];
        }

        /// <summary>
        /// Constructor using three coordinates to make a 3D normal
        /// </summary>
        /// <param name="d0"> Coordinate in dimension 0 </param>
        /// <param name="d1"> Coordinate in dimension 1 </param>
        /// <param name="d2"> Coordinate in dimension 2 </param>
        public Normal(double d0, double d1, double d2) : 
            this(new double[] { d0, d1, d2 }) { }

        # endregion Constructors

        # region Operators

        /// <summary>
        /// Adds two normals
        /// </summary>
        /// <param name="v0"> first normal </param>
        /// <param name="v1"> second normal </param>
        /// <returns> Normal sum </returns>
        static public Normal operator +(Normal v0, Normal v1)
        {
            return new Normal(Maths.Operator.Add(v0.Comp, v1.Comp));
        }

        /// <summary>
        /// Adds a scalar to a normal
        /// </summary>
        /// <param name="v0"> Normal </param>
        /// <param name="t"> Scalar value </param>
        /// <returns> Normal sum </returns>
        static public Normal operator +(Normal v0, double t)
        {
            Normal v = new Normal(Maths.Operator.Add(v0.Comp, t));
            return v;
        }

        /// <summary>
        /// Adds a scalar to a normal
        /// </summary>
        /// <param name="t"> Scalar value </param>
        /// <param name="v0"> Normal </param>
        /// <returns> Normal sum </returns>
        static public Normal operator +(double t, Normal v0)
        {
            Normal v = new Normal(Maths.Operator.Add(v0.Comp, t));
            return v;
        }


        /// <summary>
        /// Subtracts two values
        /// </summary>
        /// <param name="v0"> First value </param>
        /// <param name="v1"> Second value </param>
        /// <returns> Normal difference </returns>
        static public Normal operator -(Normal v0, Normal v1)
        {
            return new Normal(Maths.Operator.Subtract(v0.Comp, v1.Comp));
        }

        /// <summary>
        /// Multiplies two normals
        /// </summary>
        /// <param name="v0"> First normal </param>
        /// <param name="v1"> Second normal </param>
        /// <returns> Normal product </returns>
        static public Normal operator *(Normal v0, Normal v1)
        {
            return new Normal(Maths.Operator.Multiply(v0.Comp, v1.Comp));
        }

        /// <summary>
        /// Multiplies a normal by a scalar
        /// </summary>
        /// <param name="t"> Scalar value </param>
        /// <param name="v0"> Normal </param>
        /// <returns> Normal product </returns>
        static public Normal operator *(double t, Normal v0)
        {
            return new Normal(Maths.Operator.Multiply(v0.Comp, t));
        }

        /// <summary>
        /// Multiplies a normal by a scalar
        /// </summary>
        /// <param name="v0"> Normal </param>
        /// <param name="t"> Scalar value </param>
        /// <returns> Normal product </returns>
        static public Normal operator *(Normal v0, double t)
        {
            return t * v0;
        }

        /// <summary>
        /// Divides a normal by another normal
        /// </summary>
        /// <param name="v0"> First normal </param>
        /// <param name="v1"> Second normal </param>
        /// <returns> Product of normal division </returns>
        static public Normal operator /(Normal v0, Normal v1)
        {
            return new Normal(Maths.Operator.Divide(v0.Comp, v1.Comp));
        }

        /// <summary>
        /// Divides a scalar by a normal.  Each component in the returned
        /// normal will be the product of the scalar divided by each normal
        /// component.
        /// </summary>
        /// <param name="s0"> Scalar value </param>
        /// <param name="v1"> Normal </param>
        /// <returns> Normal product of the scalar divided by the 
        /// normal </returns>
        static public Normal operator /(double s0, Normal v1)
        {
            return new Normal(Maths.Operator.Divide(s0, v1.Comp));
        }

        /// <summary>
        /// Divides a normal by a scalar value
        /// </summary>
        /// <param name="v0"> Normal </param>
        /// <param name="s1"> Scalar value </param>
        /// <returns> Normal division </returns>
        static public Normal operator /(Normal v0, double s1)
        {
            return new Normal(Maths.Operator.Divide(v0.Comp, s1));
        }

        /// <summary>
        /// Evaluates equivalence of two Normals
        /// </summary>
        /// <param name="n0"> First Normal </param>
        /// <param name="n1"> Second Normal </param>
        /// <returns> Boolean equivalence </returns>
        static public bool operator ==(Normal n0, Normal n1)
        {
            // If both are null, or both are same instance, returns true
            if (ReferenceEquals(n0, n1))
                return true;

            // If one is null, but not both, return false
            if (((object)n0 == null) || ((object)n1 == null))
                return false;

            // If the point components are a different size, returns false
            if (n0.Comp.Length != n1.Comp.Length)
                return false;

            // Checks each value in each dimension
            for (int idx = 0; idx < n0.Comp.Length; idx++)
                if (n0.Comp[idx] != n1.Comp[idx])
                    return false;

            // At this point, they match
            return true;
        }

        /// <summary>
        /// Evaluates difference of two Normals
        /// </summary>
        /// <param name="v0"> First Normal </param>
        /// <param name="v1"> Second Normal </param>
        /// <returns> Boolean difference </returns>
        static public bool operator !=(Normal v0, Normal v1)
        {
            return !(v0 == v1);
        }

        # endregion

        # region Explicit Operators

        /// <summary>
        /// Casts a normal to a point by copying the values from 
        /// Comp to Comp.
        /// </summary>
        /// <param name="v"> Normal to cast </param>
        /// <returns> Point with position = components </returns>
        public static explicit operator Point(Normal v)
        {
            Point pt = new Point(v.Comp);
            return pt;
        }

        # endregion Expilicit Operators

        # region Methods

        /// <summary>
        /// Returns a normal whose components are the square of this normal.
        /// </summary>
        /// <returns> v^2 </returns>
        new public Normal Sq()
        {
            // This ensures the return is a Normal & not a Vector
            return new Normal(base.Sq());
        }

        /// <summary>
        /// Returns a normal whose components are the square root of 
        /// this normal.
        /// </summary>
        /// <returns> normal square root </returns>
        new public Normal Sqrt()
        {
            return new Normal(base.Sqrt());
        }

        /// <summary>
        /// Returns the copy of this normal that has been normalized.
        /// </summary>
        /// <returns> Normalized Normal</returns>
        new public Normal CopyNormalize()
        {
            return new Normal(base.CopyNormalize());
        }

        /// <summary>
        /// Returns the cross product of this and another normal
        /// </summary>
        /// <param name="v1"> Second normal </param>
        /// <returns> Cross product </returns>
        public Normal Cross(Normal v1)
        {
            return new Normal(base.Cross(v1));
        }

        /// <summary>
        /// Given approximate orthogonal vector, returns the true
        /// orthogonal vector to this normal
        /// </summary>
        /// <param name="v1approx"> Orthogonal approximation </param>
        /// <returns> True orthogonal vector </returns>
        new public Vector Orthogonal(Vector v1approx)
        {
            return Orthogonal(this, v1approx);
        }

        /// <summary>
        /// Returns the minimum value for each dimension of two normals
        /// </summary>
        /// <param name="v0"> First normal </param>
        /// <param name="v1"> Second normal </param>
        /// <returns> Minimum normal </returns>
        public Normal Min(Normal v0, Normal v1)
        {
            return new Normal(base.Min(v0, v1));

        }

        /// <summary>
        /// Returns the minimum value for each dimension of this and another 
        /// normal
        /// </summary>
        /// <param name="v1"> Second normal </param>
        /// <returns> Minimum normal </returns>
        public Normal Min(Normal v1)
        {
            return Min(this, v1);
        }

        /// <summary>
        /// Returns the maximum value for each dimension of two normals
        /// </summary>
        /// <param name="v0"> First normal </param>
        /// <param name="v1"> Second normal </param>
        /// <returns> Maximum normal </returns>
        public Normal Max(Normal v0, Normal v1)
        {
            return new Normal(base.Max(v0, v1));
        }

        /// <summary>
        /// Returns the maximum value for each dimension of this and another 
        /// normal
        /// </summary>
        /// <param name="v1"> Second normal </param>
        /// <returns> Maximum normal </returns>
        public Normal Max(Normal v1)
        {
            return Max(this, v1);
        }

        /// <summary>
        /// Transforms this normal by the inverse transpose of the point
        /// transformation, preserving its perpendicular relationship to surfaces.
        /// </summary>
        /// <param name="trans"> Transformation instance </param>
        /// <param name="inverse"> Use the reverse transformation </param>
        public override void Transform(Transform trans, bool inverse = false)
        {
            double[,] matrix = inverse ? trans.M : trans.iM;
            int dimensions = matrix.GetLength(0) - 1;
            double[] original = (double[])Comp.Clone();
            for (int row = 0; row < dimensions; row++)
            {
                Comp[row] = 0;
                for (int column = 0; column < dimensions; column++)
                    Comp[row] += original[column] * matrix[column, row];
            }
        }

        /// <summary>
        /// Applies the transformation matrix to a copy of this normal.
        /// The matrix is a member of Transform.
        /// </summary>
        /// <param name="trans"> Transform instance </param>
        /// <param name="inverse"> Switch for using the inverse 
        /// transform </param>
        /// <returns> A copy of this normal in m-space </returns> 
        new public Normal CopyTransform(Transform trans, 
            bool inverse = false)
        {
            Normal newVec = Clone();
            newVec.Transform(trans, inverse);
            return newVec;
        }

        /// <summary>
        /// Returns a clone of this normal by performing a deep copy.
        /// </summary>
        /// <returns> Normal's clone </returns>
        new public Normal Clone()
        {
            // Performs a deep copy
            return new Normal(Comp.Clone() as double[]);

            // MemberwiseClone() performs a shallow copy, 
            // so values are referenced (very bad)
            //return (Normal)MemberwiseClone();
        }

        # endregion Methods

        # region Static Methods

        /// <summary>
        /// Returns the cross product of two normals in three dimension
        /// </summary>
        /// <param name="v0"> First normal </param>
        /// <param name="v1"> Second normal </param>
        /// <returns> Cross product </returns>
        static public Normal Cross(Normal v0, Normal v1)
        {
            return new Normal(Vector.Cross(v0, v1));
        }

        /// <summary>
        /// Returns the dot product of two normals
        /// </summary>
        /// <param name="v0"> First normal </param>
        /// <param name="v1"> Second normal </param>
        /// <returns> Dot product</returns>
        static public double Dot(Normal v0, Normal v1)
        {
            // Casts Normals to vectors
            return Dot(v0 as Vector, v1 as Vector);
        }

        /// <summary>
        /// Given a normal and an approximate orthogonal vector, 
        /// returns the true orthogonal vector
        /// </summary>
        /// <param name="v0"> normal </param>
        /// <param name="v1approx"> Orthogonal approximation </param>
        /// <returns> True orthogonal vector </returns>
        public Vector Orthogonal(Normal v0, Vector v1approx)
        {
            return (Orthogonal(v0 as Vector, v1approx));
        }

        /// <summary>
        /// Returns a new normal of zero length in N dimensional space.
        /// </summary>
        /// <param name="N"> Number of dimensions the normal 
        /// crosses </param>
        /// <returns> Normal instance </returns>
        new static public Normal Zero(int N = 3)
        {
            return new Normal(new double[N]);
        }

        /// <summary>
        /// Returns a new normal with NaN lengths in N dimensional space.
        /// </summary>
        /// <param name="N"> Number of dimensions the normal 
        /// crosses </param>
        /// <returns> Normal instance </returns>
        new static public Normal NaN(int N = 3)
        {
            return new Normal(Maths.Distribution.Uniform(N, double.NaN));
        }

        /// <summary>
        /// Return a unit normal aligned to a provided axis
        /// </summary>
        /// <param name="N"> Number of dimensions the normal 
        /// crosses </param>
        /// <param name="Axis"> Axis the normal is aligned to </param>
        /// <returns> The axis-aligned unit normal </returns>
        new static public Normal Unit(int N = 3, int Axis = 0)
        {
            Normal v = new Normal(new double[N]);
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
            return base.Equals(obj);
        }

        /// <summary>
        /// Check for equivalence
        /// </summary>
        /// <param name="n"> Normal to compare </param>
        /// <returns> Boolean equivalence </returns>
        public bool Equals(Normal n)
        {
            return base.Equals(n);
        }

        /// <summary>
        /// Returns the object's hash code. Used with 
        /// Dictionaries and Equals
        /// </summary>
        /// <returns> Object's hash code </returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
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
