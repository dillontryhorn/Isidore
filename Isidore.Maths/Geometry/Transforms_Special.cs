using System;

namespace Isidore.Maths
{
    /// <summary>
    /// Specialized spatial transforms
    /// </summary>
    public partial class Transform
    {

        /// <summary>
        /// Rotational transformation around an arbitrary axis
        /// </summary>
        /// <param name="Radian"> Radians of rotation </param>
        /// <param name="Axis"> Axis of rotation </param>
        /// <returns> Rotation transform </returns>
        public static Transform Rotate(double Radian, Vector Axis)
        {
            Axis.Normalize();
            double cosA = Math.Cos(Radian);
            double sinA = Math.Sin(Radian);

            double[,] m = Distribution.Identity(4);

            m[0, 0] = Axis.Comp[0] * Axis.Comp[0] + 
                (1 - Axis.Comp[0] * Axis.Comp[0]) * cosA;
            m[0, 1] = Axis.Comp[0] * Axis.Comp[1] * (1 - cosA) - Axis.Comp[2] * sinA;
            m[0, 2] = Axis.Comp[0] * Axis.Comp[2] * (1 - cosA) + Axis.Comp[1] * sinA;
            m[0, 3] = 0;

            m[1, 0] = Axis.Comp[0] * Axis.Comp[1] * (1 - cosA) + Axis.Comp[2] * sinA;
            m[1, 1] = Axis.Comp[1] * Axis.Comp[1] + 
                (1 - Axis.Comp[1] * Axis.Comp[1]) * cosA;
            m[1, 2] = Axis.Comp[1] * Axis.Comp[2] * (1 - cosA) - Axis.Comp[0] * sinA;
            m[1, 3] = 0;

            m[2, 0] = Axis.Comp[0] * Axis.Comp[2] * (1 - cosA) - Axis.Comp[1] * sinA;
            m[2, 1] = Axis.Comp[1] * Axis.Comp[2] * (1 - cosA) + Axis.Comp[0] * sinA;
            m[2, 2] = Axis.Comp[2] * Axis.Comp[2] + 
                (1 - Axis.Comp[2] * Axis.Comp[2]) * cosA;
            m[2, 3] = 0;

            m[3, 0] = 0;
            m[3, 1] = 0;
            m[3, 2] = 0;
            m[3, 3] = 1;

            return new Transform(m, Arr.Transpose(m));
        }

        /// <summary>
        /// PointTo transformation using a vector to point.
        /// Aligns the local coordinate system to the world coordinate.
        /// This is useful for pointing a local space to a global point.
        /// </summary>
        /// <param name="dir"> Camera pointing direction </param>
        /// <param name="up"> Camera up vector </param>
        /// <returns> PointTo transform </returns>
        public static Transform PointTo(Vector dir, Vector up)
        {
            // Watts & Policarpo, 3D Games, pg. 174-179 or
            // Watts & Watts, Adv. Animation & Rendering Tech., pg. 7-9
            // Initialize columns
            double[,] dm = new double[4, 4];
            dir.Normalize();
            up.Normalize();
            Vector left = up.Cross(dir);
            left.Normalize();
            Vector newUp = dir.Cross(left);
            dm[0, 0] = left.Comp[0];
            dm[1, 0] = left.Comp[1];
            dm[2, 0] = left.Comp[2];
            dm[3, 0] = 0;
            dm[0, 1] = newUp.Comp[0];
            dm[1, 1] = newUp.Comp[1];
            dm[2, 1] = newUp.Comp[2];
            dm[3, 1] = 0;
            dm[0, 2] = dir.Comp[0];
            dm[1, 2] = dir.Comp[1];
            dm[2, 2] = dir.Comp[2];
            dm[3, 2] = 0;
            dm[0, 3] = 0;
            dm[1, 3] = 0;
            dm[2, 3] = 0;
            dm[3, 3] = 1;

            return new Transform(dm);
        }

        /// <summary>
        /// PointTo transformation using a point as the pointing focus.  
        /// Aligns the local coordinate system to the world coordinate.
        /// This is useful for pointing a local space to a global point.
        /// </summary>
        /// <param name="pos"> Camera position </param>
        /// <param name="look"> Look at point </param>
        /// <param name="up"> Camera up vector </param>
        /// <returns> LookAt transform </returns>
        public static Transform PointTo(Point pos, Point look, Vector up)
        {
            Vector dir = new Vector(look - pos);
            return PointTo(dir, up);
        }

        /// <summary>
        /// LocalCoord transform is a rigid body transform that establishes
        /// a local coordinate system in global space.  All spatial data is  
        /// oriented to this local
        /// frame of reference.
        /// </summary>
        /// <param name="origin"> Local coordinate origin </param>
        /// <param name="axis0"> 1st (X) axis of the local coordinate </param>
        /// <param name="axis1"> 2nd (Y) axis of the local coordinate </param>
        /// <returns> LocalCoord transform </returns>
        public static Transform LocalCoord(Point origin, Vector axis0, Vector axis1)
        {
            // This is my formulation for converting from global to local
            // coordinate space similar to that in CAD

            // Finds the third axis and refines the second
            axis0.Normalize();
            axis1.Normalize();
            Vector axis2 = axis0.Cross(axis1);
            axis2.Normalize();
            Vector newAxis1 = axis2.Cross(axis0);

            double[,] dm = new double[4, 4];
            dm[0, 0] = axis0.Comp[0];
            dm[1, 0] = axis0.Comp[1];
            dm[2, 0] = axis0.Comp[2];
            dm[3, 0] = 0;
            dm[0, 1] = newAxis1.Comp[0];
            dm[1, 1] = newAxis1.Comp[1];
            dm[2, 1] = newAxis1.Comp[2];
            dm[3, 1] = 0;
            dm[0, 2] = axis2.Comp[0];
            dm[1, 2] = axis2.Comp[1];
            dm[2, 2] = axis2.Comp[2];
            dm[3, 2] = 0;
            dm[0, 3] = origin.Comp[0];
            dm[1, 3] = origin.Comp[1];
            dm[2, 3] = origin.Comp[2];
            dm[3, 3] = 1;

            return new Transform(dm);
        }

        /// <summary>
        /// LookAt transformation using a vector to point.
        /// LookAt transforms world coordinates into viewer coordinates.
        /// </summary>
        /// <param name="pos"> Camera position </param>
        /// <param name="dir"> Camera pointing direction </param>
        /// <param name="up"> Camera up vector </param>
        /// <returns> LookAt transform </returns>
        public static Transform LookAt(Point pos, Vector dir, Vector up)
        {
            // Initialize columns
            double[,] dm = new double[4, 4];
            dir.Normalize();
            up.Normalize();
            Vector left = up.Cross(dir);
            left.Normalize();
            Vector newUp = dir.Cross(left);
            dm[0, 0] = left.Comp[0];
            dm[1, 0] = left.Comp[1];
            dm[2, 0] = left.Comp[2];
            dm[3, 0] = 0;
            dm[0, 1] = newUp.Comp[0];
            dm[1, 1] = newUp.Comp[1];
            dm[2, 1] = newUp.Comp[2];
            dm[3, 1] = 0;
            dm[0, 2] = dir.Comp[0];
            dm[1, 2] = dir.Comp[1];
            dm[2, 2] = dir.Comp[2];
            dm[3, 2] = 0;
            dm[0, 3] = pos.Comp[0];
            dm[1, 3] = pos.Comp[1];
            dm[2, 3] = pos.Comp[2];
            dm[3, 3] = 1;

            return new Transform(dm);
        }

        /// <summary>
        /// LookAt transformation using a point as the pointing focus
        /// </summary>
        /// <param name="pos"> Camera position </param>
        /// <param name="look"> Look at point </param>
        /// <param name="up"> Camera up vector </param>
        /// <returns> LookAt transform </returns>
        public static Transform LookAt(Point pos, Point look, Vector up)
        {
            Vector dir = new Vector(look - pos);
            return LookAt(pos, dir, up);
        }

        /// <summary>
        /// Transform derived from Euler angles
        /// </summary>
        /// <param name="X"> X-axis/Pitch rotation [Radians] </param>
        /// <param name="Y"> Y-axis/Yaw rotation [Radians] </param>
        /// <param name="Z"> Z-axis/Roll rotation [Radians] </param>
        /// <returns> Euler transform </returns>
        public static Transform Euler(double X, double Y, double Z)
        {
            Transform rX = RotX(X);
            Transform rY = RotY(Y);
            Transform rZ = RotZ(Z);
            return rZ * rY * rX;
        }
    }
}
