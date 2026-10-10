using BenchmarkDotNet.Attributes;
using Isidore.Maths;

namespace Isidore.Benchmarks
{
    internal static class ArrayInputs
    {
        internal static int[] Integers(int count, int offset)
        {
            int[] values = new int[count];
            for (int i = 0; i < count; i++) values[i] = unchecked(i * 397 + offset);
            return values;
        }

        internal static double[] Doubles(int count, double offset)
        {
            double[] values = new double[count];
            for (int i = 0; i < count; i++) values[i] = (i % 4093) * 0.125 + offset;
            return values;
        }
    }

    [Config(typeof(PerformanceConfig)), MemoryDiagnoser]
    public class IntAddBenchmarks
    {
        [Params(1048579)] public int Count { get; set; }
        private int[] left;
        private int[] right;

        [GlobalSetup]
        public void Setup()
        {
            left = ArrayInputs.Integers(Count, 17);
            right = ArrayInputs.Integers(Count, -11);
        }

        [Benchmark(Baseline = true)]
        public int[] ScalarAdd()
        {
            int[] result = new int[left.Length];
            for (int i = 0; i < result.Length; i++) result[i] = unchecked(left[i] + right[i]);
            return result;
        }

        [Benchmark]
        public int[] SimdAdd() { return Operator.Add(left, right); }
    }

    [Config(typeof(PerformanceConfig)), MemoryDiagnoser]
    public class IntMultiplyBenchmarks
    {
        [Params(1048579)] public int Count { get; set; }
        private int[] left;
        private int[] right;

        [GlobalSetup]
        public void Setup()
        {
            left = ArrayInputs.Integers(Count, 17);
            right = ArrayInputs.Integers(Count, -11);
        }

        [Benchmark(Baseline = true)]
        public int[] ScalarMultiply()
        {
            int[] result = new int[left.Length];
            for (int i = 0; i < result.Length; i++) result[i] = unchecked(left[i] * right[i]);
            return result;
        }

        [Benchmark]
        public int[] SimdMultiply() { return Operator.Multiply(left, right); }
    }

    [Config(typeof(PerformanceConfig)), MemoryDiagnoser]
    public class DoubleAddBenchmarks
    {
        [Params(1048579)] public int Count { get; set; }
        private double[] left;
        private double[] right;

        [GlobalSetup]
        public void Setup()
        {
            left = ArrayInputs.Doubles(Count, 0.25);
            right = ArrayInputs.Doubles(Count, -0.75);
        }

        [Benchmark(Baseline = true)]
        public double[] ScalarAdd()
        {
            double[] result = new double[left.Length];
            for (int i = 0; i < result.Length; i++) result[i] = left[i] + right[i];
            return result;
        }

        [Benchmark]
        public double[] SimdAdd() { return Operator.Add(left, right); }
    }

    [Config(typeof(PerformanceConfig)), MemoryDiagnoser]
    public class DoubleMultiplyBenchmarks
    {
        [Params(1048579)] public int Count { get; set; }
        private double[] left;
        private double[] right;

        [GlobalSetup]
        public void Setup()
        {
            left = ArrayInputs.Doubles(Count, 0.25);
            right = ArrayInputs.Doubles(Count, -0.75);
        }

        [Benchmark(Baseline = true)]
        public double[] ScalarMultiply()
        {
            double[] result = new double[left.Length];
            for (int i = 0; i < result.Length; i++) result[i] = left[i] * right[i];
            return result;
        }

        [Benchmark]
        public double[] SimdMultiply() { return Operator.Multiply(left, right); }
    }
}
