using System;
using System.Linq;

namespace Isidore.Maths
{
    public partial class Interpolate
    {
        # region Factional interpolation

        /// <summary>
        /// Linearly interpolates between two double data types
        /// </summary>
        /// <param name="v0"> First value </param>
        /// <param name="v1"> Second value </param>
        /// <param name="unitFactor"> Position between the 
        /// two values </param>
        /// <returns> Interpolated values </returns>
        public static double Linear(double v0, double v1, double unitFactor)
        {
            // Simple linear interpolation
            double vVal = v0 + unitFactor * (v1 - v0);
            return vVal;
        }

        /// <summary>
        /// Linearly interpolates between two double data arrays
        /// </summary>
        /// <param name="v0"> First value array </param>
        /// <param name="v1"> Second value array </param>
        /// <param name="unitFactor"> Position between the 
        /// two values </param>
        /// <returns> Interpolated values </returns>
        public static double[] Linear(double[] v0, double[] v1, 
            double unitFactor)
        {
            // Uses the Operator class for most functionality
            double[] dVal = Operator.Add(v0, Operator.Multiply(
                unitFactor, Operator.Subtract(v1, v0)));
            return dVal;
        }

        /// <summary>
        /// Linearly interpolates between two numeric data arrays
        /// </summary>
        /// <param name="v0"> First value </param>
        /// <param name="v1"> Second value </param>
        /// <param name="unitFactor"> Position between the 
        /// two values </param>
        /// <returns> Interpolated values </returns>
        public static double[,] Linear(double[,] v0, double[,] v1, 
            double unitFactor)
        {
            // Uses the Operator class for most functionality
            double[,] dVal = Operator.Add(v0, Operator.Multiply(
                unitFactor, Operator.Subtract(v1, v0)));
            return dVal;
        }

        /// <summary>
        /// Linearly interpolates between two numeric data types
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="v0"> First value </param>
        /// <param name="v1"> Second value </param>
        /// <param name="unitFactor"> Position between the 
        /// two values </param>
        /// <returns> Interpolated values </returns>
        public static T Linear<T>(T v0, T v1, double unitFactor)
        {
            // Converts to double to ensure the most accurate interpolation
            // Can also use Convert.ToDouble(v0);
            double d1 = Operator.Convert<T, double>(v1);
            double d0 = Operator.Convert<T, double>(v0);
            double dVal = d0 + unitFactor * (d1 - d0);
            return Operator.Convert<double, T>(dVal);
        }

        /// <summary>
        /// Linearly interpolates between two numeric data arrays
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="v0"> First value </param>
        /// <param name="v1"> Second value </param>
        /// <param name="unitFactor"> Position between the 
        /// two values </param>
        /// <returns> Interpolated values </returns>
        public static T[] Linear<T>(T[] v0, T[] v1, double unitFactor)
        {
            // Uses the Operator class for most functionality
            // Can also use Convert.ToDouble(v0);
            double[] d1 = Operator.Convert<T, double>(v1);
            double[] d0 = Operator.Convert<T, double>(v0);
            double[] dVal = Operator.Add(d0, Operator.Multiply(unitFactor, 
                Operator.Subtract(d1, d0)));
            return Operator.Convert<double, T>(dVal);
        }

        /// <summary>
        /// Linearly interpolates between two numeric data arrays
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="v0"> First value </param>
        /// <param name="v1"> Second value </param>
        /// <param name="unitFactor"> Position between the 
        /// two values </param>
        /// <returns> Interpolated values </returns>
        public static T[,] Linear<T>(T[,] v0, T[,] v1, double unitFactor)
        {
            // Uses the Operator class for most functionality
            // Can also use Convert.ToDouble(v0);
            double[,] d1 = Operator.Convert<T, double>(v1);
            double[,] d0 = Operator.Convert<T, double>(v0);
            double[,] dVal = Operator.Add(d0, Operator.Multiply(unitFactor, 
                Operator.Subtract(d1, d0)));
            return Operator.Convert<double, T>(dVal);
        }

        /// <summary>
        /// Linear interpolates between two points
        /// </summary>
        /// <param name="p0"> First point </param>
        /// <param name="p1"> Second point </param>
        /// <param name="unitFactor"> Position between the 
        /// two points </param>
        /// <returns> Interpolated vector </returns>
        public static Point Linear(Point p0, Point p1, double unitFactor)
        {
            return p0 + unitFactor * (p1 - p0);
        }

        /// <summary>
        /// Linear interpolates between two vectors
        /// </summary>
        /// <param name="v0"> First vector </param>
        /// <param name="v1"> Second vector </param>
        /// <param name="unitFactor"> Position between the 
        /// two vectors </param>
        /// <returns> Interpolated vector </returns>
        public static Vector Linear(Vector v0, Vector v1, 
            double unitFactor)
        {
            return v0 + unitFactor * (v1 - v0);
        }

        /// <summary>
        /// Linear interpolates between two quaternions.  
        /// A.K.A. Spherical linear Interpolation (SLERP)
        /// </summary>
        /// <param name="q0"> First quaternion </param>
        /// <param name="q1"> Second quaternion </param>
        /// <param name="unitFactor"> Position between the 
        /// two Quaternion </param>
        /// <returns> Interpolated Quaternion </returns>
        public static Quaternion Linear(Quaternion q0, Quaternion q1, 
            double unitFactor)
        {
            double cosAng = Quaternion.Dot(q0, q1);
            // Ensures closest interpolation by limiting to 
            // positive dot products only 
            if (cosAng < 0.0)
            {
                q0 = q0 * (-1.0);
                cosAng = -cosAng;
            }
            if (cosAng > 0.999)
            {
                Quaternion q = q0 * (1.0 - unitFactor) + q1 * unitFactor;
                q.Normalize();
                return q;
            }
            else
            {
                double ang = Math.Acos(Math.Max(Math.Min(cosAng, 1.0), 
                    -1.0));
                double angInt = ang * unitFactor;
                Quaternion qPrep = q1 - q0 * cosAng;
                qPrep.Normalize();
                Quaternion qInt = q0 * Math.Cos(angInt) + qPrep * 
                    Math.Sin(angInt);
                return qInt;
            }
        }

        /// <summary>
        /// Linear interpolates between two transforms using spherical 
        /// interpolation where appropriate.
        /// </summary>
        /// <param name="t0"> First transform </param>
        /// <param name="t1"> Second transform </param>
        /// <param name="unitFactor"> Position between the two 
        /// transforms </param>
        /// <returns> Interpolated transform </returns>
        public static Transform Linear(Transform t0, Transform t1, 
            double unitFactor)
        {
            // Decomposition
            Vector T, T0, T1;
            Quaternion R, R0, R1;
            double[,] S, S0, S1;
            t0.Decompose(out S0, out R0, out T0);
            t1.Decompose(out S1, out R1, out T1);

            // Interpolate
            T = T0 * (1.0 - unitFactor) + T1 * unitFactor;
            R = Linear(R0, R1, unitFactor);
            S = Linear(S0, S1, unitFactor);

            Transform tS = new Transform(S);
            Transform tR = new Transform(R);
            Transform tT = Transform.Translate(T.Comp);

            Transform iTrans = tT * tR * tS;
            return iTrans;
        }

        # endregion Fractional Interpolation

        # region Time Array Interpolation 

        /// <summary>
        /// Returns a linear interpolated double type from an array. 
        /// Time/location point outside the bounds of the array will 
        /// return the closest element.
        /// </summary>
        /// <param name="intPt"> point to interpolate to </param>
        /// <param name="arrPts"> Points corresponding to the array </param>
        /// <param name="arr"> Data array </param>
        /// <returns> Interpolated value </returns>
        public static double Linear(double intPt, double[] arrPts, 
            double[] arr)
        {
            // Finds current location in the time history
            Tuple<int, double> place = PlaceVal(intPt, arrPts);
            int Idx = place.Item1; // Lower bound index
            double fac = place.Item2; // Unit interpolation factor

            // Handles points outside of range
            // Pt lower that first value
            if (double.IsNegativeInfinity(fac)) 
                return arr[0];
            // Pt lower that last value
            else if (double.IsPositiveInfinity(fac))
                return arr.Last();

            // Interpolated point
            return Linear(arr[Idx], arr[Idx + 1], fac);
        }

        /// <summary>
        /// Returns a linear interpolated data type from an array. 
        /// Time/location point outside the bounds of the array will 
        /// return the closest element.
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="intPt"> Point to interpolate to </param>
        /// <param name="arrPts"> Points corresponding to the array </param>
        /// <param name="arr"> Data array </param>
        /// <returns> Interpolated value </returns>
        public static T Linear<T>(double intPt, double[] arrPts, T[] arr)
        {
            // Finds current location in the time history
            Tuple<int, double> place = PlaceVal(intPt, arrPts);
            int Idx = place.Item1; // Lower bound index
            double fac = place.Item2; // Unit interpolation factor

            // Handles points outside of range
            // Pt lower that first value
            if (double.IsNegativeInfinity(fac)) 
                return arr[0];
            // Pt lower that last value
            else if (double.IsPositiveInfinity(fac))
                return arr.Last();

            // Linear interpolation
            return Linear(arr[Idx], arr[Idx + 1], fac);
        }

        /// <summary>
        /// Returns an array of linear interpolated double types 
        /// from an array.  Time/location point outside the bounds 
        /// of the array will return the closest element.
        /// </summary>
        /// <param name="intPts"> point array to interpolate to </param>
        /// <param name="arrPts"> Points corresponding to the array </param>
        /// <param name="arr"> Data array </param>
        /// <returns> Interpolated value array </returns>
        public static double[] Linear(double[] intPts, double[] arrPts, 
            double[] arr)
        {
            double[] ints = new double[intPts.Length];

            // Cycles through each point, calls single point interpolator
            for (int idx = 0; idx < intPts.Length; idx++)
                ints[idx] = Linear(intPts[idx], arrPts, arr);

            return ints;
        }

        /// <summary>
        /// Returns an array of linear interpolated data types from an 
        /// array.  Time/location point outside the bounds of the array 
        /// will return the closest element.
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="intPts"> Points to interpolate to </param>
        /// <param name="arrPts"> Points corresponding to the array </param>
        /// <param name="arr"> Data array </param>
        /// <returns> Interpolated value array </returns>
        public static T[] Linear<T>(double[] intPts, double[] arrPts, 
            T[] arr)
        {
            T[] ints = new T[intPts.Length];

            // Cycles through each point, calls single point interpolator
            for (int idx = 0; idx < intPts.Length; idx++)
                ints[idx] = Linear(intPts[idx], arrPts, arr);

            return ints;
        }

        /// <summary>
        /// Returns a linear interpolated data type from an array. 
        /// Time/location point outside the bounds of the array will 
        /// return the closest element.
        /// </summary>
        /// <typeparam name="T1"> Sample location data type </typeparam>
        /// <typeparam name="T2"> Sample value data type </typeparam>
        /// <param name="idxPt"> Point to interpolate to </param>
        /// <param name="arrPts"> Points corresponding to the array </param>
        /// <param name="arr"> Data array </param>
        /// <returns> Interpolated value </returns>
        public static T2 Linear<T1, T2>(T1 idxPt, T1[] arrPts, T2[] arr)
        {
            // Converts data to doubles
            double idxPtD = Operator.Convert<T1, double>(idxPt);
            double[] arrPtsD = Operator.Convert<T1, double>(arrPts);

            return Linear(idxPtD, arrPtsD, arr);
        }

        /// <summary>
        /// Returns a linear interpolated quaternion type from an array. 
        /// Time/location point outside the bounds of the array will 
        /// return the closest element.
        /// </summary>
        /// <param name="idxPt"> point to interpolate to </param>
        /// <param name="arrPts"> Points corresponding to the array </param>
        /// <param name="arr"> Data array </param>
        /// <returns> Interpolated value </returns>
        public static Quaternion Linear(double idxPt, double[] arrPts, 
            Quaternion[] arr)
        {
            // Finds current location in the time history
            Tuple<int, double> place = PlaceVal(idxPt, arrPts);
            int Idx = place.Item1; // Lower bound index
            double fac = place.Item2; // Unit interpolation factor

            // Handles points outside of range
            // Pt lower that first value
            if (double.IsNegativeInfinity(fac)) 
                return arr[0];
            // Pt lower that last value
            else if (double.IsPositiveInfinity(fac)) 
                return arr.Last();

            // Interpolated point
            return Linear(arr[Idx], arr[Idx + 1], fac);
        }

        /// <summary>
        /// Returns a linear interpolated transform type from an array. 
        /// Time/location point outside the bounds of the array will 
        /// return the closest element.
        /// </summary>
        /// <param name="idxPt"> point to interpolate to </param>
        /// <param name="arrPts"> Points corresponding to the array </param>
        /// <param name="arr"> Data array </param>
        /// <returns> Interpolated value </returns>
        public static Transform Linear(double idxPt, double[] arrPts, 
            Transform[] arr)
        {
            // Finds current location in the time history
            Tuple<int, double> place = PlaceVal(idxPt, arrPts);
            int Idx = place.Item1; // Lower bound index
            double fac = place.Item2; // Unit interpolation factor

            // Handles points outside of range
            // Pt lower that first value
            if (double.IsNegativeInfinity(fac))
                return arr[0];
            // Pt lower that last value
            else if (double.IsPositiveInfinity(fac))
                return arr.Last();

            // Interpolated point
            return Linear(arr[Idx], arr[Idx + 1], fac);
        }

        # endregion Time Array Interpolation

        # region 2D interpolation

        /// <summary>
        /// Returns the linearly interpolated point over a 2D array.
        /// </summary>
        /// <param name="idxPt0"> Interpolated point position in the 
        /// first dimension </param>
        /// <param name="idxPt1"> Interpolated point position in the 
        /// second dimension </param>
        /// <param name="arrPts0"> Points corresponding to the array in the 
        /// first dimension </param>
        /// <param name="arrPts1"> Points corresponding to the array in the 
        /// second dimension </param>
        /// <param name="arr"> Data array </param>
        /// <returns> Interpolated value </returns>
        public static double Linear(double idxPt0, double idxPt1, 
            double[] arrPts0, double[] arrPts1, double[,] arr)
        {
            if (arrPts0.Length != arr.GetLength(0) || arrPts1.Length != arr.GetLength(1))
                throw new ArgumentException("Coordinates must match the data dimensions.", "arr");
            // Finds current location in the time history
            // First axis
            Tuple<int, double> place0 = LinearInterval(idxPt0, arrPts0);
            int Idx0 = place0.Item1; // Lower bound index
            double fac0 = place0.Item2; // Unit interpolation factor
            // Second axis
            Tuple<int, double> place1 = LinearInterval(idxPt1, arrPts1);
            int Idx1 = place1.Item1; // Lower bound index
            double fac1 = place1.Item2; // Unit interpolation factor

            int upper0 = Math.Min(Idx0 + 1, arr.GetLength(0) - 1);
            int upper1 = Math.Min(Idx1 + 1, arr.GetLength(1) - 1);

            // First axis interpolation
            // 1st point
            double val = arr[Idx0, Idx1] * (1.0 - fac0) * (1.0 - fac1);
            // 2nd point
            val += arr[Idx0, upper1] * (1.0 - fac0) * fac1;
            // 3rd point
            val += arr[upper0, Idx1] * fac0 * (1.0 - fac1);
            // 4th point
            val += arr[upper0, upper1] * fac0 * fac1;

            return val;
        }

        // Bilinear interpolation includes its bounding edges, whereas
        // PlaceVal uses infinity to signal clamping in the 1D overloads.
        private static Tuple<int, double> LinearInterval(double point, double[] coordinates)
        {
            if (coordinates.Length == 0 || double.IsNaN(point) ||
                point < coordinates[0] || point > coordinates[coordinates.Length - 1])
                throw new ArgumentException("The point must be within the array bounds.", "point");
            if (coordinates.Length == 1 || point == coordinates[0])
                return Tuple.Create(0, 0.0);
            if (point == coordinates[coordinates.Length - 1])
                return Tuple.Create(coordinates.Length - 2, 1.0);
            return PlaceVal(point, coordinates);
        }

        /// <summary>
        /// Returns the linearly interpolated point over a 2D array.
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="idxPt0"> Interpolated point position in the 
        /// first dimension </param>
        /// <param name="idxPt1"> Interpolated point position in the 
        /// second dimension </param>
        /// <param name="arrPts0"> Points corresponding to the array in the 
        /// first dimension </param>
        /// <param name="arrPts1"> Points corresponding to the array in the 
        /// second dimension </param>
        /// <param name="arr"> Data array </param>
        /// <returns> Interpolated value </returns>
        public static T Linear<T>(double idxPt0, double idxPt1, 
            double[] arrPts0, double[] arrPts1, T[,] arr)
        {
            // Converts to double and sends it to the 
            // generic interpolator above
            double[,] darr = Operator.Convert<T, double>(arr);
            double dval = Linear(idxPt0, idxPt1, arrPts0, arrPts1, darr);

            // Converts back to generic
            T val = Operator.Convert<double, T>(dval);

            return val;
        }

        #endregion 2D interpolation

    }
}
