using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace Isidore.Benchmarks
{
    public sealed class PerformanceConfig : ManualConfig
    {
        public PerformanceConfig()
        {
            // Measure under the user's current power plan without changing it.
            AddJob(Job.ShortRun.DontEnforcePowerPlan());
        }
    }
}
