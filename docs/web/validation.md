---
title: Model binding and validation
description: How input becomes typed objects, where to validate, and how to report failures consistently.
order: 80
---

## Binding

Model binding turns route values, the query string, headers, form fields, and the JSON body into typed parameters. Explicit attributes beat inference whenever there is doubt:

```csharp
app.MapGet("/orders", (
    [FromQuery] int page,
    [FromQuery] int pageSize,
    [FromHeader(Name = "X-Tenant")] string tenant,
    CancellationToken ct) => ...);
```

Group many query parameters into one type with `[AsParameters]`:

```csharp
public readonly record struct OrderQuery(int Page = 1, int PageSize = 20, string? Status = null);

app.MapGet("/orders", ([AsParameters] OrderQuery query, IOrderService orders) => orders.SearchAsync(query));
```

A type can bind itself by implementing `TryParse` (for simple values) or a static `BindAsync` (for anything needing the `HttpContext`).

## Validation belongs at the edge

Validate the request model where it arrives, and keep the domain model impossible to construct in an invalid state. Two layers, two purposes: the edge produces friendly 400s, the domain protects invariants.

### Data annotations

```csharp
public sealed record CreateOrder
{
    [Required, StringLength(64)] public required string Reference { get; init; }
    [Range(0.01, 1_000_000)] public decimal Total { get; init; }
    [EmailAddress] public required string CustomerEmail { get; init; }
}
```

Enable the built-in minimal API validation filter:

```csharp
builder.Services.AddValidation();
```

### FluentValidation, when rules get real

```csharp
public sealed class CreateOrderValidator : AbstractValidator<CreateOrder>
{
    public CreateOrderValidator(ICustomerRepository customers)
    {
        RuleFor(x => x.Reference).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Total).GreaterThan(0);
        RuleFor(x => x.CustomerEmail)
            .EmailAddress()
            .MustAsync(async (email, ct) => await customers.ExistsAsync(email, ct))
            .WithMessage("Unknown customer.");
    }
}
```

Conditional rules, cross-field rules, and rules needing IO are where attributes stop being enough.

## Reporting failures

Return `ValidationProblemDetails` (RFC 9457) so every client sees one shape:

```csharp
return TypedResults.ValidationProblem(new Dictionary<string, string[]>
{
    ["total"] = ["Total must be greater than zero."]
});
```

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": { "total": ["Total must be greater than zero."] }
}
```

## Guidance

- 400 for a malformed or invalid request; 422 only if you distinguish "syntactically valid but semantically rejected" and document it.
- Never trust a client-side check; the API is the boundary.
- Validate identifiers you will use in a query even when the route constraint already matched — constraints produce 404s, not messages.

## Further reading

- [Model binding](https://learn.microsoft.com/aspnet/core/mvc/models/model-binding)
- [Problem Details (RFC 9457)](https://www.rfc-editor.org/rfc/rfc9457)
