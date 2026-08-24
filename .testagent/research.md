# Security test matrix — research

## Scope

Security matrix from auth hardening roadmap: JWT, cookies, CSRF, refresh concurrency,
multi-replica Redis coordination, production startup validation.

## Existing conventions

- Framework: xUnit + `Microsoft.AspNetCore.Mvc.Testing`
- Projects: `Api.Tests`, `Application.Tests`, `Domain.Tests` (no `Infrastructure.Tests`)
- `ApiFactory` hosts Development without seeders/Postgres/Redis
- `Api` exposes `InternalsVisibleTo("Api.Tests")`
- Existing CSRF coverage: login + refresh cookie + untrusted Origin only

## Target inventory (security-relevant)

| Area | Source | Current pairing |
|------|--------|-----------------|
| JWT bearer validation | `api/Extensions/AuthenticationExtensions.cs` | untested |
| Cookie flags | `api/Identity/RefreshTokenCookie.cs` | untested |
| CSRF middleware | `api/Middleware/CookieCsrfMiddleware.cs` | partial (`SecurityPipelineTests`) |
| Refresh race revoke | `Application/.../Refresh/RefreshTokens.cs` | partial (revoked/locked; no TryRotate fail) |
| Authz version | `MemoryAuthorizationStateVersion` / `RedisAuthorizationStateVersion` | untested (internal) |
| Distributed rate limit | `RedisAuthRateLimitStore` (public) | untested |
| Production guards | `SecurityPipelineExtensions` + `AddRedisOrMemoryCaching` + `JwtOptionsValidator` | untested |

## Acceptance checklist (verbatim requirements)

1. JWT — invalid signature / wrong issuer / wrong audience / expired access token rejected (401)
2. Cookies — refresh cookie is HttpOnly, Path `/api/identity`, SameSite=Lax; Secure off in Development
3. CSRF — MFA verify rejects untrusted Origin; trusted Origin passes CSRF; body refresh without cookie skips Origin check; cookie refresh without Origin/Referer rejected
4. Concurrency — refresh rotation race (`TryRotateAsync` false) revokes family and records reuse
5. Multi-replica — shared authz version bump is visible across readers; Redis rate-limit counter enforces shared permit limit (skip if Redis down)
6. Production startup — missing Redis fails; non-HTTPS Client:Origins fails; development JWT signing key fails outside Development

## Notes

- Full login/cookie round-trips need PostgreSQL; cookie flag tests use `DefaultHttpContext` via InternalsVisibleTo.
- Redis tests use `localhost:6379` and skip when ping fails so local/CI without Redis stay green.
- Infrastructure internals need `InternalsVisibleTo("Api.Tests")` for Memory/Redis version types.
