# Isidore performance benchmarks

This isolated .NET Framework 4.8 console project uses BenchmarkDotNet 0.15.8.
Recorded measurements and validation are in [RESULTS.md](RESULTS.md).
Build Isidore.Maths, Isidore.ImgProcess, and Isidore.Load in Release first: the
benchmark project references those DLLs directly, so it does not build the
legacy projects or their resource and COM dependencies.

Run from the repository root with a .NET SDK and .NET Framework 4.8 installed:

```powershell
.\benchmarks\run-benchmarks.ps1
# For a focused comparison:
.\benchmarks\run-benchmarks.ps1 -Filter '*Bitmap*'
```

The script builds and verifies the core Release libraries, restores the locked
tool dependencies, builds the benchmark executable, and exports package notices.
To inspect cases after building, run
`benchmarks\bin\Release\net48\Isidore.Benchmarks.exe --list flat`.

Narrow `-Filter` to a class, such as
`'*Bitmap*'`, `'*IntAdd*'`, or `'*BufferStaging*'`. All classes use the short job
and `MemoryDiagnoser` under the current power plan; results include execution
time and managed allocation.
The short job is suitable for an initial comparison. Repeat uncertain results
under a longer job before treating a small difference as conclusive.

| Cases | Comparison |
| --- | --- |
| `IntAddBenchmarks`, `IntMultiplyBenchmarks` | Scalar loops and public SIMD operations on 1,048,579-element integer arrays. |
| `DoubleAddBenchmarks`, `DoubleMultiplyBenchmarks` | Scalar loops and public SIMD operations on 1,048,579-element double arrays. |
| `BitmapReadBenchmarks` | Original `GetPixel` conversion and bulk `ConvertImg.toColor` on a 512 by 513 bitmap. |
| `BitmapWriteBenchmarks` | Original grayscale formulas and `SetPixel` writes compared with `ConvertImg.toBitmap`, including disposal on every invocation. |
| `BufferStagingBenchmarks` | Original clone staging and bounded primitive staging at 256 KiB, 1 MiB, and 8 MiB, including identical simulated download and publication copies. |

The staging benchmark excludes OpenCL initialization, kernels, and driver I/O.
It uses the runtime's bounded pool with a 1 MiB maximum buffer and two retained
arrays per size bucket. It measures repeated use after BenchmarkDotNet warmup.
Larger buffers use the original clone staging path because large pooled copies
showed inconsistent performance during evaluation.
Bitmap fixture creation runs outside the measurements, and the retained input
bitmap is disposed at global cleanup. The math operations preserve independent
result-array allocation in both implementations.
