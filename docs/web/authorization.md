---
title: Authorization
description: Roles, policies, claims, and resource-based checks — deciding what an authenticated caller may do.
order: 130
---

Authorization runs after authentication and, because it runs after routing, knows which endpoint was selected and what it requires.

## Start with policies, not roles

Roles scattered through attributes turn every permission change into a code change. A policy names the *requirement*; the mapping to roles or claims lives in one place.

```csharp
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("orders:read",  p => p.RequireClaim("scope", "orders.read"))
    .AddPolicy("orders:write", p => p.RequireClaim("scope", "orders.write"))
    .AddPolicy("back-office",  p => p.RequireRole("Operations", "Support"))
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
```

A **fallback policy** makes every endpoint protected unless it opts out with `AllowAnonymous` — secure by default, which is the only default worth having.

```csharp
orders.MapGet("/", ListOrders).RequireAuthorization("orders:read");
app.MapGet("/health", () => Results.Ok()).AllowAnonymous();
```

## Custom requirements

When a rule needs more than a claim check:

```csharp
public sealed record MinimumAgeRequirement(int Years) : IAuthorizationRequirement;

public sealed class MinimumAgeHandler(TimeProvider time)
    : AuthorizationHandler<MinimumAgeRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, MinimumAgeRequirement requirement)
    {
        var claim = context.User.FindFirst(c => c.Type == ClaimTypes.DateOfBirth);
        if (claim is not null && DateTime.TryParse(claim.Value, out var dob))
        {
            var age = (time.GetUtcNow().Year - dob.Year);
            if (age >= requirement.Years) context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
```

Register the handler as a singleton and reference the requirement from a policy.

## Resource-based authorization

"Can this user edit *this* order?" cannot be answered before the order is loaded:

```csharp
var authorization = await authorizationService.AuthorizeAsync(user, order, "order-owner");
if (!authorization.Succeeded) return TypedResults.Forbid();
```

The handler receives both the principal and the resource. This is the correct place for ownership and tenancy checks.

## Multi-tenancy

Never take the tenant from the request body or query string. Take it from the token (a `tenant_id` claim) or a verified host mapping, put it in a scoped service, and apply it as a filter in the data layer — an [EF Core global query filter](/docs/data/querying) is the enforcement point that cannot be forgotten.

## Testing it

Authorization is exactly the code that must not regress. Write integration tests that call a protected endpoint anonymously (401), as the wrong user (403), and as the right user (200). See [Integration testing](/docs/testing/integration-testing).

## Further reading

- [Authorization in ASP.NET Core](https://learn.microsoft.com/aspnet/core/security/authorization/introduction)
