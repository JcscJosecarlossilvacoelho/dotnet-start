---
title: Native AOT, trimming, and startup
description: Publishing a self-contained native binary, what it forbids, and when it is the right trade.
order: 50
---

By default, .NET ships IL and compiles it at runtime. **Native AOT** compiles ahead of time to a single native executable: no JIT, no IL, no runtime code generation.

## What you gain and lose

| | JIT (default) | Native AOT |
| --- | --- | --- |
| Startup | ~50–200 ms to first request | Single-digit milliseconds |
| Memory | Higher baseline | Substantially lower |
| Deployment | Runtime or self-contained folder | One file, no runtime installed |
| Peak throughput | Often higher (tiered JIT specialises) | Comparable, sometimes lower |
| Build time | Fast | Slow; needs a native toolchain |
| Reflection | Unrestricted | Restricted to what can be proven statically |

The verdict: excellent for CLIs, serverless functions, sidecars, and small HTTP services. Not worth it for a large application built on reflection-heavy libraries.

## Publishing

```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
  <InvariantGlobalization>true</InvariantGlobalization>
  <StripSymbols>true</StripSymbols>
</PropertyGroup>
```

```bash
dotnet publish -c Release -r linux-x64
```

You must publish for a specific runtime identifier, and you must build on (or cross-compile for) that platform.

## What breaks

- `Assembly.Load`, `Type.GetType("...")` on a name computed at runtime, `Activator.CreateInstance` on an unreferenced type.
- Reflection-based serialization — use the [`System.Text.Json` source generator](/docs/csharp/source-generators).
- `System.Reflection.Emit`, dynamic proxies, most classic mocking frameworks (test projects are unaffected — you test the IL build).
- EF Core has limited support; check your provider before committing.

The compiler tells you in advance:

```xml
<IsAotCompatible>true</IsAotCompatible>
<TrimmerSingleWarn>false</TrimmerSingleWarn>
```

Warnings IL2xxx (trimming) and IL3xxx (AOT) name the exact call site.

## Trimming without AOT

If you want a smaller deployment but still need the JIT:

```xml
<PublishTrimmed>true</PublishTrimmed>
<TrimMode>full</TrimMode>
```

Trimming removes unreferenced IL. The same reflection caveats apply, and the same warnings appear.

## Minimal API on AOT

```csharp
var builder = WebApplication.CreateSlimBuilder(args);
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonContext.Default));

var app = builder.Build();
app.MapGet("/orders/{id:guid}", (Guid id) => new Order(id));
app.Run();
```

`CreateSlimBuilder` drops the parts of the default host that AOT cannot use.

## Further reading

- [Native AOT deployment](https://learn.microsoft.com/dotnet/core/deploying/native-aot/)
- [Prepare libraries for trimming](https://learn.microsoft.com/dotnet/core/deploying/trimming/prepare-libraries-for-trimming)
