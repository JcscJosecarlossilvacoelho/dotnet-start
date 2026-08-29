---
title: Nullable reference types
description: How the compiler tracks null, what the annotations mean, and how to adopt them in existing code.
order: 20
---

With `<Nullable>enable</Nullable>`, the compiler tracks whether a reference can be `null` and warns when you dereference something that might be. It is static analysis, not a runtime guarantee — but it removes the majority of `NullReferenceException`s from new code.

## The annotations

```csharp
string name;       // not null: assigning null is a warning
string? maybe;     // may be null: dereferencing without a check is a warning
```

The compiler performs **flow analysis**: after `if (maybe is not null)`, `maybe` is treated as non-null inside that branch.

## Attributes that describe intent

When flow analysis cannot see through a method boundary, annotate it:

| Attribute | Meaning |
| --- | --- |
| `[NotNullWhen(true)]` | The out parameter is non-null when the method returns `true` |
| `[MaybeNullWhen(false)]` | The out parameter may be null when the method returns `false` |
| `[NotNullIfNotNull(nameof(input))]` | The result is non-null if that argument was |
| `[MemberNotNull(nameof(_field))]` | After this method returns, the field is initialised |
| `[DoesNotReturn]` | Control never continues past this call |

```csharp
public static bool TryParse(string? text, [NotNullWhen(true)] out Order? order) { ... }
```

## The null-forgiving operator

`value!` tells the compiler "trust me". Each use is a claim you are making without proof; a codebase that needs many of them usually has a modelling problem instead. Reserve it for cases the compiler genuinely cannot see, and add a comment saying why.

## Guarding at boundaries

Annotations disappear at runtime and callers may be untyped (JSON, reflection, older libraries). Validate at the edges:

```csharp
ArgumentNullException.ThrowIfNull(order);
ArgumentException.ThrowIfNullOrWhiteSpace(currency);
```

## Adopting it in an existing codebase

1. Turn nullable on for the whole project, so new code is analysed.
2. Set `<WarningsNotAsErrors>CS8600;CS8602;CS8604</WarningsNotAsErrors>` temporarily if the backlog is large.
3. Fix file by file. `#nullable disable` at the top of a legacy file is a valid, visible marker of remaining work.
4. Start with the domain model — annotating the core removes warnings everywhere else.

## Related operators

```csharp
var display = name ?? "unknown";       // null-coalescing
count ??= 0;                           // null-coalescing assignment
var length = customer?.Name?.Length;   // null-conditional, result is int?
```

## Further reading

- [Nullable reference types](https://learn.microsoft.com/dotnet/csharp/nullable-references)
- [Nullable attributes](https://learn.microsoft.com/dotnet/csharp/language-reference/attributes/nullable-analysis)
