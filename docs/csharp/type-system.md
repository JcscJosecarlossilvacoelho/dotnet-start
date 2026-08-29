---
title: The type system
description: Classes, structs, records, and interfaces — what each one costs and when to reach for it.
order: 10
---

C# has two families of types. Everything else follows from the difference.

## Value types and reference types

| | Value types (`struct`, `enum`, primitives) | Reference types (`class`, `interface`, `record`, arrays, `string`) |
| --- | --- | --- |
| Storage | Inline: stack, or inside the containing object | On the managed heap |
| Assignment | Copies the whole value | Copies the reference |
| Default | All fields zeroed; never `null` (unless `T?`) | `null` |
| Equality (default) | Field-by-field | Reference identity |
| Cost | No allocation; copying cost grows with size | Allocation plus GC pressure |

A struct larger than about 16–24 bytes that is passed around frequently usually costs more in copying than a class costs in allocation. Measure before optimising.

## Declaring types

```csharp
public sealed class Order            // reference type, identity matters
{
    public required Guid Id { get; init; }
    public required Customer Customer { get; init; }
    private readonly List<OrderLine> _lines = [];
    public IReadOnlyList<OrderLine> Lines => _lines;
}

public readonly record struct Money(decimal Amount, string Currency);  // small, immutable, compared by value

public record Customer(Guid Id, string Name);   // reference type with value equality
```

- `sealed` by default on classes: it documents intent and lets the JIT devirtualise calls.
- `required` forces the initialiser to supply a member, replacing most constructor boilerplate.
- `readonly record struct` is the right shape for small value objects — no allocation, structural equality, immutability enforced by the compiler.

## Records

A `record` generates a value-based `Equals`/`GetHashCode`, a readable `ToString`, a deconstructor, and a `with` expression for non-destructive mutation:

```csharp
var updated = customer with { Name = "Ana Silva" };
```

Use records for data whose identity *is* its contents: DTOs, events, query results, value objects. Use classes when the object has identity and behaviour that changes over time.

## Interfaces

Interfaces can carry default implementations and static abstract members:

```csharp
public interface IParsable<TSelf> where TSelf : IParsable<TSelf>
{
    static abstract TSelf Parse(string s, IFormatProvider? provider);
}
```

Static abstract members are what make generic math work: a generic method can constrain `T` to a type that provides `+`, `Zero`, or `Parse`.

## Inheritance, sparingly

Prefer composition and interfaces. Deep hierarchies distribute a single behaviour across files and make change expensive. When you do inherit, make the base class `abstract` and its extension points explicit.

## Type-level defaults worth adopting

- Enable [nullable reference types](/docs/csharp/nullable-reference-types) and treat warnings as errors.
- Make types immutable unless mutation is required.
- Keep constructors free of side effects: no IO, no thread starts.

## Further reading

- [Types (C# language reference)](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/types)
- [Records](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/record)
