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
Auth/authorization handoff guide for feature developers:
[`docs/auth-development.md`](docs/auth-development.md).

## Authorization model

Business authorization is a dynamic matrix:

- **Permission catalog** — runtime entries such as `users.read`, `invoice.export.excel`, with admin metadata (`Resource`, `ScopeMode`, `RiskLevel`). Catalog rows do not invent endpoints or policies by themselves.
- **User groups** — business groups such as leadership or department teams
- **Group permissions** — which actions a group may perform
- **Organization units** — nested tree with unlimited depth
- **Group unit scope** — a group assigned to unit X can access X and all descendants
- **User↔OU membership** — Primary/Additional organizational affiliation metadata only; does **not** grant permissions or expand accessible OU scope
- **Capabilities API** — `GET /api/authorization/me/capabilities` returns granted permission metadata + separate user↔OU rows for UI show/hide (not security)

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
- `Redis__ConnectionString` (or `ConnectionStrings__Redis` when
  `Redis:ConnectionString` is blank) — shared authorization
  cache/version and distributed auth rate limits. PostgreSQL remains the source of
  truth for users, passwords, refresh tokens, permissions, and audit.
  Production Redis should use ACL/password, TLS, and network restriction via secrets —
  never commit credentials.
- `Jwt__SigningKey` (at least 32 bytes)
- `Client__Origins__0` (HTTPS origins outside Development)

Behind a reverse proxy, set trusted addresses so `X-Forwarded-*` is not spoofable:

- `ReverseProxy__KnownProxies__0` (proxy IP)
- `ReverseProxy__KnownNetworks__0` (CIDR, for example `10.0.0.0/8`)

Optional:

- `Identity__RunSeeders=true` to run permission/admin seeding on startup
- `Identity__AllowRegistration=true` to allow self-service registration

Local infrastructure:

- PostgreSQL: Windows host on `localhost:5432` (manage with Windows pgAdmin)
- Redis: Docker Compose on `localhost:6379`

```powershell
docker compose up -d redis
```

```powershell
dotnet user-secrets set "ConnectionStrings:Database" `
  "Host=localhost;Port=5432;Database=app;Username=postgres;Password=your-password" `
  --project api/api.csproj

dotnet user-secrets set "Redis:ConnectionString" "localhost:6379" `
  --project api/api.csproj

dotnet user-secrets set "Jwt:SigningKey" `
  "replace-with-a-32-byte-or-longer-secret" `
  --project api/api.csproj
```

To seed an admin user with full authorization permissions, set both values
via user-secrets (never commit the password):

```powershell
dotnet user-secrets set "Identity:SeedAdmin:Email" "admin@localhost.dev" `
  --project api/api.csproj

dotnet user-secrets set "Identity:SeedAdmin:Password" `
  "Replace-With-A-Strong-Dev-Password1!" `
  --project api/api.csproj
```

Development already sets `Identity:RunSeeders=true` and a default seed email
(`admin@localhost.dev`). On startup the seeder creates that Identity user (when
password is present), the `System Administrators` business group, all system
permissions, and membership so you can exercise admin APIs. This is a **group**,
not an ASP.NET Identity role named ADMIN.

Refresh tokens: each login/refresh appends a hashed row; revoked rows are retained
for replay detection for `Identity:RefreshTokenRetentionDays` (default 30), then
purged by a background cleanup job. `POST /api/identity/sessions/revoke-all`
revokes every active refresh family for the current user. Access JWTs already
issued remain valid until expiry.

Cookie CSRF: browser `POST /api/identity/refresh` and `/revoke` that send the
HttpOnly cookie must present an `Origin` or `Referer` matching `Client:Origins`.
Browser `POST /login` and `/register` that include Origin/Referer are similarly
checked (login CSRF). Non-browser clients that omit Origin remain allowed.
Auth rate limiting is partitioned by client IP + path (10 req/min). When Redis is
configured, a shared Redis counter runs across replicas in addition to the
process-local ASP.NET limiter. Prefer an edge/WAF limiter as well for public
deployments. `/health/ready` probes PostgreSQL and Redis (when configured) and
must be restricted to the orchestrator network at the ingress.

Privileged groups: the seeded `System Administrators` group is `IsPrivileged`.
High-risk `authorization.*.write` permissions can only be assigned to privileged
groups, and only privileged members may change privileged membership. The last
active member of a privileged group cannot be removed (break-glass lockout
protection). Privileged groups are global and cannot be bound to organization
units. Resource APIs should use `[RequirePermissionOnUnit]`; authorization
catalog admin APIs stay global.

ASP.NET Identity **roles are unused** for access control (schema scaffolding only).
Auth metrics meter: `Net10Angular19.Auth` (`auth.login.*`, `auth.refresh.*`,
`auth.mfa.*`, `auth.rate_limited`). OpenTelemetry OTLP export is off by default;
enable with `OpenTelemetry:Enabled=true` and/or `OpenTelemetry:OtlpEndpoint`
(or `OTEL_EXPORTER_OTLP_ENDPOINT`). Permission context cache uses Redis when
configured (`Authorization:Cache:AbsoluteExpirationSeconds`, default 30) with a
shared authorization version key so revocations apply across replicas.

Authenticator MFA (TOTP): after password login, accounts with MFA enabled receive
an `MfaChallengeResponse` and must call `POST /api/identity/mfa/verify`. Enroll via
`POST /api/identity/mfa/setup/begin` then `.../confirm`. Privileged accounts cannot
disable MFA when `Identity:RequireMfaForPrivileged` is true. `GET /api/identity/me`
exposes `twoFactorEnabled`, `isPrivileged`, and `requiresMfaEnrollment`.

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
- Identity: `/api/identity/register`, `/login`, `/refresh`, `/revoke`,
  `/sessions/revoke-all`, `/me`, `/users`
- Authorization admin: `/api/authorization/*` (including update/activate,
  assignment revoke, and `/audit-events`)

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
