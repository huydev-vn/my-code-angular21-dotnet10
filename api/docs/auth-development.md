# Auth development guide

Base authentication and authorization for the ASP.NET Core API. Use this when
adding business features on top of the existing auth foundation.

## Local startup

```powershell
# From repo root — Redis only (PostgreSQL runs on the Windows host, port 5432)
docker compose up -d redis

cd api
dotnet user-secrets set "ConnectionStrings:Database" "Host=localhost;Port=5432;Database=app;Username=postgres;Password=postgres" --project api/api.csproj
dotnet user-secrets set "Redis:ConnectionString" "localhost:6379" --project api/api.csproj
dotnet user-secrets set "Jwt:SigningKey" "DEV-ONLY-CHANGE-ME-USE-USER-SECRETS!!" --project api/api.csproj

dotnet tool restore
dotnet ef database update --project Infrastructure/Infrastructure.csproj --startup-project api/api.csproj
dotnet run --project api/api.csproj
```

Use Windows pgAdmin against `localhost:5432` for the host PostgreSQL. Redis may also be set via `ConnectionStrings:Redis` when `Redis:ConnectionString` is blank.

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
- Login / register / MFA verify always require a trusted `Origin` or `Referer` (fail closed if both are missing).
- Body refresh token without cookie skips CSRF (non-browser clients).
- Access JWTs already issued remain valid until expiry after revoke (default lifetime: `Jwt:AccessTokenMinutes`, typically 10 minutes).
- Concurrent refresh retries within a short grace window do not revoke the whole family; true reuse still does.
- Logout/revoke-all serializes with refresh rotation so logout wins (no new refresh token after revoke).

## Authorization model

- Catalog: permission definitions with metadata (`Resource`, `Action`, `ScopeMode`, `RiskLevel`, `IsSystemManaged`).
- Catalog entries do **not** auto-create endpoints, query filters, or executable policies — they describe grants for admin configuration and future enforcement.
- `ScopeMode`: `None` (capability only), `OrganizationUnit` (data scoped to accessible OUs), `Global` (no OU filter). `Owner` is deferred until owner filtering exists.
- `RiskLevel`: `Low`–`Critical`; `Critical` (and hard-coded `authorization.*.write` codes) are assignable only to privileged groups.
- Groups: business groups; privileged groups hold high-risk write permissions.
- Organization units: nested tree; group scope includes descendants.
  Move: `POST /api/authorization/organization-units/{id}/move` with `{ "parentId": "<guid>|null" }`
  (`authorization.organization-units.write`). Cycle checks reject ancestor loops; non-privileged
  actors must keep both the unit and new parent in their accessible OU set (privileged bypass).
- Runtime decisions come from PostgreSQL (cached in Redis when configured).
- Do **not** embed permission lists in JWT claims as the source of truth.

Admin APIs live under `/api/authorization/*` and require `authorization.*` permissions.
Current user context: `GET /api/authorization/me`.
UI capabilities (permission metadata + separate user↔OU membership): `GET /api/authorization/me/capabilities`.
Angular (and any client) should call capabilities for show/hide only — never treat UI as security; the API enforces permissions on every request.

### Two-tier admin permissions (Agent D)

| Tier | Permissions | Who | Authority |
|------|-------------|-----|-----------|
| System | `authorization.groups.write`, `authorization.organization-units.write`, `authorization.permissions.write` | Privileged-group members | Full catalog/admin; Critical; bypass OU containment |
| Regional / delegated | `authorization.assignments.delegate`, `authorization.users-organization-units.manage` | Non-privileged holders (OU-scoped High, not Critical) | Assignments only within **grant containment** |

Assignment endpoints accept either system write **or** the matching delegate permission via `[RequireAnyPermission]` (`permission-any:a|b` policy). Handlers always apply `IDelegationAuthorityService` containment for non-privileged actors.

**Containment rules (non-privileged):**

- May assign/revoke only permissions the actor **holds**, excluding privileged catalog codes and `RiskLevel.Critical`.
- May attach/revoke group→OU roots and user↔OU membership only when the target OU is in `AccessibleOrganizationUnitIds` (empty set → fail closed).
- May assign/revoke user↔group only when the group is not privileged, has at least one OU root, and **every** root is inside the actor's accessible set.
- Privileged actors bypass containment (existing `PrivilegedGroupGuard` still applies to privileged targets).
- Revoke of group permissions uses the same “delegatable” rule as assign (symmetry).
- Revoke group→OU is symmetric with assign: group must exist, privileged groups are forbidden (`PrivilegedGroupOrganizationUnitForbidden`), OU must exist, then OU containment applies.

`UserOrganizationUnit` still does **not** expand accessible OU scope (Agent C).

### User ↔ organization unit membership (Agent C)

`UserOrganizationUnit` stores Primary/Additional organizational affiliation. It does **not** grant permissions and does **not** expand `accessibleOrganizationUnitIds` (that still comes only from group→OU scope). Assign/revoke requires `authorization.organization-units.write` **or** `authorization.users-organization-units.manage` (with OU containment for non-privileged actors); list requires `authorization.organization-units.read`.

## Organization-unit scope enforcement (Agent B)

Future business features must enforce OU scope through `IAuthorizationScopeService` — never
trust a client-supplied `organizationUnitId` alone.

| Operation | Call |
|-----------|------|
| List | Resolve accessible ids, then `ApplyOrganizationUnitFilter` on `IOrganizationUnitScoped` queries (or filter by id set). Empty accessible set → empty result. |
| Get / update / delete | `AuthorizePermissionOnResourceAsync(userId, permission, resource.OrganizationUnitId)`. |
| Create | `AuthorizePermissionForCreateAsync(userId, permission, requestedOrganizationUnitId)`. |
| Bulk | `AuthorizePermissionOnResourcesAsync` — **all-or-nothing**; any out-of-scope item denies the batch. |

Rules:

1. Implement `IOrganizationUnitScoped` on entities/projections that carry `OrganizationUnitId`.
2. Catalog `ScopeMode`: `Global` / `None` → permission grant only (no OU filter). `OrganizationUnit` → resource OU must be in the caller's accessible set; empty accessible set or unknown OU → deny.
3. HTTP: use `[RequirePermissionOnUnit("feature.action")]` for OU-scoped resource routes. The handler respects `ScopeMode` (Global/None ignore the route OU).
4. `ListAccessibleOrganizationUnits` is exposed at `GET /api/authorization/me/organization-units` (authenticated; caller-scoped; fail closed → empty). Admin `ListOrganizationUnits` remains `GET /api/authorization/organization-units` for Global `authorization.organization-units.read`.
5. Authz context cache now includes `PermissionScopeByCode`. Shape change is safe: shared version bumps + TTL clear stale Redis entries; missing map is treated as empty (fail closed for OU checks).

### OU-scoped vertical-slice checklist

When adding a business feature that carries an organization unit:

1. **Domain** — implement `IOrganizationUnitScoped` on entities/projections that expose `OrganizationUnitId`.
2. **Application** — use `IAuthorizationScopeService` for list (`ApplyOrganizationUnitFilter` / id set), get/update/delete (`AuthorizePermissionOnResourceAsync`), create (`AuthorizePermissionForCreateAsync`), and bulk (`AuthorizePermissionOnResourcesAsync`, all-or-nothing).
3. **Api** — protect OU-scoped routes with `[RequirePermissionOnUnit("feature.action")]` (plus resource permission); keep controllers thin.
4. **Dual gate** — HTTP attribute is not enough; Application must still enforce scope on the resource OU.
5. **Catalog** — `ScopeMode` / permission catalog metadata does **not** auto-protect endpoints; each feature must opt in with attribute + Application checks.
6. **`UserOrganizationUnit`** — membership metadata only; does not grant permissions or expand accessible OU ids.

## Rules when adding a business endpoint

1. Keep the controller thin — call an Application handler.
2. Require authentication (`[Authorize]` or fallback policy).
3. Add a specific permission with `[RequirePermission("feature.action")]`.
4. If the resource is OU-scoped, use `[RequirePermissionOnUnit("feature.action")]` and accept `organizationUnitId`; also call `IAuthorizationScopeService` inside the use case for query/mutation filters.
5. Accept/return DTOs only; pass `CancellationToken`.
6. Return `ProblemDetails` via existing result mapping helpers.
7. Never log secrets (passwords, tokens, MFA codes).
8. Do not use ASP.NET Identity roles for authorization.

## Redis boundary

Redis is **not** an auth database. Allowed uses only:

- Authorization context cache + shared version key (+ temporary cache-bypass marker if version bump fails)
- Short-lived MFA challenge tickets
- Distributed auth rate-limit counters (login/MFA fail closed when Redis is down; refresh/revoke fail open to the process-local limiter)

PostgreSQL remains authoritative for users, passwords, refresh tokens, permissions, groups, OUs, and audit.

## Reverse proxy

Outside Development, set `ReverseProxy:KnownProxies` and/or `KnownNetworks` to your load-balancer addresses so `X-Forwarded-For` is trusted and auth rate limits use the real client IP. Leave them empty only when the API is not behind a proxy.

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
