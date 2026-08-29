---
title: Pattern matching
description: Every pattern form, and how switch expressions replace branching trees.
order: 30
---

Pattern matching turns "inspect, cast, branch" into a single expression the compiler can check for exhaustiveness.

## The pattern forms

```csharp
obj is Order order                       // declaration pattern
obj is Order { Total: > 100 } big        // property pattern
obj is (var x, var y)                    // positional pattern (needs Deconstruct)
value is > 0 and < 100                   // relational + logical patterns
value is null or ""                      // constant + or pattern
value is not null                        // negation
list is [var first, .., var last]        // list pattern with a slice
list is []                               // empty collection
```

Patterns nest freely, which is what makes them worth learning:

```csharp
if (response is { Status: HttpStatusCode.OK, Content.Headers.ContentLength: > 0 })
```

## Switch expressions

```csharp
public static decimal Fee(Payment payment) => payment switch
{
    { Method: PaymentMethod.Card, Amount: <= 10m } => 0.30m,
    { Method: PaymentMethod.Card } p               => p.Amount * 0.014m,
    { Method: PaymentMethod.Transfer }             => 0m,
    null => throw new ArgumentNullException(nameof(payment)),
    _    => throw new NotSupportedException($"Unknown method {payment.Method}")
};
```

Arms are evaluated in order, so put the specific cases first. If the compiler cannot prove the switch is exhaustive it warns (CS8509) — treat that warning as an error and the compiler becomes a check on your domain modelling.

## Guards

`when` adds a condition a pattern cannot express:

```csharp
var band = age switch
{
    < 0 => throw new ArgumentOutOfRangeException(nameof(age)),
    var a when a < 13 => "child",
    var a when a < 20 => "teen",
    _ => "adult"
};
```

## Where it pays off most

- **Parsing and dispatch** — replacing chains of `if (x is T)` and casts.
- **Domain rules** — pricing, state machines, permission checks read as tables.
- **Result handling** — matching on a `Result<T>` hierarchy of records.

## Where to stop

A switch expression with fifteen arms and nested guards is a table pretending to be code. When the rules are data, put them in data (a dictionary, a configuration file, a database) and keep the code that reads them small.

## Further reading

- [Pattern matching (C# reference)](https://learn.microsoft.com/dotnet/csharp/language-reference/operators/patterns)
