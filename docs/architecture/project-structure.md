---
title: Structuring an application
description: Layers, vertical slices, and how to decide where a piece of code belongs.
order: 10
---

Two structures dominate .NET codebases. They are not rivals; they answer different questions.

## Layers

Group by technical role — `Api`, `Application`, `Domain`, `Infrastructure` — with dependencies pointing inward. `Domain` references nothing; `Infrastructure` implements interfaces the inner layers declare.

**Good at:** enforcing that business rules cannot depend on the database or the web framework.
**Bad at:** locality. One feature is spread across four projects, so every change touches all of them.

## Vertical slices

Group by feature. Everything for placing an order — endpoint, request model, validation, handler, persistence — lives in one folder.

```
Features/
  Orders/
    PlaceOrder.cs          # request, validator, handler, endpoint mapping
    GetOrder.cs
    OrderEndpoints.cs
  Payments/
Domain/
  Order.cs
Infrastructure/
  AppDbContext.cs
```

**Good at:** change locality. A new feature is a new file; deleting one deletes a file.
**Bad at:** on its own, nothing stops a slice from doing whatever it likes to the database.

## What actually works

Vertical slices for the application code, plus a small, protected domain and a shared infrastructure layer. The domain holds the invariants; the slices hold the use cases. Keep the boundary honest with a project reference or an architecture test:

```csharp
[Fact]
public void Domain_does_not_reference_infrastructure() =>
    Assert.DoesNotContain(
        typeof(Order).Assembly.GetReferencedAssemblies(),
        a => a.Name?.Contains("Infrastructure") == true);
```

## When to add a project

Add one when you want the compiler to enforce a rule you keep breaking. Do not add one because a diagram has four boxes: each project costs build time, navigation, and a NuGet-versioning conversation later.

A service under a few thousand lines is usually best as **one project with folders**. Split later, when the pain is real and the seam is obvious.

## Naming and predictability

- One public type per file, named after the file.
- Namespaces mirror folders.
- The same concept has the same name everywhere — `Order`, not `OrderDto`, `OrderModel`, `OrderEntity`, and `OrderVm` for four views of one idea.

Predictable placement is what lets a newcomer — or a coding agent — find the right file on the first try. See [Working with coding agents](/docs/architecture/coding-agents).

## Further reading

- [Architecture guides](https://learn.microsoft.com/dotnet/architecture/)
