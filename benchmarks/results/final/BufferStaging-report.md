```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9550/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 7940HS w/ Radeon 780M Graphics 4.00GHz, 1 CPU, 16 logical and 8 physical cores
  [Host]   : .NET Framework 4.8.1 (4.8.9345.0), X64 RyuJIT VectorSize=256
  ShortRun : .NET Framework 4.8.1 (4.8.9345.0), X64 RyuJIT VectorSize=256

Job=ShortRun  PowerPlanMode=00000000-0000-0000-0000-000000000000  IterationCount=5  
LaunchCount=1  WarmupCount=3  

```
| Method               | Bytes   | Mean        | Error        | StdDev       | Ratio | RatioSD | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|--------------------- |-------- |------------:|-------------:|-------------:|------:|--------:|---------:|---------:|---------:|----------:|------------:|
| **CloneStageAndPublish** | **262144**  |   **182.62 μs** |    **22.419 μs** |     **5.822 μs** |  **1.00** |    **0.04** |  **83.2520** |  **83.2520** |  **83.2520** |  **262168 B** |        **1.00** |
| PoolStageAndPublish  | 262144  |    35.01 μs |     1.609 μs |     0.418 μs |  0.19 |    0.01 |        - |        - |        - |         - |        0.00 |
|                      |         |             |              |              |       |         |          |          |          |           |             |
| **CloneStageAndPublish** | **1048576** |   **379.98 μs** |    **46.436 μs** |    **12.059 μs** |  **1.00** |    **0.04** | **222.1680** | **222.1680** | **222.1680** | **1048600 B** |        **1.00** |
| PoolStageAndPublish  | 1048576 |   151.56 μs |     2.259 μs |     0.350 μs |  0.40 |    0.01 |        - |        - |        - |         - |        0.00 |
|                      |         |             |              |              |       |         |          |          |          |           |             |
| **CloneStageAndPublish** | **8388608** | **4,893.97 μs** |   **585.469 μs** |   **152.045 μs** |  **1.00** |    **0.04** | **156.2500** | **156.2500** | **156.2500** | **8388632 B** |        **1.00** |
| PoolStageAndPublish  | 8388608 | 5,967.84 μs | 3,925.408 μs | 1,019.416 μs |  1.22 |    0.19 | 156.2500 | 156.2500 | 156.2500 | 8388632 B |        1.00 |
