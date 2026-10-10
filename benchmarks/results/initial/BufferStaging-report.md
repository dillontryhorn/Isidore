```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9550/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 7940HS w/ Radeon 780M Graphics 4.00GHz, 1 CPU, 16 logical and 8 physical cores
  [Host]   : .NET Framework 4.8.1 (4.8.9345.0), X64 RyuJIT VectorSize=256
  ShortRun : .NET Framework 4.8.1 (4.8.9345.0), X64 RyuJIT VectorSize=256

Job=ShortRun  PowerPlanMode=00000000-0000-0000-0000-000000000000  IterationCount=3  
LaunchCount=1  WarmupCount=3  

```
| Method               | Bytes   | Mean         | Error       | StdDev     | Ratio | RatioSD | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|--------------------- |-------- |-------------:|------------:|-----------:|------:|--------:|---------:|---------:|---------:|----------:|------------:|
| **CloneStageAndPublish** | **262144**  |    **149.32 μs** |   **175.50 μs** |   **9.620 μs** |  **1.00** |    **0.08** |  **83.2520** |  **83.2520** |  **83.2520** |  **262168 B** |        **1.00** |
| PoolStageAndPublish  | 262144  |     27.36 μs |    20.29 μs |   1.112 μs |  0.18 |    0.01 |        - |        - |        - |         - |        0.00 |
|                      |         |              |             |            |       |         |          |          |          |           |             |
| **CloneStageAndPublish** | **8388608** |  **6,055.13 μs** | **5,030.22 μs** | **275.723 μs** |  **1.00** |    **0.06** | **156.2500** | **156.2500** | **156.2500** | **8388632 B** |        **1.00** |
| PoolStageAndPublish  | 8388608 | 12,329.74 μs | 7,459.69 μs | 408.891 μs |  2.04 |    0.10 |        - |        - |        - |         - |        0.00 |
