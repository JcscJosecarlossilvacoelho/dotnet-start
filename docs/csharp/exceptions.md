---
title: Exceptions and error handling
description: Which failures deserve exceptions, which deserve results, and how to keep both useful in production.
order: 80
---

## Exceptions are for the exceptional

Use an exception when the caller could not reasonably have prevented the failure and cannot sensibly continue: a dropped connection, a corrupted file, a bug. Use a **result** when failure is an expected outcome of a normal path: validation, "not found", a declined payment.

```csharp
// expected outcome, part of the contract
public Result<Order> Place(OrderRequest request);

// exceptional: the caller violated the contract
ArgumentOutOfRangeException.ThrowIfNegative(quantity);
```

The cost argument is secondary but real: throwing is orders of magnitude more expensive than returning, so exceptions in a per-item loop are a performance problem as well as a design one.

## Throwing well

```csharp
ArgumentNullException.ThrowIfNull(customer);
ArgumentException.ThrowIfNullOrWhiteSpace(currency);
ObjectDisposedException.ThrowIf(_disposed, this);

throw new InvalidOperationException($"Order {id} is already shipped.");
```

Include the values that identify the failure. A message without the id is a message that costs someone an hour.

## Catching well

Catch what you can act on. `catch (Exception)` at a boundary — a request handler, a message consumer, a background loop — is legitimate; the same catch inside a helper method usually hides a bug.

```csharp
try
{
    await _payments.ChargeAsync(order, cancellationToken);
}
catch (HttpRequestException ex) when (ex.StatusCode >= HttpStatusCode.InternalServerError)
{
    _logger.LogWarning(ex, "Payment provider unavailable for order {OrderId}", order.Id);
    throw new PaymentUnavailableException(order.Id, ex);
}
```

Exception filters (`when`) evaluate **before** the stack unwinds, which keeps the original stack intact for debugging.

## Rethrowing

`throw;` preserves the stack trace. `throw ex;` resets it and destroys the evidence. When wrapping, always pass the original as `innerException`.

## Cancellation

`OperationCanceledException` signals cooperative shutdown, not failure. Do not log it as an error:

```csharp
catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
{
    return; // expected during shutdown
}
```

## Custom exception types

Add one only when a caller will catch it specifically. Give it the standard constructors, make it `sealed`, and carry the identifying data as properties rather than only in the message.

## At the HTTP boundary

Map exceptions to responses in one place with `IExceptionHandler` and return `ProblemDetails`, so clients get a consistent shape and stack traces never leak. See [Error handling in ASP.NET Core](/docs/web/error-handling).

## Further reading

- [Best practices for exceptions](https://learn.microsoft.com/dotnet/standard/exceptions/best-practices-for-exceptions)
