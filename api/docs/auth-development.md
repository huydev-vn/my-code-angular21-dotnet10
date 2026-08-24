# Auth development guide

Base authentication and authorization for the ASP.NET Core API. Use this when
adding business features on top of the existing auth foundation.

## Local startup

```powershell
# From repo root
docker compose up -d postgres redis

cd api
dotnet user-secrets set "ConnectionStrings:Database" "Host=localhost;Port=5432;Database=app;Username=postgres;Password=postgres" --project api/api.csproj
dotnet user-secrets set "Redis:ConnectionString" "localhost:6379" --project api/api.csproj
dotnet user-secrets set "Jwt:SigningKey" "DEV-ONLY-CHANGE-ME-USE-USER-SECRETS!!" --project api/api.csproj

dotnet tool restore
dotnet ef database update --project Infrastructure/Infrastructure.csproj --startup-project api/api.csproj
dotnet run --project api/api.csproj
```

Redis may also be set via `ConnectionStrings:Redis` when `Redis:ConnectionString` is blank.

## Identity API contract

| Endpoint | Auth | Notes |
|----------|------|-------|
| `POST /api/identity/login` | anonymous + rate limit | Password login. May return access token **or** MFA challenge. |
| `POST /api/identity/mfa/verify` | anonymous + rate limit | Completes login with TOTP; sets refresh cookie. |
| `POST /api/identity/mfa/setup/begin` | bearer | Starts authenticator enrollment. |
| `POST /api/identity/mfa/setup/confirm` | bearer | Confirms enrollment with TOTP. |
| `POST /api/identity/mfa/disable` | bearer | Blocked for privileged users when `RequireMfaForPrivileged` is true. |
| `POST /api/identity/refresh` | anonymous + rate limit | Cookie or body refresh token; rotates family. |
| `POST /api/identity/revoke` | anonymous + rate limit | Revokes current refresh family. |
| `POST /api/identity/sessions/revoke-all` | bearer | Revokes all refresh families for the user. |
| `GET /api/identity/me` | bearer | Includes MFA/privileged flags. |
| `GET /api/identity/users` | `users.read` | Paginated user list. |

### Login outcomes

Success without MFA:

```json
{ "accessToken": "...", "accessTokenExpiresAt": "..." }
```

Plus `Set-Cookie: refresh_token=...; HttpOnly; Path=/api/identity; SameSite=Lax`.

MFA required:

```json
{ "mfaToken": "...", "expiresAt": "..." }
```

Then `POST /api/identity/mfa/verify` with `{ "mfaToken", "code" }`.

### Refresh / CSRF

- Browser cookie refresh/revoke must send `Origin` or `Referer` matching `Client:Origins`.
- Body refresh token without cookie skips CSRF (non-browser clients).
- Access JWTs already issued remain valid until expiry after revoke.

## Authorization model

- Catalog: permission definitions.
- Groups: business groups; privileged groups hold high-risk write permissions.
- Organization units: nested tree; group scope includes descendants.
- Runtime decisions come from PostgreSQL (cached in Redis when configured).
- Do **not** embed permission lists in JWT claims as the source of truth.

Admin APIs live under `/api/authorization/*` and require `authorization.*` permissions.
Current user context: `GET /api/authorization/me`.

## Rules when adding a business endpoint

1. Keep the controller thin — call an Application handler.
2. Require authentication (`[Authorize]` or fallback policy).
3. Add a specific permission with `[RequirePermission("feature.action")]`.
4. If the resource is OU-scoped, use `[RequirePermissionOnUnit("feature.action")]` and accept `organizationUnitId`.
5. Accept/return DTOs only; pass `CancellationToken`.
6. Return `ProblemDetails` via existing result mapping helpers.
7. Never log secrets (passwords, tokens, MFA codes).
8. Do not use ASP.NET Identity roles for authorization.

## Redis boundary

Redis is **not** an auth database. Allowed uses only:

- Authorization context cache + shared version key
- Short-lived MFA challenge tickets
- Distributed auth rate-limit counters

PostgreSQL remains authoritative for users, passwords, refresh tokens, permissions, groups, OUs, and audit.

## Deploy order

1. Apply EF migrations once (CI/CD job or operator), never from every replica startup.
2. Ensure Redis is reachable (production readiness checks Redis when configured).
3. Deploy API replicas.
4. Keep `Identity:RunSeeders=false` outside Development.

## Production Redis

Use managed Redis or Redis with ACL/password, TLS, and network restriction. Do not commit credentials. Prefer environment variables / secret store:

- `Redis__ConnectionString`
- `ConnectionStrings__Database`
- `Jwt__SigningKey`
- `Client__Origins__0` (HTTPS)

## Handoff checklist (base auth)

- [ ] Local: Postgres + Redis via Docker; migrations applied; API runs.
- [ ] Team read `docs/auth-development.md` and `.cursor/rules/backend-auth.mdc`.
- [ ] Login / MFA / refresh cookie / CSRF contracts understood by frontend.
- [ ] New business endpoints use permission attributes + DTOs only.
- [ ] CI green: build, tests (including Redis coordination), migration smoke.
- [ ] Production secrets set outside repo; Redis has auth/TLS/network controls.
- [ ] Deploy order: migrate once → Redis healthy → API replicas (`RunSeeders=false`).
