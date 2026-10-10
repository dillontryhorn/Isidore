```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9550/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 7940HS w/ Radeon 780M Graphics 4.00GHz, 1 CPU, 16 logical and 8 physical cores
  [Host]   : .NET Framework 4.8.1 (4.8.9345.0), X64 RyuJIT VectorSize=256
  ShortRun : .NET Framework 4.8.1 (4.8.9345.0), X64 RyuJIT VectorSize=256

Job=ShortRun  PowerPlanMode=00000000-0000-0000-0000-000000000000  IterationCount=5  
LaunchCount=1  WarmupCount=3  

```
| Method               | Bytes   | Mean         | Error        | StdDev     | Ratio | RatioSD | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|--------------------- |-------- |-------------:|-------------:|-----------:|------:|--------:|---------:|---------:|---------:|----------:|------------:|
| **CloneStageAndPublish** | **262144**  |    **184.23 μs** |    **23.909 μs** |   **6.209 μs** |  **1.00** |    **0.04** |  **83.2520** |  **83.2520** |  **83.2520** |  **262168 B** |        **1.00** |
| PoolStageAndPublish  | 262144  |     22.61 μs |     2.386 μs |   0.620 μs |  0.12 |    0.00 |        - |        - |        - |         - |        0.00 |
|                      |         |              |              |            |       |         |          |          |          |           |             |
| **CloneStageAndPublish** | **8388608** |  **4,327.36 μs** |   **432.689 μs** | **112.368 μs** |  **1.00** |    **0.03** | **156.2500** | **156.2500** | **156.2500** | **8388632 B** |        **1.00** |
| PoolStageAndPublish  | 8388608 | 12,595.92 μs | 2,211.766 μs | 574.389 μs |  2.91 |    0.14 |        - |        - |        - |         - |        0.00 |
