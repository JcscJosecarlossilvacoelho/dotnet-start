---
title: Minimal APIs
description: The endpoint model — parameter binding, results, grouping, filters, and how to keep it organised as it grows.
order: 20
---

Minimal APIs map a route directly to a delegate. There is no controller, no convention to learn, and no reflection at runtime when the request delegate generator is enabled.

## Mapping endpoints

```csharp
app.MapGet("/orders/{id:guid}", GetOrder);
app.MapPost("/orders", CreateOrder);
app.MapPut("/orders/{id:guid}", UpdateOrder);
app.MapDelete("/orders/{id:guid}", DeleteOrder);
```

Handlers can be lambdas, local functions, static methods, or instance methods — a named static method is easier to test and to read.

## Parameter binding

The framework infers each parameter's source:

| Parameter | Bound from |
| --- | --- |
| Matches a route token | The route |
| Simple type, no match | The query string |
| Complex type | The JSON body |
| Registered in DI | The service provider |
| `HttpContext`, `ClaimsPrincipal`, `CancellationToken` | The request |

Be explicit when inference would be wrong: `[FromQuery]`, `[FromHeader]`, `[FromBody]`, `[FromServices]`, `[AsParameters]`.

```csharp
static async Task<Results<Ok<Order>, NotFound>> GetOrder(
    Guid id,
    IOrderService orders,
    CancellationToken cancellationToken)
{
    var order = await orders.GetAsync(id, cancellationToken);
    return order is null ? TypedResults.NotFound() : TypedResults.Ok(order);
}
```

## Returning results

Prefer `TypedResults` over `Results`: the return type documents every status code the endpoint can produce, which feeds [OpenAPI](/docs/web/openapi) automatically and is checked by the compiler.

| Helper | Status |
| --- | --- |
| `TypedResults.Ok(value)` | 200 |
| `TypedResults.Created($"/orders/{id}", value)` | 201 |
| `TypedResults.NoContent()` | 204 |
| `TypedResults.ValidationProblem(errors)` | 400 with `ProblemDetails` |
| `TypedResults.NotFound()` | 404 |
| `TypedResults.Problem(...)` | 500 with `ProblemDetails` |

## Groups keep it organised

```csharp
var orders = app.MapGroup("/orders")
                .RequireAuthorization()
                .WithTags("Orders")
                .AddEndpointFilter<ValidationFilter>();

orders.MapGet("/{id:guid}", GetOrder).WithName("GetOrder");
orders.MapPost("/", CreateOrder);
```

A group applies metadata, filters, and policies once for every endpoint inside it.

## Endpoint filters

Filters run around the handler — the minimal API equivalent of MVC action filters, without the reflection:

```csharp
public sealed class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var argument = context.Arguments.OfType<T>().FirstOrDefault();
        if (argument is null) return await next(context);

        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(argument, new ValidationContext(argument), results, true))
            return TypedResults.ValidationProblem(results.ToDictionary(
                r => r.MemberNames.FirstOrDefault() ?? "", r => new[] { r.ErrorMessage ?? "" }));

        return await next(context);
    }
}
```

## Structure at scale

Keep `Program.cs` small by giving each feature its own registration method:

```csharp
// Features/Orders/OrderEndpoints.cs
public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrders(this IEndpointRouteBuilder app) { ... return app; }
}

// Program.cs
app.MapOrders().MapCustomers().MapPayments();
```

This is the vertical-slice shape described in [Project structure](/docs/architecture/project-structure).

## Further reading

- [Minimal APIs overview](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/overview)
