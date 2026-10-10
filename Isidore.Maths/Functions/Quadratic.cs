using System;

namespace Isidore.Maths
{
    public partial class Function
    {
        /// <summary>
        /// Finds the roots of the quadratic equation 
        /// Ax^2 + Bx + C = 0
        /// </summary>
        /// <param name="A"> Quadratic coefficient </param>
        /// <param name="B"> Linear coefficient </param>
        /// <param name="C"> Free term </param>
        /// <returns> The two roots </returns>
        static public double[] Quadratic(double A, double B, double C)
        {
            // A degenerate quadratic is a linear equation.
            if (A == 0)
            {
                double root = B == 0 ? double.NaN : -C / B;
                return new double[] { root, root };
            }
            // Finds square of discriminant
            double D = B * B - 4.0 * A * C;

            // A negative discriminant has no real roots.
            if (D < 0 || double.IsNaN(D))
                return new double[]{double.NaN, double.NaN};

            if (D == 0)
            {
                double root = -B / (2.0 * A);
                return new double[] { root, root };
            }

            // Now calculates the discriminant
            D = Math.Sqrt(D);

            // This approach help negate small number error when B ~ D
            // Quadratic portion of the solution
            double quad = -0.5*(B + (B >= 0 ? D : -D));
            double t0 = quad / A; // B << or on the order of A and C
            double t1 = C / quad; // B >> A or C

            // Swaps so smallest is returned first
            if (t0 > t1)
                Function.Swap(ref t0, ref t1);

            // Returns tuple
            return new double[]{t0, t1};
        }
    }
}
