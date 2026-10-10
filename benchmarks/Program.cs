using System;
using BenchmarkDotNet.Running;
using System.Linq;

namespace Isidore.Benchmarks
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            // Some launchers supply both Path and PATH. .NET Framework's child
            // process environment rejects those duplicates; normalize locally.
            string path = Environment.GetEnvironmentVariable("PATH");
            if (path != null)
            {
                Environment.SetEnvironmentVariable("Path", null);
                Environment.SetEnvironmentVariable("PATH", null);
                Environment.SetEnvironmentVariable("PATH", path);
            }
            var summaries = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args).ToArray();
            if (summaries.Length == 0)
                return args.Contains("--list") || args.Contains("--help") ? 0 : 1;
            return summaries.Any(summary => summary.Reports.Any(report => !report.Success)) ? 1 : 0;
        }
    }
}
