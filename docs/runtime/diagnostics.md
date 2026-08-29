---
title: Diagnosing a running application
description: The dotnet-* tools, what each one answers, and a triage order that works under pressure.
order: 30
---

You can diagnose a production .NET process without stopping it. Install the tools once:

```bash
dotnet tool install -g dotnet-counters
dotnet tool install -g dotnet-trace
dotnet tool install -g dotnet-dump
dotnet tool install -g dotnet-gcdump
dotnet tool install -g dotnet-stack
```

## Which tool answers which question

| Question | Tool |
| --- | --- |
| Is it CPU, memory, GC, or the thread pool? | `dotnet-counters` |
| Which methods are burning CPU? | `dotnet-trace` (then PerfView / SpeedScope) |
| Why is memory growing? | `dotnet-gcdump`, then compare two captures |
| What is every thread doing right now? | `dotnet-stack` |
| What happened before it crashed? | `dotnet-dump` + `dotnet-dump analyze` |
| How long did each step of the request take? | [OpenTelemetry traces](/docs/ops/observability) |

## A triage order

1. **Look at counters first.**

   ```bash
   dotnet-counters monitor -p <pid> System.Runtime Microsoft.AspNetCore.Hosting
   ```

   High CPU + high GC time → allocation problem. Low CPU + high latency → blocking or a slow dependency. Growing heap across gen 2 → a leak.

2. **Take a trace under load** for CPU problems:

   ```bash
   dotnet-trace collect -p <pid> --profile cpu-sampling --duration 00:00:30
   ```

3. **Take two gcdumps five minutes apart** for memory problems and diff them. The type whose count keeps rising, and its retention path, is the answer.

4. **Capture a dump before restarting** anything you cannot reproduce:

   ```bash
   dotnet-dump collect -p <pid>
   dotnet-dump analyze core_dump
   > clrthreads
   > dumpheap -stat
   > pstacks
   ```

## In containers

Run the tools as a sidecar sharing the process namespace, or install them into the image for debug builds. Set `DOTNET_DbgEnableMiniDump=1` and a dump path so crashes leave evidence behind:

```
DOTNET_DbgEnableMiniDump=1
DOTNET_DbgMiniDumpType=2
DOTNET_DbgMiniDumpName=/dumps/core.%p
```

## Built-in counters worth watching

- `System.Runtime` — CPU, heap size, GC counts, exception rate, thread-pool queue.
- `Microsoft.AspNetCore.Hosting` — requests per second, failed requests, current requests.
- `System.Net.Http` — outbound requests, connection pool usage.
- `Microsoft.EntityFrameworkCore` — active DbContexts, executed commands.

## Further reading

- [.NET diagnostic tools](https://learn.microsoft.com/dotnet/core/diagnostics/)
