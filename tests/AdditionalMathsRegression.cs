using System;
using System.Collections.Generic;
using Isidore.Maths;

internal static class AdditionalMathsRegression
{
    public static void Run()
    {
        StableArrayStatistics();
        RotationAndIdentityTransforms();
        NormalResultsAndTransforms();
        GeometricEqualityAndHashing();
        BilinearBoundaries();
        RectangularAndEmptyArrayOperations();
        InverseInputShape();
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }

    private static void Near(double expected, double actual, string message)
    {
        if (double.IsNaN(actual) || double.IsInfinity(actual) ||
            Math.Abs(expected - actual) > 1e-10 * Math.Max(1, Math.Abs(expected)))
            throw new Exception(message + ": expected " + expected + ", received " + actual);
    }

    private static void Throws<T>(Action action, string message) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new Exception(message);
    }

    private static void StableArrayStatistics()
    {
        double[] values = { 1e12 + 1, 1e12 + 2, 1e12 + 3 };
        double variance = 2.0 / 3.0;
        Near(variance, Stats.Variance(values)[0], "Variance must retain small differences on a large offset");
        Near(Math.Sqrt(variance), Stats.STD(values)[0], "Standard deviation must use stable array variance");
        Check(Stats.Variance(values)[1] == 1e12 + 2, "Stable variance mean must retain its large offset");
        Near(variance, Stats.Variance(new[,] { { values[0], values[1], values[2] } })[0],
            "Matrix variance must retain small differences");
        Near(Math.Sqrt(variance), Stats.STD(new[,] { { values[0], values[1], values[2] } })[0],
            "Matrix standard deviation must retain small differences");
        double[] tagged = { values[0], double.PositiveInfinity, values[1], values[2] };
        bool[] tags = { true, false, true, true };
        Near(variance, Stats.Variance(tagged, tags)[0], "Tagged variance must ignore excluded non-finite values");
        Near(Math.Sqrt(variance), Stats.STD(tagged, tags)[0], "Tagged standard deviation must be stable");
        double[,] matrix = { { values[0], double.PositiveInfinity }, { values[1], values[2] } };
        bool[,] matrixTags = { { true, false }, { true, true } };
        Near(variance, Stats.Variance(matrix, matrixTags)[0], "Tagged matrix variance must be stable");
        Near(Math.Sqrt(variance), Stats.STD(matrix, matrixTags)[0], "Tagged matrix standard deviation must be stable");
        long[] integers = { 1000000000001, 1000000000002, 1000000000003 };
        Near(variance, Stats.Variance<long>(integers)[0], "Generic variance must use stable accumulation");
        Near(Math.Sqrt(variance), Stats.STD<long>(integers)[0], "Generic standard deviation must use stable accumulation");
        Near(variance, Stats.Variance<long>(integers, new[] { true, true, true })[0],
            "Generic tagged variance must be stable");
        Near(0, Stats.Variance(new[] { 1e200, 1e200 })[0], "A constant array must not overflow its squared values");
        Near(0, Stats.STD(new[] { 1e200, 1e200 })[0], "A large constant array must have zero standard deviation");
        Array.Reverse(values);
        Near(variance, Stats.Variance(values)[0], "Stable statistics must also handle decreasing samples");
        double[] wideRange = { 1e308, -1e308, 0 };
        double[] wideStats = Stats.Variance(wideRange);
        Check(double.IsPositiveInfinity(wideStats[0]) && wideStats[1] == 0,
            "Widely separated finite samples must preserve a finite mean and overflow variance to positive infinity");
        wideStats = Stats.STD(wideRange);
        Check(double.IsPositiveInfinity(wideStats[0]) && wideStats[1] == 0,
            "Overflow variance must produce infinite standard deviation rather than NaN");
    }

    private static void RotationAndIdentityTransforms()
    {
        Transform zero = Transform.Rotate(0, new Vector(1, 2, 3));
        for (int row = 0; row < 4; row++)
            for (int column = 0; column < 4; column++)
                Near(row == column ? 1 : 0, zero.M[row, column], "A zero-angle rotation must be identity");
        Transform axisAligned = Transform.Rotate(0.7, new Vector(0, 0, 1));
        Transform expected = Transform.RotZ(0.7);
        for (int row = 0; row < 4; row++)
            for (int column = 0; column < 4; column++)
                Near(expected.M[row, column], axisAligned.M[row, column], "Arbitrary-axis rotation must match Z rotation");
        Vector axis = new Vector(1, 2, 3);
        Transform rotation = Transform.Rotate(1.1, axis.Clone());
        Vector original = new Vector(2, -1, 4);
        Vector rotated = original.CopyTransform(rotation);
        Near(original.Mag(), rotated.Mag(), "Rotation must preserve vector magnitude");
        Vector restored = rotated.CopyTransform(rotation, true);
        for (int component = 0; component < 3; component++)
        {
            Near(original.Comp[component], restored.Comp[component], "Rotation inverse must restore every component");
            Near(axis.Comp[component], axis.CopyTransform(rotation).Comp[component], "The rotation axis must remain fixed");
        }
        for (int size = 2; size <= 6; size++)
        {
            Transform identity = new Transform(size);
            Check(identity.iM.GetLength(0) == size, "Identity and inverse matrices must have matching dimensions");
            double[] coordinates = new double[size - 1];
            for (int component = 0; component < coordinates.Length; component++)
                coordinates[component] = component + 1;
            Point point = new Point(coordinates);
            Check(point.CopyTransform(identity, true).Equals(point), "A non-4D identity inverse must preserve points");
            Transform composed = identity * identity;
            Check(composed.iM.GetLength(0) == size, "Non-4D identity transforms must compose");
        }
    }

    private static void NormalResultsAndTransforms()
    {
        Normal normal = new Normal(2, 3, 6);
        Normal squared = normal.Sq();
        Check(squared != null, "Normal.Sq must return a normal");
        Near(36, squared.Comp[2], "Squared normal component");
        Normal root = squared.Sqrt();
        Check(root != null && root.Equals(normal), "Normal.Sqrt must return the component square roots");
        Normal normalized = normal.CopyNormalize();
        Check(normalized != null, "Normal.CopyNormalize must return a normal");
        Near(1, normalized.Mag(), "Copied normal must be normalized");
        Near(7, normal.Mag(), "CopyNormalize must preserve the original normal");
        Normal cross = new Normal(1, 0, 0).Cross(new Normal(0, 1, 0));
        Check(cross != null && cross.Equals(new Normal(0, 0, 1)), "Instance normal cross product must return a normal");
        cross = Normal.Cross(new Normal(1, 0, 0), new Normal(0, 1, 0));
        Check(cross != null && cross.Equals(new Normal(0, 0, 1)), "Static normal cross product must return a normal");
        Normal minimum = normal.Min(new Normal(4, 1, 7));
        Normal maximum = normal.Max(new Normal(4, 1, 7));
        Check(minimum != null && minimum.Equals(new Normal(2, 1, 6)), "Normal minimum must return component minima");
        Check(maximum != null && maximum.Equals(new Normal(4, 3, 7)), "Normal maximum must return component maxima");
        Check(normal.Equals(new Normal(2, 3, 6)), "Component minima and maxima must preserve the original normal");
        Transform scale = Transform.Scale(2, 3, 4);
        Normal surfaceNormal = new Normal(1, 1, 0);
        Vector tangent = new Vector(1, -1, 0);
        Normal transformed = surfaceNormal.CopyTransform(scale);
        Near(0.5, transformed.Comp[0], "A normal must transform by inverse transpose");
        Near(1.0 / 3, transformed.Comp[1], "A normal must compensate for nonuniform scale");
        Near(0, transformed.Dot(tangent.CopyTransform(scale)), "Transformed normals must remain perpendicular to transformed surfaces");
        Normal inverse = transformed.CopyTransform(scale, true);
        Check(inverse.Equals(surfaceNormal), "Inverse normal transformation must restore the original components");
        Vector baseReference = surfaceNormal.Clone();
        baseReference.Transform(scale);
        Near(0.5, baseReference.Comp[0], "Normal transformation must dispatch through a Vector reference");
        Check(surfaceNormal.CopyTransform(Transform.Translate(4, 5, 6)).Equals(surfaceNormal),
            "Translation must not change a surface normal");
    }

    private static void GeometricEqualityAndHashing()
    {
        Point point = new Point(new[] { 1.0, -0.0, 3.0 }, 1);
        Point equalPoint = new Point(new[] { 1.0, 0.0, 3.0 }, 5);
        Check(point.Equals(equalPoint) && point.GetHashCode() == equalPoint.GetHashCode(),
            "Equal point coordinates must have equal hashes regardless of signed zero or w");
        Dictionary<Point, string> points = new Dictionary<Point, string> { { point, "value" } };
        Check(points.ContainsKey(equalPoint), "Dictionary lookup must use point coordinate equality");
        Vector vector = new Vector(1, -0.0, 3);
        Normal normal = new Normal(1, 0, 3);
        Check(vector.Equals((object)normal) && normal.Equals((object)vector), "Vector/normal equality must be symmetric");
        Check(vector.GetHashCode() == normal.GetHashCode(), "Equal vector and normal coordinates must have equal hashes");
        HashSet<Vector> vectors = new HashSet<Vector> { vector };
        Check(vectors.Contains(normal), "Vector sets must find equal normal coordinates");
        vectors = new HashSet<Vector> { normal };
        Check(vectors.Contains(vector), "Normal-backed vector sets must find equal vectors");
        Point nanPoint = Point.NaN();
        Vector nanVector = Vector.NaN();
        Check(nanPoint.Equals((object)nanPoint) && nanVector.Equals((object)nanVector),
            "Object equality must be reflexive for NaN-bearing geometry");
    }

    private static void BilinearBoundaries()
    {
        double[] x = { 0, 2 };
        double[] y = { 0, 4 };
        double[,] values = { { 0, 4 }, { 2, 6 } };
        Near(0, Interpolate.Linear(0, 0, x, y, values), "Lower corner interpolation");
        Near(6, Interpolate.Linear(2, 4, x, y, values), "Upper corner interpolation");
        Near(2, Interpolate.Linear(0, 2, x, y, values), "Lower X edge interpolation");
        Near(4, Interpolate.Linear(2, 2, x, y, values), "Upper X edge interpolation");
        Near(1, Interpolate.Linear(1, 0, x, y, values), "Lower Y edge interpolation");
        Near(5, Interpolate.Linear(1, 4, x, y, values), "Upper Y edge interpolation");
        Near(3, Interpolate.Linear(1, 2, x, y, values), "Interior bilinear interpolation");
        Near(15, Interpolate.Linear(5, 2, new[] { 5.0 }, y, new[,] { { 10.0, 20.0 } }),
            "A singleton X axis must support interpolation along Y");
        Near(15, Interpolate.Linear(1, 5, x, new[] { 5.0 }, new[,] { { 10.0 }, { 20.0 } }),
            "A singleton Y axis must support interpolation along X");
        Near(7, Interpolate.Linear(5, 6, new[] { 5.0 }, new[] { 6.0 }, new[,] { { 7.0 } }),
            "A one-cell grid must return its only value");
        Check(Interpolate.Linear<int>(2, 4, x, y, new[,] { { 0, 4 }, { 2, 6 } }) == 6,
            "Generic bilinear interpolation must include boundaries");
        Throws<ArgumentException>(() => Interpolate.Linear(-0.1, 2, x, y, values), "Points outside the grid must still be rejected");
    }

    private static void RectangularAndEmptyArrayOperations()
    {
        int[,] integers = { { 1, -2, 3 }, { -4, 5, -6 } };
        double[,] doubles = { { 1, -2, 3 }, { -4, 5, -6 } };
        int[,] negatedIntegers = Operator.Negate(integers);
        double[,] negatedDoubles = Operator.Negate(doubles);
        double[,] absoluteDoubles = Operator.Absolute(doubles);
        for (int row = 0; row < 2; row++)
            for (int column = 0; column < 3; column++)
            {
                Near(-integers[row, column], negatedIntegers[row, column], "Rectangular integer matrix negation");
                Near(-doubles[row, column], negatedDoubles[row, column], "Rectangular double matrix negation");
                Near(Math.Abs(doubles[row, column]), absoluteDoubles[row, column], "Rectangular double matrix absolute value");
            }
        Check(integers[0, 0] == 1 && doubles[0, 1] == -2, "Element-wise operations must preserve their inputs");
        int[] threshold = Arr.Threshold(new[] { -1, 0, 2, 3 }, 2);
        Check(threshold.Length == 4 && threshold[0] == 0 && threshold[2] == 0 && threshold[3] == 3,
            "Vector thresholding must operate without requesting a nonexistent second dimension");
        Check(Arr.Threshold(new int[0], 2).Length == 0, "Threshold must support an empty vector");
        double[,] empty = new double[0, 3];
        Check(Operator.Add(empty, empty).GetLength(1) == 3, "Matrix addition must preserve empty shapes");
        Check(Operator.Subtract(empty, 1.0).Length == 0, "Matrix subtraction must support empty arrays");
        Check(Operator.Multiply(empty, 2.0).Length == 0, "Matrix multiplication by a scalar must support empty arrays");
        Check(Operator.Divide(2.0, empty).Length == 0, "Scalar division by an empty matrix must return an empty matrix");
        Check(Operator.Negate(empty).Length == 0 && Operator.Absolute(empty).Length == 0,
            "Double unary operations must support empty matrices");
        Check(Operator.Absolute(new int[0, 3]).Length == 0 && Operator.Negate(new int[0, 3]).Length == 0,
            "Integer unary operations must support empty matrices");
        Check(Operator.Convert<int, double>(new int[0, 3]).GetLength(1) == 3,
            "Generic unary operations must preserve empty matrix dimensions");
        Check(Element.Op<int, int>((a, b) => a + b, new int[0, 3], new int[0, 3]).Length == 0,
            "Generic binary operations must support empty matrices");
        Check(Element.Op<int, int>((a, b) => a + b, new int[0, 3], 2).Length == 0,
            "Generic array/scalar operations must support empty matrices");
        Check(Element.Op<int, int>((a, b) => a + b, 2, new int[0, 3]).Length == 0,
            "Generic scalar/array operations must support empty matrices");
    }

    private static void InverseInputShape()
    {
        Throws<ArgumentException>(() => Arr.Inverse(new[,] { { 1.0, 0, 9 }, { 0, 1, 9 } }),
            "A nonsquare matrix must not silently produce a partial inverse");
        double[,] matrix = { { 0, 2, 1 }, { 1, 0, 3 }, { 4, 1, 0 } };
        double[,] identity = Arr.MatrixMultiply(matrix, Arr.Inverse(matrix));
        for (int row = 0; row < 3; row++)
            for (int column = 0; column < 3; column++)
                Near(row == column ? 1 : 0, identity[row, column], "A valid square matrix must still invert correctly");
    }
}
