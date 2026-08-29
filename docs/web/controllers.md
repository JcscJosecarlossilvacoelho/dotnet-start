---
title: Controllers and MVC
description: The convention-based endpoint model, filters, and when it beats minimal APIs.
order: 170
---

Controllers are still fully supported and are the better fit when you want conventions, filters, and model binding to do the repetitive work across dozens of similar endpoints.

```csharp
[ApiController]
[Route("v1/[controller]")]
public sealed class OrdersController(IOrderService orders) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType<Order>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Order>> Get(Guid id, CancellationToken ct)
        => await orders.GetAsync(id, ct) is { } order ? Ok(order) : NotFound();

    [HttpPost]
    [Authorize("orders:write")]
    public async Task<ActionResult<Order>> Create(CreateOrder request, CancellationToken ct)
    {
        var order = await orders.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = order.Id }, order);
    }
}
```

`[ApiController]` turns on automatic 400 responses for invalid models, binding source inference, and `ProblemDetails` for error status codes.

## Filters

Filters are the MVC cross-cutting mechanism, running in a defined order around the action:

| Filter | Runs |
| --- | --- |
| Authorization | First; short-circuits unauthorised requests |
| Resource | Around model binding — useful for caching |
| Action | Before and after the action method |
| Exception | When an action throws |
| Result | Around result execution |

```csharp
public sealed class AuditFilter(ILogger<AuditFilter> logger) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        logger.LogInformation("Executing {Action}", context.ActionDescriptor.DisplayName);
        var executed = await next();
        if (executed.Exception is not null) logger.LogError(executed.Exception, "Action failed");
    }
}
```

Register globally, per controller, or per action.

## Controllers or minimal APIs?

| Prefer controllers when | Prefer minimal APIs when |
| --- | --- |
| Many endpoints share filters and conventions | Endpoints are few or heterogeneous |
| The team already knows MVC | Startup time and AOT matter |
| You use model-level conventions heavily | You want explicit, readable wiring per endpoint |

Both use the same routing, DI, binding, and authorization. Mixing them in one application is fine and common: controllers for the large CRUD surface, minimal APIs for health, webhooks, and internal endpoints.

## Server-rendered UI

For HTML, the same runtime offers **Razor Pages** (page-focused, less ceremony than MVC views) and **Blazor** (component-based, interactive). See [Blazor](/docs/ui/blazor).

## Further reading

- [Controller-based APIs](https://learn.microsoft.com/aspnet/core/web-api/)
