```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.7840/25H2/2025Update/HudsonValley2)
12th Gen Intel Core i7-1260P 2.10GHz, 1 CPU, 16 logical and 12 physical cores
.NET SDK 10.0.101
  [Host]     : .NET 10.0.1 (10.0.1, 10.0.125.57005), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.1 (10.0.1, 10.0.125.57005), X64 RyuJIT x86-64-v3


```
| Method         | MaxValue | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|--------------- |--------- |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| **Original_Next**  | **52**       | **90.71 μs** | **1.770 μs** | **2.423 μs** |  **1.00** |    **0.04** |         **-** |          **NA** |
| Optimized_Next | 52       | 38.37 μs | 0.514 μs | 0.429 μs |  0.42 |    0.01 |         - |          NA |
|                |          |          |          |          |       |         |           |             |
| **Original_Next**  | **100**      | **79.94 μs** | **1.450 μs** | **1.285 μs** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Optimized_Next | 100      | 79.23 μs | 1.575 μs | 2.674 μs |  0.99 |    0.04 |         - |          NA |
