# Backend

.NET 10 backend organized with Clean Architecture:

- `Domain` contains business rules and has no framework dependencies.
- `Application` contains use cases and abstractions; it references only `Domain`.
- `Infrastructure` implements persistence and external integrations.
- `api` is the HTTP host and dependency-injection composition root.

Optional packages and when to enable them are listed in
[`docs/backend-capabilities.md`](docs/backend-capabilities.md).
Coding patterns for new features are in
[`docs/base-conventions.md`](docs/base-conventions.md).

## Authorization model

Business authorization is a dynamic matrix:

- **Permission catalog** — runtime entries such as `users.read`, `invoice.export.excel`
- **User groups** — business groups such as leadership or department teams
- **Group permissions** — which actions a group may perform
- **Organization units** — nested tree with unlimited depth
- **Group unit scope** — a group assigned to unit X can access X and all descendants

ASP.NET Core Identity handles authentication (users, passwords, refresh tokens).
Authorization is resolved at runtime from the database, not from static role claims
embedded in JWT.

On first run, the seeder creates system permissions, a `System Administrators`
group with all permissions, and optionally assigns the configured seed admin user
to that group.

## Local setup

Configure PostgreSQL, JWT signing key, and optional seed admin with environment
variables or .NET user secrets. Production defaults disable open registration
and startup seeding; enable them explicitly only when needed.

Required production environment variables:

- `ConnectionStrings__Database`
- `Jwt__SigningKey` (at least 32 bytes)
- `Client__Origins__0` (and additional indexed origins if needed)

Behind a reverse proxy, set trusted addresses so `X-Forwarded-*` is not spoofable:

- `ReverseProxy__KnownProxies__0` (proxy IP)
- `ReverseProxy__KnownNetworks__0` (CIDR, for example `10.0.0.0/8`)

Optional:

- `Identity__RunSeeders=true` to run permission/admin seeding on startup
- `Identity__AllowRegistration=true` to allow self-service registration

```powershell
dotnet user-secrets set "ConnectionStrings:Database" `
  "Host=localhost;Port=5432;Database=app;Username=postgres;Password=your-password" `
  --project api/api.csproj

dotnet user-secrets set "Jwt:SigningKey" `
  "replace-with-a-32-byte-or-longer-secret" `
  --project api/api.csproj
```

To seed an admin user, set `Identity:SeedAdmin:Email` and
`Identity:SeedAdmin:Password` the same way. Do not commit those values.

Restore tools, apply migrations, and run the API:

```powershell
dotnet tool restore
dotnet restore api.slnx
dotnet ef database update `
  --project Infrastructure/Infrastructure.csproj `
  --startup-project api/api.csproj
dotnet run --project api/api.csproj
```

Development endpoints:

- Scalar UI: `/scalar`
- OpenAPI document: `/openapi/v1.json`
- Health checks: `/health/live`, `/health/ready`
- Identity: `/api/identity/register`, `/login`, `/refresh`, `/revoke`, `/me`, `/users`
- Authorization admin: `/api/authorization/*`

Authentication contract for the Angular client:

- Access tokens are returned in the JSON body (`accessToken`, `accessTokenExpiresAt`)
- Refresh tokens are issued only as the HttpOnly cookie `refresh_token` (`Path=/api/identity`)
- Browser clients should call identity endpoints with credentials and use the Angular
  development proxy (`/api` → `http://localhost:5050`) so cookies stay same-origin
- `POST /api/identity/refresh` and `/revoke` read the cookie; optional body refresh tokens
  remain supported for non-browser clients

`GET /api/identity/users` requires the `users.read` permission.
Authorization admin endpoints require the corresponding `authorization.*` permissions.
Resource-scoped endpoints should use `[RequirePermissionOnUnit("invoice.read")]`
so the caller must have the permission **and** access the organization unit from
the route/query value `organizationUnitId`.

## EF Core migrations

```powershell
dotnet ef migrations add AuthorizationMatrix `
  --project Infrastructure/Infrastructure.csproj `
  --startup-project api/api.csproj `
  --output-dir Persistence/Migrations

dotnet ef database update `
  --project Infrastructure/Infrastructure.csproj `
  --startup-project api/api.csproj
```
