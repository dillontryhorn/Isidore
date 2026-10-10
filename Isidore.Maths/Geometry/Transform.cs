using System;

namespace Isidore.Maths
{
    /// <summary>
    /// Basic transform constructors and operations
    /// </summary>
    public partial class Transform
    {
        # region Fields & Properties

        /// <summary>
        /// Forward transform matrix (i.e. local-to-world)
        /// </summary>
        private double[,] m = Distribution.Identity<double>(4);
        /// <summary>
        /// Reverse transform matrix (i.e. world-to-local)
        /// </summary>
        private double[,] im = Distribution.Identity<double>(4);

        /// <summary>
        /// Forward transform matrix (i.e. local-to-world)
        /// </summary>
        public double[,] M { get { return m; } }

        /// <summary>
        /// Reverse transform matrix (i.e. world-to-local)
        /// </summary>
        public double[,] iM { get { return im; } }

        # endregion Fields & Properties
        # region Constructors

        /// <summary>
        /// Identity Constructor
        /// </summary>
        public Transform() { }

        /// <summary>
        /// Constructor using a square double array (usually 4x4)
        /// </summary>
        /// <param name="mat"> transformation array </param>
        public Transform(double[,] mat) 
        {
            // Checks that the input is a 4x4 matrix
            if(mat.Rank != 2 || mat.GetLength(0) != mat.GetLength(1))
                throw new System.ArgumentException(
                    "Array must be square (e.g. 4x4)", "mat");

            m = (double[,])mat.Clone();
            im = Arr.Inverse(m);
        }

        /// <summary>
        /// Constructor using both a matrix and inverse matrix
        /// </summary>
        /// <param name="mat"> Transformation matrix </param>
        /// <param name="imat"> Inverse of the transformation 
        /// matrix </param>
        private Transform(double[,] mat, double[,] imat)
        {
            // Checks that the input is a 4x4 matrix
            if (mat.Rank != 2 || mat.GetLength(0) != mat.GetLength(1))
                throw new System.ArgumentException(
                    "Array must be square (e.g. 4x4)", "mat");

            // Checks that the input is a 4x4 matrix
            if (imat.Rank != 2 || imat.GetLength(0) != imat.GetLength(1))
                throw new System.ArgumentException(
                    "Reverse array must be square (e.g. 4x4)", "mat");

            m = (double[,])mat.Clone();
            im = (double[,])imat.Clone();
        }

        /// <summary>
        /// Constructor copying a transform
        /// </summary>
        /// <param name="t0"> Transform to copy </param>
        public Transform(Transform t0) : this(t0.m, t0.im) { }

        /// <summary>
        /// Constructor using a Quaternion
        /// </summary>
        /// <param name="q"> Quaternion </param>
        public Transform(Quaternion q)
        {

            double xx = q.v.Comp[0] * q.v.Comp[0];
            double yy = q.v.Comp[1] * q.v.Comp[1];
            double zz = q.v.Comp[2] * q.v.Comp[2];
            double xy = q.v.Comp[0] * q.v.Comp[1];
            double xz = q.v.Comp[0] * q.v.Comp[2];
            double yz = q.v.Comp[1] * q.v.Comp[2];
            double xw = q.v.Comp[0] * q.w;
            double yw = q.v.Comp[1] * q.w;
            double zw = q.v.Comp[2] * q.w;

            m[0, 0] = 1.0 - 2.0 * (yy + zz);
            m[1, 0] = 2.0 * (xy + zw);
            m[2, 0] = 2.0 * (xz - yw);
            m[0, 1] = 2.0 * (xy - zw);
            m[1, 1] = 1.0 - 2.0 * (xx + zz);
            m[2, 1] = 2.0 * (yz + xw);
            m[0, 2] = 2.0 * (xz + yw);
            m[1, 2] = 2.0 * (yz - xw);
            m[2, 2] = 1.0 - 2.0 * (xx + yy);

            im = Arr.Inverse(m);
        }

        /// <summary>
        /// Generates an Identity matrix of length N
        /// </summary>
        /// <param name="N"> Length of transform matrix </param>
        public Transform(int N)
        {
            m = Distribution.Identity<double>(N);
            im = Distribution.Identity<double>(N);
        }

        # endregion Constructors
        # region Operators

        /// <summary>
        /// Multiplies two transforms together. 
        /// Functionally t1 applies after t2.
        /// </summary>
        /// <param name="t0"> First transform (A of A*B) </param>
        /// <param name="t1"> Second transform (B of A*B) </param>
        /// <returns> Transform product </returns>
        public static Transform operator *(Transform t0, Transform t1)
        {
            Transform t = new Transform();
            t.m = Arr.MatrixMultiply(t0.m, t1.m);
            t.im = Arr.MatrixMultiply(t1.im, t0.im);
            return t;
        }

        # endregion Operators
        # region Methods

        /// <summary>
        /// Decomposes the matrix
        /// </summary>
        /// <param name="S"> Scalar transform </param>
        /// <param name="R"> Rotation quaternion </param>
        /// <param name="T"> Translation vector </param>
        public void Decompose(out double[,] S,
            out Quaternion R, out Vector T)
        {
            // Translation Decomposition
            T = new Vector(new double[] { m[0, 3], m[1, 3], m[2, 3] });

            // M portion of matrix (no translation)
            double[,] M3x3 = (double[,])m.Clone();
            for (int idx = 0; idx < 3; idx++)
                M3x3[idx, 3] = 0.0;
            M3x3[3, 3] = 1.0;

            // Polar (Rotational) Decomposition 
            double[,] mR = (double[,])M3x3.Clone();
            double magN = Double.PositiveInfinity;
            int count = 0;
            do
            {
                double[,] mRnext =  Distribution.Identity(4);
                double[,] mRinv = Arr.Inverse(Arr.Transpose(mR));

                // Convergence step
                for (int idx0 = 0; idx0 < 4; idx0++)
                    for (int idx1 = 0; idx1 < 4; idx1++)
                        mRnext[idx0, idx1] = 0.5 * (mR[idx0, idx1] +
                            mRinv[idx0, idx1]);

                // Magnitude of normal difference
                magN = 0.0;
                for (int idx = 0; idx < 3; idx++)
                {
                    double mag = Math.Abs(mR[idx, 0] - mRnext[idx, 0]) +
                                 Math.Abs(mR[idx, 1] - mRnext[idx, 1]) +
                                 Math.Abs(mR[idx, 2] - mRnext[idx, 2]);
                    magN = Math.Max(magN, mag);
                }
                mR = (double[,])mRnext.Clone();
            } while (++count < 1000 && magN > 1.0e-6);

            R = new Quaternion(mR);

            //Scalar
            S = Arr.MatrixMultiply(Arr.Inverse(mR), M3x3);
        }

        /// <summary>
        /// Exchanges space transforms. i.e. Local2World becomes World2Local
        /// </summary>
        /// <returns> The transform reciprocal of the input </returns>
        public Transform Reverse()
        {
            return new Transform(im, m);
        }

        /// <summary>
        /// Returns the translation vector as a point
        /// </summary>
        /// <returns> Reference position point </returns>
        public Point GetReferencePoint()
        {
            int len = this.M.GetLength(0); // size of matrix
            Point refPt = new Point(len);
            for(int idx = 0; idx < len; idx++)
                refPt.Comp[idx] = this.M[idx, len-1];
            return refPt;
        }

        /// <summary>
        /// Returns a clone of this transform
        /// </summary>
        /// <returns> Cloned transform </returns>
        public Transform Clone()
        {
            return new Transform(m, im);
        }

        /// <summary>
        /// Checks for equivalence
        /// </summary>
        /// <param name="obj"> .NET object </param>
        /// <returns> Equivalence boolean </returns>
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
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

        # endregion Methods
    }
}
