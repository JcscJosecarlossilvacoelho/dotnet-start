---
title: Benchmarking
description: Measuring with BenchmarkDotNet instead of guessing, and reading the numbers honestly.
order: 40
---

A stopwatch around a loop measures the JIT, the GC, and whatever else your machine was doing. BenchmarkDotNet handles warm-up, isolation, statistics, and memory accounting for you.

## Setup

```bash
dotnet new console -n MyApp.Benchmarks
dotnet add MyApp.Benchmarks package BenchmarkDotNet
```

```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class ParsingBenchmarks
{
    private readonly string _line = "2026-08-29,142.50,EUR";

    [Benchmark(Baseline = true)]
    public decimal Split() => decimal.Parse(_line.Split(',')[1], CultureInfo.InvariantCulture);

    [Benchmark]
    public decimal Span()
    {
        var rest = _line.AsSpan(_line.IndexOf(',') + 1);
        return decimal.Parse(rest[..rest.IndexOf(',')], CultureInfo.InvariantCulture);
    }
}

BenchmarkRunner.Run<ParsingBenchmarks>();
```

Always run in Release: `dotnet run -c Release --project MyApp.Benchmarks`.

## Reading the output

| Column | Meaning |
| --- | --- |
| Mean | Average time per operation |
| Error / StdDev | Confidence in the mean — a large StdDev means the benchmark is noisy |
| Ratio | Relative to the `Baseline` method |
| Gen0/Gen1/Gen2 | Collections per 1,000 operations |
| Allocated | Bytes allocated per operation |

`Allocated` is often the most actionable number: reducing allocation reduces GC pauses, which is what users actually feel.

## Getting it right

- Return a value from every benchmark, or the JIT may delete the work.
- Put setup in `[GlobalSetup]`, not in the benchmark body.
- Benchmark realistic input sizes; a 10-character string tells you nothing about a 10 MB one.
- Do not benchmark IO with BenchmarkDotNet — network and disk variance drowns the signal. Use a load test.
- Compare like with like: the same machine, the same power profile, nothing else running.

## Load testing the whole system

Micro-benchmarks answer "is this method faster". Only a load test answers "is the service faster". Drive the deployed service with `k6`, `bombardier`, or `wrk`, and read the result alongside your [traces and metrics](/docs/ops/observability) — a p99 improvement invisible in the mean is still worth shipping.

## Further reading

- [BenchmarkDotNet documentation](https://benchmarkdotnet.org/)
