```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9550/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 7940HS w/ Radeon 780M Graphics 4.00GHz, 1 CPU, 16 logical and 8 physical cores
  [Host]   : .NET Framework 4.8.1 (4.8.9345.0), X64 RyuJIT VectorSize=256
  ShortRun : .NET Framework 4.8.1 (4.8.9345.0), X64 RyuJIT VectorSize=256

Job=ShortRun  PowerPlanMode=00000000-0000-0000-0000-000000000000  IterationCount=5  
LaunchCount=1  WarmupCount=3  

```
| Method                   | Bytes   | Mean        | Error      | StdDev    | Ratio | RatioSD | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|------------------------- |-------- |------------:|-----------:|----------:|------:|--------:|---------:|---------:|---------:|----------:|------------:|
| **CloneStageAndPublish**     | **262144**  |   **183.81 μs** |  **57.702 μs** | **14.985 μs** |  **1.01** |    **0.11** |  **83.2520** |  **83.2520** |  **83.2520** |  **262168 B** |        **1.00** |
| PoolStageAndPublish      | 262144  |    22.59 μs |   0.577 μs |  0.150 μs |  0.12 |    0.01 |        - |        - |        - |         - |        0.00 |
| TypedPoolStageAndPublish | 262144  |    34.71 μs |   2.336 μs |  0.607 μs |  0.19 |    0.02 |        - |        - |        - |         - |        0.00 |
|                          |         |             |            |           |       |         |          |          |          |           |             |
| **CloneStageAndPublish**     | **8388608** | **4,303.61 μs** | **259.943 μs** | **67.506 μs** |  **1.00** |    **0.02** | **152.3438** | **152.3438** | **152.3438** | **8388632 B** |        **1.00** |
| PoolStageAndPublish      | 8388608 | 4,229.46 μs | 363.301 μs | 94.348 μs |  0.98 |    0.02 |        - |        - |        - |         - |        0.00 |
| TypedPoolStageAndPublish | 8388608 | 2,416.33 μs | 172.339 μs | 44.756 μs |  0.56 |    0.01 |        - |        - |        - |         - |        0.00 |
