---
title: Generics and constraints
description: How generics are specialised at runtime, what each constraint buys you, and generic math.
order: 60
---

Generics give type safety without boxing and without duplication. Unlike Java, .NET generics are **reified**: type arguments exist at runtime, so `typeof(List<int>)` is a real, distinct type.

## Specialisation

The runtime generates one native code body per **value type** argument (`List<int>` and `List<double>` are separate machine code) and shares one body across all **reference type** arguments. That is why generic code over structs has no boxing cost.

## Constraints

```csharp
where T : class            // reference type
where T : struct           // non-nullable value type
where T : notnull          // neither null nor a nullable value type
where T : new()            // has a public parameterless constructor
where T : IComparable<T>   // implements an interface
where T : Base             // derives from a class
where T : unmanaged        // no references; usable with pointers and Span
where T : allows ref struct// accepts ref struct arguments
```

Constraints are not restrictions for their own sake: each one unlocks operations inside the method body.

## Variance

```csharp
IEnumerable<out T>   // covariant: IEnumerable<string> is an IEnumerable<object>
IComparer<in T>      // contravariant: IComparer<object> can compare strings
```

`out` means T only appears in output positions; `in` means only in input positions. `List<T>` is invariant because it is both.

## Generic math

Static abstract interface members let you write arithmetic over any numeric type:

```csharp
public static T Sum<T>(IEnumerable<T> values) where T : INumber<T>
{
    var total = T.Zero;
    foreach (var value in values) total += value;
    return total;
}
```

`INumber<T>`, `IAdditionOperators<,,>`, `IParsable<T>`, and `ISpanFormattable` are the building blocks. This replaces the old pattern of one overload per numeric type.

## Practical guidance

- Do not add a type parameter that appears exactly once — it usually means an interface parameter is what you wanted.
- Static fields on a generic type are **per closed type**: `Cache<int>.Items` and `Cache<string>.Items` are different fields. This is a useful trick for per-type caches and a subtle bug when unintended.
- Generic type arguments participate in [Native AOT](/docs/runtime/native-aot) trimming analysis; avoid constructing closed generic types by reflection when you plan to publish AOT.

## Further reading

- [Generics in .NET](https://learn.microsoft.com/dotnet/standard/generics/)
- [Generic math](https://learn.microsoft.com/dotnet/standard/generics/math)
