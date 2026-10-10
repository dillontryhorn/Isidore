using System;
using Isidore.Maths;

internal static class SimdRegression
{
    public static void Run()
    {
        IntegerArithmetic();
        DoubleArithmetic();
        InvalidInputs();
        GenericOperators();
        Console.WriteLine("SIMD hardware acceleration: " + System.Numerics.Vector.IsHardwareAccelerated +
            "; int lanes: " + System.Numerics.Vector<int>.Count +
            "; double lanes: " + System.Numerics.Vector<double>.Count + ".");
    }

    private static int[] Lengths(int width)
    {
        return new[] { 0, 1, width - 1, width, 2 * width - 1, 2 * width,
            2 * width + 1, 3 * width - 1, 3 * width + 1, 1025 };
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void Equal(int[] expected, int[] actual, string message)
    {
        Check(expected.Length == actual.Length, message + " length differs");
        for (int index = 0; index < expected.Length; index++)
            Check(expected[index] == actual[index], message + " differs at " + index);
    }

    private static void Equal(double[] expected, double[] actual, string message)
    {
        Check(expected.Length == actual.Length, message + " length differs");
        for (int index = 0; index < expected.Length; index++)
        {
            bool match = double.IsNaN(expected[index]) ? double.IsNaN(actual[index])
                : BitConverter.DoubleToInt64Bits(expected[index]) == BitConverter.DoubleToInt64Bits(actual[index]);
            Check(match, message + " differs at " + index);
        }
    }

    private static void Unchanged(double[] expected, double[] actual, string message)
    {
        Check(expected.Length == actual.Length, message + " length differs");
        for (int index = 0; index < expected.Length; index++)
            Check(BitConverter.DoubleToInt64Bits(expected[index]) == BitConverter.DoubleToInt64Bits(actual[index]),
                message + " bits differ at " + index);
    }

    private static void IntegerArithmetic()
    {
        int[] values = { int.MinValue, int.MaxValue, -1, 0, 1, 46341, -46341, 1073741824 };
        int[] scalars = { int.MinValue, int.MaxValue, -1, 0, 1, 46341 };
        foreach (int length in Lengths(System.Numerics.Vector<int>.Count))
        {
            var first = new int[length];
            var second = new int[length];
            var expectedSum = new int[length];
            var expectedProduct = new int[length];
            for (int index = 0; index < length; index++)
            {
                first[index] = values[index % values.Length];
                second[index] = values[(index * 3 + 1) % values.Length];
                expectedSum[index] = unchecked(first[index] + second[index]);
                expectedProduct[index] = unchecked(first[index] * second[index]);
            }
            var originalFirst = (int[])first.Clone();
            var originalSecond = (int[])second.Clone();
            int[] sum = Operator.Add(first, second);
            int[] product = Operator.Multiply(first, second);
            Equal(expectedSum, sum, "Integer array sum, length " + length);
            Equal(expectedProduct, product, "Integer array product, length " + length);
            Check(!ReferenceEquals(first, sum) && !ReferenceEquals(second, sum) &&
                !ReferenceEquals(first, product) && !ReferenceEquals(second, product),
                "Integer arithmetic must return owned output arrays");
            foreach (int scalar in scalars)
            {
                for (int index = 0; index < length; index++)
                {
                    expectedSum[index] = unchecked(first[index] + scalar);
                    expectedProduct[index] = unchecked(first[index] * scalar);
                }
                Equal(expectedSum, Operator.Add(first, scalar), "Integer scalar sum");
                Equal(expectedSum, Operator.Add(scalar, first), "Integer scalar-first sum");
                Equal(expectedProduct, Operator.Multiply(first, scalar), "Integer scalar product");
                Equal(expectedProduct, Operator.Multiply(scalar, first), "Integer scalar-first product");
            }
            Equal(originalFirst, first, "Integer first input preservation");
            Equal(originalSecond, second, "Integer second input preservation");
            if (length > 0)
            {
                sum[0] = 123;
                product[0] = 456;
                Equal(originalFirst, first, "Integer output mutation must not alter the first input");
                Equal(originalSecond, second, "Integer output mutation must not alter the second input");
            }
        }
    }

    private static void DoubleArithmetic()
    {
        double negativeZero = BitConverter.Int64BitsToDouble(long.MinValue);
        double payloadNaN = BitConverter.Int64BitsToDouble(0x7ff8000000000042L);
        double[] values = { 0.0, negativeZero, double.Epsilon, -double.Epsilon,
            BitConverter.Int64BitsToDouble(0x000fffffffffffffL),
            double.MaxValue, -double.MaxValue, double.PositiveInfinity,
            double.NegativeInfinity, double.NaN, payloadNaN, 1.0, -1.0, 0.5, -0.5,
            1.0 / 3.0, -Math.PI, 1e-100, 1e100, BitConverter.Int64BitsToDouble(0x3ff0000000000001L) };
        double[] scalars = { 0.0, negativeZero, double.Epsilon, -double.Epsilon, 0.5, -1.0,
            double.MaxValue, double.PositiveInfinity, double.NegativeInfinity, payloadNaN };
        foreach (int length in Lengths(System.Numerics.Vector<double>.Count))
        {
            var first = new double[length];
            var second = new double[length];
            var expectedSum = new double[length];
            var expectedProduct = new double[length];
            for (int index = 0; index < length; index++)
            {
                first[index] = values[index % values.Length];
                second[index] = values[(index * 7 + 1) % values.Length];
                expectedSum[index] = first[index] + second[index];
                expectedProduct[index] = first[index] * second[index];
            }
            var originalFirst = (double[])first.Clone();
            var originalSecond = (double[])second.Clone();
            double[] sum = Operator.Add(first, second);
            double[] product = Operator.Multiply(first, second);
            Equal(expectedSum, sum, "Double array sum, length " + length);
            Equal(expectedProduct, product, "Double array product, length " + length);
            Check(!ReferenceEquals(first, sum) && !ReferenceEquals(second, sum) &&
                !ReferenceEquals(first, product) && !ReferenceEquals(second, product),
                "Double arithmetic must return owned output arrays");
            foreach (double scalar in scalars)
            {
                for (int index = 0; index < length; index++)
                {
                    expectedSum[index] = first[index] + scalar;
                    expectedProduct[index] = first[index] * scalar;
                }
                Equal(expectedSum, Operator.Add(first, scalar), "Double scalar sum");
                Equal(expectedSum, Operator.Add(scalar, first), "Double scalar-first sum");
                Equal(expectedProduct, Operator.Multiply(first, scalar), "Double scalar product");
                Equal(expectedProduct, Operator.Multiply(scalar, first), "Double scalar-first product");
            }
            Unchanged(originalFirst, first, "Double first input preservation");
            Unchanged(originalSecond, second, "Double second input preservation");
            if (length > 0)
            {
                sum[0] = 123;
                product[0] = 456;
                Unchanged(originalFirst, first, "Double output mutation must not alter the first input");
                Unchanged(originalSecond, second, "Double output mutation must not alter the second input");
            }
        }
    }

    private static void Throws<T>(Action action, string message) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new Exception(message);
    }

    private static void SizeMismatch(Action action)
    {
        try { action(); }
        catch (ArgumentException error)
        {
            Check(error.ParamName == "arr1", "Mismatched arrays must preserve the arr1 parameter name");
            return;
        }
        throw new Exception("Mismatched arrays must throw ArgumentException");
    }

    private static void InvalidInputs()
    {
        SizeMismatch(() => Operator.Add(new int[9], new int[8]));
        SizeMismatch(() => Operator.Multiply(new int[9], new int[8]));
        SizeMismatch(() => Operator.Add(new double[9], new double[8]));
        SizeMismatch(() => Operator.Multiply(new double[9], new double[8]));
        Throws<NullReferenceException>(() => Operator.Add((int[])null, new int[0]), "Null int first input");
        Throws<NullReferenceException>(() => Operator.Add(new int[0], (int[])null), "Null int second input");
        Throws<NullReferenceException>(() => Operator.Add((int[])null, 1), "Null int scalar input");
        Throws<NullReferenceException>(() => Operator.Multiply((int[])null, new int[0]), "Null int first product input");
        Throws<NullReferenceException>(() => Operator.Multiply(new int[0], (int[])null), "Null int second product input");
        Throws<NullReferenceException>(() => Operator.Multiply((int[])null, 1), "Null int scalar product input");
        Throws<NullReferenceException>(() => Operator.Add((double[])null, new double[0]), "Null double first input");
        Throws<NullReferenceException>(() => Operator.Add(new double[0], (double[])null), "Null double second input");
        Throws<NullReferenceException>(() => Operator.Add((double[])null, 1.0), "Null double scalar input");
        Throws<NullReferenceException>(() => Operator.Multiply((double[])null, new double[0]), "Null double first product input");
        Throws<NullReferenceException>(() => Operator.Multiply(new double[0], (double[])null), "Null double second product input");
        Throws<NullReferenceException>(() => Operator.Multiply((double[])null, 1.0), "Null double scalar product input");
    }

    public struct CustomNumber
    {
        public int Value;
        public CustomNumber(int value) { Value = value; }
        public static CustomNumber operator +(CustomNumber first, CustomNumber second)
        {
            return new CustomNumber(first.Value * 10 + second.Value);
        }
        public static CustomNumber operator *(CustomNumber first, CustomNumber second)
        {
            return new CustomNumber(first.Value * 100 + second.Value);
        }
    }

    private static void GenericOperators()
    {
        int length = 2 * System.Numerics.Vector<int>.Count + 1;
        var first = new CustomNumber[length];
        var second = new CustomNumber[length];
        for (int index = 0; index < length; index++)
        {
            first[index] = new CustomNumber(index + 1);
            second[index] = new CustomNumber(index + 2);
        }
        CustomNumber[] sum = Operator.Add<CustomNumber>(first, second);
        CustomNumber[] product = Operator.Multiply<CustomNumber>(first, second);
        CustomNumber[] scalarSum = Operator.Add<CustomNumber>(first, new CustomNumber(3));
        CustomNumber[] scalarFirstSum = Operator.Add<CustomNumber>(new CustomNumber(3), first);
        CustomNumber[] scalarProduct = Operator.Multiply<CustomNumber>(first, new CustomNumber(3));
        CustomNumber[] scalarFirstProduct = Operator.Multiply<CustomNumber>(new CustomNumber(3), first);
        for (int index = 0; index < length; index++)
        {
            Check(sum[index].Value == (index + 1) * 10 + index + 2, "Generic custom addition");
            Check(product[index].Value == (index + 1) * 100 + index + 2, "Generic custom multiplication");
            Check(scalarSum[index].Value == (index + 1) * 10 + 3, "Generic custom scalar addition");
            Check(scalarFirstSum[index].Value == 30 + index + 1, "Generic custom scalar-first addition");
            Check(scalarProduct[index].Value == (index + 1) * 100 + 3, "Generic custom scalar multiplication");
            Check(scalarFirstProduct[index].Value == 300 + index + 1, "Generic custom scalar-first multiplication");
        }
    }
}
