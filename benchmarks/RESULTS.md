# Performance verification, 2026-10-10

Measured on Windows 11 with an AMD Ryzen 9 7940HS (8 cores, 16 logical
processors), .NET Framework 4.8.1, x64 RyuJIT, and 256-bit SIMD. The libraries
target .NET Framework 4.8. BenchmarkDotNet 0.15.8 ran Release code under the
existing power plan with concurrent workstation GC. Fixture creation is excluded.

The SIMD and bitmap comparisons use three warmups and three measured iterations
in one launch. Each baseline reproduces the original implementation, including
result allocation or bitmap disposal. Speedups below divide the reported means;
they are short-run observations rather than guarantees for other inputs or hosts.

| Operation | Input | Original mean | Updated mean | Observed speedup |
| --- | --- | ---: | ---: | ---: |
| Integer addition | 1,048,579 elements | 2.404 ms | 1.916 ms | 1.25x |
| Integer multiplication | 1,048,579 elements | 3.029 ms | 1.566 ms | 1.93x |
| Double addition | 1,048,579 elements | 3.868 ms | 3.757 ms | Inconclusive |
| Double multiplication | 1,048,579 elements | 4.662 ms | 3.635 ms | 1.28x |
| Bitmap to colors | 512 x 513 pixels | 99.36 ms | 12.60 ms | 7.89x |
| Grayscale array to bitmap | 512 x 513 pixels | 132.65 ms | 25.52 ms | 5.20x |

SIMD retains the original output allocation: approximately 4 MiB for integer
arrays and 8 MiB for doubles. Bitmap reading adds only a row scratch buffer.
Bitmap writing uses approximately 1 MiB of additional pixel scratch space for
this fixture. Its numeric conversions and their existing allocations remain.
Double addition's roughly 3% difference is within the noise. The integer
multiplication baseline was also noisy; the raw reports include standard
deviations and confidence intervals.

The final GPU staging comparison is recorded in
[the final report](results/final/BufferStaging-report.md) and
[CSV](results/final/BufferStaging-report.csv). It uses five measured iterations.
This isolates managed staging with identical simulated download and publication
copies; it excludes OpenCL driver transfers and kernel execution.

| Staged output | Original mean | Final mean | Interpretation |
| --- | ---: | ---: | --- |
| 256 KiB | 182.62 us | 35.01 us | 5.22x faster |
| 1 MiB | 379.98 us | 151.56 us | 2.51x faster |
| 8 MiB | 4.894 ms | 5.968 ms | Original clone path; noisy comparison |

The warmed 256 KiB and 1 MiB cases allocate no scratch array per invocation,
compared with 262,168 and 1,048,600 bytes respectively for cloning. Production
dispatches still allocate their small bookkeeping arrays. The 8 MiB cases call
the same original staging function and allocate 8,388,632 bytes each; no gain is
claimed there. Its fallback result has a 1.019 ms standard deviation and a wide
confidence interval, consistent with the variability seen in large copies.

Large pooled-copy timings varied substantially between runs for both byte and
double storage. The final runtime therefore pools only outputs through 1 MiB,
retaining less than 4 MiB across all buckets, and preserves the original clone
staging above that limit. Only exact logical bytes are downloaded and published;
caller arrays retain their ownership and shape. The initial unrestricted pooling
results, including regressions, are retained under
[initial reports](results/initial/BufferStaging-report.md) and
[staging investigation](results/staging-investigation/TypedOnly-report.md).

Raw SIMD and bitmap CSV reports are in `results/initial`. The final pool cutoff
does not change those implementations. See [README](README.md) to rerun the
benchmarks and [tooling notices](THIRD-PARTY-NOTICES.md) for dependency terms.

The Release regression suite passed all 17 CPU/GPU groups on the NVIDIA GeForce
RTX 4060 Laptop GPU. Coverage includes bitmap formats and signed strides, SIMD
tails and numeric edge cases, mixed GPU array ranks, both sides of the pool cap,
concurrent callers, and failed dispatches. The x86 CPU suite also passed with
SIMD disabled. MATLAB wrapper checks use a test double; the original full MATLAB
COM demonstration suite was not run.
