# Backend conventions (training)

This document is the pattern sheet for new backend work. Copy an existing vertical
slice instead of inventing a new style.

## Request flow

```text
HTTP
  → CorrelationId + security headers
  → CORS / rate limiter
  → Authentication (JWT)
  → Authorization (fallback authenticated + [RequirePermission])
  → Controller (orchestration only)
  → Application use case + FluentValidation
  → Infrastructure (EF Core, Identity, stores)
  → Result → ProblemDetails or DTO
```

[`Program.cs`](../api/Program.cs) is the composition root only: configuration,
DI, middleware order, and endpoint mapping.

## Layers

| Layer | Allowed | Forbidden |
|---|---|---|
| Domain | entities, value objects, permission matching | EF, ASP.NET, HTTP |
| Application | use cases, DTOs, validators, `Result` | `HttpContext`, `DbContext` |
| Infrastructure | EF, Identity, JWT issuance, seeders | HTTP status codes |
| Api | controllers, middleware, auth attributes | business rules |

## Adding a feature

1. Domain entity or permission code if needed.
2. Request/response `sealed record` DTOs with XML `<summary>`.
3. FluentValidation validator next to the request.
4. Use case class `HandleAsync(..., CancellationToken)`.
5. Register the use case in [`Application/DependencyInjection.cs`](../Application/DependencyInjection.cs).
6. Controller action: call use case, map `Result` with `ToActionResult` / `ToCreatedAtAction`.
7. Protect with `[RequirePermission]` or `[RequirePermissionOnUnit]` — never a raw role check.
8. Add `.http` examples and tests.

## Authorization

- JWT proves **who** the caller is. It does not carry permission claims.
- `[RequirePermission("invoice.read")]` asks the database: does this user currently have that permission via an active group?
- Permission decisions are cached in-process briefly (`Authorization:Cache:AbsoluteExpirationSeconds`, default 30s) and invalidated immediately in the same process when authorization rows change. Multi-instance deployments still wait for TTL unless a shared cache is added later.
- Do **not** use ASP.NET Identity roles for authorization. `ApplicationRole` / AspNetRoles exist only for Identity schema compatibility; policies must use permission codes / groups.
- `[RequirePermissionOnUnit("invoice.read", "organizationUnitId")]` also checks organization-unit scope (the unit from the route/query and its descendants). **Use this for resource APIs.**
- Authorization catalog admin endpoints (`/api/authorization/*` mutations) are intentionally **global** (`[RequirePermission]` only). Privileged groups (`IsPrivileged`, e.g. System Administrators) hold high-risk `authorization.*.write` permissions and must not be OU-scoped.
- High-risk write permissions may only be assigned to privileged groups; privileged membership changes require a privileged actor.
- `GET /api/authorization/me` is for the signed-in user only; admin catalog APIs need `authorization.*` permissions.

## HTTP contract

- Success: DTO body. Create: `201` + `Location` of `GET .../{id}`.
- Failure: RFC 7807 Problem Details with `code` and `traceId`.
- Validation: `400` + `ValidationProblemDetails.errors`.
- Do not return EF entities.

## Logging

- Structured Serilog only.
- Never log passwords, tokens, or connection strings.
- Correlation id header: `X-Correlation-Id`.
