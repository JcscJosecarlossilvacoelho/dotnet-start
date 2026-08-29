---
title: OpenAPI and API documentation
description: Generating an accurate contract from the code, and keeping it accurate.
order: 100
---

ASP.NET Core generates OpenAPI documents from your endpoints — no separate package required.

```csharp
builder.Services.AddOpenApi();

var app = builder.Build();
app.MapOpenApi();                 // serves /openapi/v1.json
```

Add a UI in development with Scalar, Swagger UI, or Redoc:

```csharp
if (app.Environment.IsDevelopment())
    app.MapScalarApiReference();
```

## Making the document accurate

The generator can only describe what it can see. Give it the shape:

```csharp
orders.MapGet("/{id:guid}", GetOrder)
      .WithName("GetOrder")
      .WithSummary("Fetch a single order")
      .WithDescription("Returns the order and its lines. Requires the orders:read scope.")
      .Produces<Order>(StatusCodes.Status200OK)
      .ProducesProblem(StatusCodes.Status404NotFound);
```

Returning `Results<Ok<Order>, NotFound>` from the handler declares the same thing in the type system, and the compiler keeps it honest — prefer that where you can.

## XML comments

```xml
<GenerateDocumentationFile>true</GenerateDocumentationFile>
```

Summaries on your DTO properties flow into the schema, which is where consumers actually read them.

## Versioning

Version in the route (`/v1/orders`) or in a header, but pick one and be consistent. With `Asp.Versioning.Http` you can generate a document per version:

```csharp
var versioned = app.NewVersionedApi("Orders");
var v1 = versioned.MapGroup("/v{version:apiVersion}/orders").HasApiVersion(1.0);
```

Additive changes (a new optional field, a new endpoint) do not need a version. Removing a field, renaming one, or tightening validation does.

## Using the document

- **Client generation** — `kiota`, NSwag, or OpenAPI Generator produce typed clients from the JSON.
- **Contract testing** — commit the generated document and fail CI when it changes unexpectedly:

  ```bash
  dotnet build
  git diff --exit-code artifacts/openapi/v1.json
  ```

- **Agent context** — a current OpenAPI document is the fastest way to give a coding agent an accurate picture of your API.

## Further reading

- [OpenAPI support in ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/overview)
