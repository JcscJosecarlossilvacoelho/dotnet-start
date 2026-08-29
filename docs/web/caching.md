---
title: Caching
description: Output caching, response caching, hybrid caching, and the invalidation problem.
order: 140
---

Caching is the cheapest performance win and the easiest correctness bug. Decide first *what may be stale, and for how long* — that answer chooses the mechanism.

## The options

| Mechanism | Lives | Good for |
| --- | --- | --- |
| `HybridCache` | In memory + a distributed backing store | Application data, with stampede protection |
| `IMemoryCache` | One process | Small, hot, non-critical data |
| `IDistributedCache` (Redis, SQL) | Shared | Data that must be consistent across instances |
| Output caching | Server-side, per endpoint | Whole responses for anonymous traffic |
| Response caching headers | Browsers and CDNs | Public, cacheable GETs |

## HybridCache

```csharp
builder.Services.AddHybridCache();

public sealed class Catalogue(HybridCache cache, IProductRepository repository)
{
    public async ValueTask<Product?> GetAsync(Guid id, CancellationToken ct) =>
        await cache.GetOrCreateAsync(
            $"product:{id}",
            async token => await repository.FindAsync(id, token),
            new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(10) },
            cancellationToken: ct);

    public ValueTask InvalidateAsync(Guid id, CancellationToken ct) =>
        cache.RemoveAsync($"product:{id}", ct);
}
```

It combines an L1 in-process cache with an L2 distributed cache and collapses concurrent misses into a single factory call — the stampede problem solved for you.

## Output caching

```csharp
builder.Services.AddOutputCache(options =>
    options.AddPolicy("products", policy => policy.Expire(TimeSpan.FromMinutes(5)).Tag("products")));

app.UseOutputCache();

app.MapGet("/products", ListProducts).CacheOutput("products");

app.MapPost("/products", async (IOutputCacheStore store, CancellationToken ct) =>
{
    // ...write...
    await store.EvictByTagAsync("products", ct);
});
```

Tag-based eviction is what makes output caching usable: writes invalidate exactly the responses they affect.

**Never cache a personalised response** unless the cache key includes the identity. `VaryByHeader` and `VaryByValue` exist for that; getting it wrong serves one user's data to another.

## HTTP caching headers

For public resources, let the CDN do the work:

```csharp
context.Response.Headers.CacheControl = "public,max-age=300";
context.Response.Headers.ETag = $"\"{version}\"";
```

Honour `If-None-Match` and return 304 — the cheapest response is the one with no body.

## Invalidation strategies

1. **Short TTL** — accept staleness; no invalidation code. The right default.
2. **Explicit eviction on write** — precise, needs discipline everywhere data changes.
3. **Version in the key** (`product:{id}:v{rowVersion}`) — old entries expire naturally, no eviction path.

## Rules

- Never cache what you must not lose: a cache can be empty at any moment.
- Set an absolute expiration on every entry, including sliding ones — otherwise you have built a memory leak.
- Cache the expensive thing (the query result), not the cheap thing (the DTO mapping).
- Measure the hit ratio. A cache below ~80% hits is usually paying more than it saves.

## Further reading

- [HybridCache](https://learn.microsoft.com/aspnet/core/performance/caching/hybrid)
- [Output caching](https://learn.microsoft.com/aspnet/core/performance/caching/output)
