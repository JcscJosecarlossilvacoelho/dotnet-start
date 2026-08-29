---
title: API design
description: Resources, status codes, versioning, and idempotency — the decisions clients live with.
order: 20
---

An API is a contract you cannot easily change. Spend the extra hour before the first client integrates.

## Resources and verbs

```
GET    /v1/orders?status=pending&page=2
POST   /v1/orders
GET    /v1/orders/{id}
PUT    /v1/orders/{id}          # full replace, idempotent
PATCH  /v1/orders/{id}          # partial update
DELETE /v1/orders/{id}
POST   /v1/orders/{id}/cancel   # an action that is not CRUD
```

Nouns for resources, plural, lowercase, hyphenated. When an operation is genuinely a verb (cancel, refund, retry), a sub-resource action is clearer than contorting it into a PATCH.

## Status codes

Return the code that describes what happened: 201 with a `Location` for creation, 204 for a successful delete, 409 for a conflicting state, 422 if you distinguish semantic rejection from malformed input. Use [`ProblemDetails`](/docs/web/error-handling) for every error body so clients parse one shape.

## Payload conventions

- Pick a casing (camelCase is the .NET default) and never vary it.
- Dates as RFC 3339 UTC strings; money as a decimal string plus a currency code, never a float.
- Enums as strings — numbers become meaningless the moment someone reorders them.
- Return objects at the top level, not arrays, so you can add fields (`{ "items": [...], "next": "..." }`).

## Paging

```json
{ "items": [ ... ], "next": "eyJwbGFjZWQiOiIyMDI2LTA4LTI5In0" }
```

Cursor paging is stable under concurrent writes and stays fast at depth; offset paging is easier and fine for small, static datasets. Whichever you pick, always cap `pageSize` server-side.

## Idempotency

Any POST that costs money or sends something must accept an idempotency key:

```
POST /v1/payments
Idempotency-Key: 0f9a...
```

Store the key with the result and return the original response on a repeat. Without it, a client retry — which *will* happen — is a duplicate charge. See [Resilience](/docs/ops/resilience).

## Versioning

Version in the path (`/v1/`) for simplicity and cacheability. Additive changes need no new version; removals, renames, and tightened validation do. Publish a deprecation date, send a `Sunset` header, and mean it.

## Documented, generated, and tested

Generate the [OpenAPI document](/docs/web/openapi) from the code, commit it, and fail CI when it changes unexpectedly. That single practice turns "we accidentally broke a client" into a review comment.

## Further reading

- [Web API design best practices](https://learn.microsoft.com/azure/architecture/best-practices/api-design)
