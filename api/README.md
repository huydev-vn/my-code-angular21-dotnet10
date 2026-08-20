# Backend

.NET 10 backend organized with Clean Architecture:

- `Domain` contains business rules and has no framework dependencies.
- `Application` contains use cases and abstractions; it references only `Domain`.
- `Infrastructure` implements persistence and external integrations.
- `api` is the HTTP host and dependency-injection composition root.

Optional packages and when to enable them are listed in
[`docs/backend-capabilities.md`](docs/backend-capabilities.md).

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

Configure PostgreSQL and the JWT signing key with environment variables or
.NET user secrets. The committed JWT key is local-development only.

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
- Health check: `/health`
- Identity: `/api/identity/register`, `/login`, `/refresh`, `/revoke`, `/me`, `/users`
- Authorization admin: `/api/authorization/*`

`GET /api/identity/users` requires the `users.read` permission.
Authorization admin endpoints require the corresponding `authorization.*` permissions.

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
