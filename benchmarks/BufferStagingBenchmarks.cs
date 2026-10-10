using System;
using System.Buffers;
using BenchmarkDotNet.Attributes;

namespace Isidore.Benchmarks
{
    [Config(typeof(PerformanceConfig)), MemoryDiagnoser]
    public class BufferStagingBenchmarks
    {
        private const int MaxPooledDownloadBytes = 1024 * 1024;
        // Match the runtime's bounded primitive staging pool.
        private static readonly ArrayPool<double> DownloadPool =
            ArrayPool<double>.Create(MaxPooledDownloadBytes / sizeof(double), 2);
        [Params(256 * 1024, 1024 * 1024, 8 * 1024 * 1024)] public int Bytes { get; set; }
        private double[] downloadedValues;
        private double[] output;

        [GlobalSetup]
        public void Setup()
        {
            downloadedValues = new double[Bytes / sizeof(double)];
            output = new double[downloadedValues.Length];
            for (int i = 0; i < downloadedValues.Length; i++) downloadedValues[i] = i * 0.125;
        }

        [Benchmark(Baseline = true)]
        public double CloneStageAndPublish()
        {
            double[] staged = (double[])output.Clone();
            // Both methods include the same simulated download and publication.
            Buffer.BlockCopy(downloadedValues, 0, staged, 0, Bytes);
            Buffer.BlockCopy(staged, 0, output, 0, Bytes);
            return output[output.Length - 1];
        }

        [Benchmark]
        public double PoolStageAndPublish()
        {
            if (Bytes > MaxPooledDownloadBytes) return CloneStageAndPublish();
            double[] staged = DownloadPool.Rent((Bytes + sizeof(double) - 1) / sizeof(double));
            try
            {
                Buffer.BlockCopy(downloadedValues, 0, staged, 0, Bytes);
                Buffer.BlockCopy(staged, 0, output, 0, Bytes);
                return output[output.Length - 1];
            }
            finally { DownloadPool.Return(staged); }
        }
    }
}
