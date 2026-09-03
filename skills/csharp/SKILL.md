---
name: csharp
description: Write, review, or modernize idiomatic C#. Use for nullable reference types, records and the type system, pattern matching, LINQ and collections, exceptions, immutability, and language-version-appropriate syntax.
---

# C#

Read the project file first. `TargetFramework`, `LangVersion`, `Nullable`, and `ImplicitUsings` decide which of the guidance below applies — never write syntax the target framework cannot compile.

```bash
rg -n 'TargetFramework|LangVersion|Nullable|ImplicitUsings|TreatWarningsAsErrors' -g '*.csproj' -g 'Directory.Build.props'
```

Then match the surrounding file: its naming, its expression-bodied vs block style, its file-scoped vs block namespaces. Consistency with the file beats consistency with this skill.

## Model the type first

Choose the type before writing the members. The choice is load-bearing, and changing it later is a breaking change.

| Need | Use |
| --- | --- |
| Immutable data compared by value | `record` (reference) |
| Small immutable value, hot path, no heap | `readonly record struct` |
| Identity and mutable state | `class` |
| Closed set of shapes | abstract record + sealed subtypes, matched with `switch` |
| A name for a primitive to stop mixing up arguments | `readonly record struct OrderId(Guid Value)` |

- `sealed` by default on classes. Inheritance is a design decision, not a default.
- Prefer `required` and `init` over constructors that leave objects half-built; prefer a primary constructor when every member is set once.
- Never expose a mutable collection from a public API. Return `IReadOnlyList<T>`.

```csharp
// Good — the type makes the invalid state unrepresentable.
public sealed record Order
{
    public required OrderId Id { get; init; }
    public required IReadOnlyList<OrderLine> Lines { get; init; }
    public decimal Total => Lines.Sum(line => line.Amount);
}

// Avoid — settable everything, nullable everything, validity checked nowhere.
public class Order
{
    public Guid Id { get; set; }
    public List<OrderLine>? Lines { get; set; }
    public decimal Total { get; set; }
}
```

## Take nullability seriously

`<Nullable>enable</Nullable>` belongs in every project. Treat a nullable warning as a bug report about a real call path.

- `!` (null-forgiving) is an assertion you cannot prove. Use it only next to a comment explaining why, or replace it with a check.
- Do not annotate a parameter as nullable just to silence a caller. Fix the caller, or overload.
- Validate at the boundary — deserialized JSON, configuration, and database rows are `null` no matter what the compiler believes.

```csharp
// Good
public static Order Parse(OrderDto? dto)
{
    ArgumentNullException.ThrowIfNull(dto);
    return new Order { Id = new OrderId(dto.Id), Lines = dto.Lines?.Select(Map).ToArray() ?? [] };
}
```

Use `ArgumentNullException.ThrowIfNull`, `ArgumentException.ThrowIfNullOrWhiteSpace`, and `ArgumentOutOfRangeException.ThrowIfNegative` instead of hand-written `if (x is null) throw`.

## Pattern match instead of branching on type

```csharp
// Good — exhaustive, expression-shaped, no casts.
public static decimal Price(Shipment shipment) => shipment switch
{
    Standard { Weight: <= 1 } => 4.50m,
    Standard s => 4.50m + (s.Weight - 1) * 0.90m,
    Express { Overnight: true } => 24.00m,
    Express => 12.00m,
    _ => throw new ArgumentOutOfRangeException(nameof(shipment)),
};
```

- `is null` / `is not null`, never `== null` on a type that may overload `==`.
- Use list patterns (`[var first, .. var rest]`) and property patterns rather than index arithmetic.
- A `switch` expression over a closed hierarchy should keep the discard arm throwing, so a new subtype fails loudly instead of silently returning a default.

## LINQ and collections

- Name what a query does. A chain longer than three operators usually wants an intermediate local or a named method.
- Enumerate once. If you need `Count` and the items, materialize with `ToArray()`/`ToList()` first — repeated enumeration of a lazy source re-runs the work.
- Prefer `Any()` over `Count() > 0`, `FirstOrDefault(predicate)` over `Where(...).FirstOrDefault()`, `TryGetValue` over `ContainsKey` + indexer.
- Use collection expressions (`[]`, `[.. items, extra]`) on C# 12+; use `FrozenDictionary`/`FrozenSet` for lookup tables built once and read forever.
- Return `IEnumerable<T>` only when laziness is intentional; otherwise return a materialized `IReadOnlyList<T>` so callers cannot accidentally re-execute it.

```csharp
// Avoid — three enumerations of a possibly expensive source.
if (source.Count() > 0 && source.Any(x => x.IsActive)) { Process(source.First()); }

// Good
var items = source.ToArray();
if (items.FirstOrDefault(x => x.IsActive) is { } active) { Process(active); }
```

## Exceptions

- Throw for broken invariants and unusable input. Return a result type or `bool Try...(out T)` for outcomes the caller routinely expects, such as validation failures and "not found".
- Catch only what you can act on. Never `catch (Exception)` without rethrowing, logging with context, or converting into a documented failure of your own.
- Rethrow with `throw;`, never `throw ex;` — the latter erases the stack trace.
- Never swallow `OperationCanceledException` as an error; cancellation is a normal outcome. See the `dotnet-async` skill.
- Prefer specific built-in exception types over a bespoke hierarchy; add a custom exception only when a caller needs to catch it distinctly.

## Modern syntax worth adopting

| Instead of | Use |
| --- | --- |
| `namespace X { ... }` | file-scoped `namespace X;` |
| `new List<int> { 1, 2 }` | `[1, 2]` (C# 12+) |
| `string.Format` / concatenation | interpolation, or `StringBuilder` in loops |
| manual `IDisposable` blocks | `using var` / `await using var` |
| `Newtonsoft.Json` in new code | `System.Text.Json` with a source-generated context |
| reflection-heavy startup code | source generators / `[JsonSerializable]` |

Use `var` when the right-hand side names the type, an explicit type when it does not.

## Complete the change

```bash
dotnet format --verify-no-changes   # style, if the repo uses it
dotnet build -warnaserror
dotnet test
```

Leave the warning count no higher than you found it. If the repo has an `.editorconfig`, it outranks every preference stated here.
