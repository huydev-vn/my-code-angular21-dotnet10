# Security test matrix — plan

## Phase 1 — Api pipeline / JWT / CSRF / cookies / production

| Checklist item | Planned test |
|----------------|--------------|
| JWT invalid signature | `JwtBearerTests.ProtectedEndpoint_WithTamperedSignature_ReturnsUnauthorized` |
| JWT wrong issuer | `JwtBearerTests.ProtectedEndpoint_WithWrongIssuer_ReturnsUnauthorized` |
| JWT wrong audience | `JwtBearerTests.ProtectedEndpoint_WithWrongAudience_ReturnsUnauthorized` |
| JWT expired | `JwtBearerTests.ProtectedEndpoint_WithExpiredToken_ReturnsUnauthorized` |
| Cookie HttpOnly/Path/SameSite/Secure | `RefreshTokenCookieTests.Set_InDevelopment_WritesHttpOnlyLaxPathCookieWithoutSecure` |
| CSRF MFA untrusted | `SecurityPipelineTests.MfaVerify_WithUntrustedOrigin_ReturnsCsrfForbidden` |
| CSRF trusted Origin | `SecurityPipelineTests.Login_WithTrustedOrigin_PassesCsrfGate` |
| CSRF body refresh no cookie | `SecurityPipelineTests.Refresh_WithoutCookie_SkipsCsrfEvenWithUntrustedOrigin` |
| CSRF cookie no Origin | `SecurityPipelineTests.Refresh_WithCookieAndNoOrigin_ReturnsCsrfForbidden` |
| Production missing Redis | `ProductionStartupTests.Host_WithoutRedis_Throws` |
| Production HTTP origins | `ProductionStartupTests.Host_WithHttpClientOrigins_Throws` |
| Production dev signing key | `ProductionStartupTests.Host_WithDevelopmentSigningKey_Throws` |

## Phase 2 — Application refresh concurrency

| Checklist item | Planned test |
|----------------|--------------|
| Refresh race family revoke | `RefreshTokensTests.HandleAsync_WhenRotationLosesRace_RevokesFamilyAndSignalsReuse` |

## Phase 3 — Multi-replica Redis / memory version

| Checklist item | Planned test |
|----------------|--------------|
| Memory version bump visible | `AuthorizationStateVersionTests.Memory_Bump_IsVisibleToReaders` |
| Redis version shared | `AuthorizationStateVersionTests.Redis_Bump_IsVisibleAcrossConnections` (skip if Redis unavailable) |
| Redis rate limit shared | `RedisAuthRateLimitStoreTests.TryAcquire_ExceedsPermitLimit_ReturnsFalse` (skip if Redis unavailable) |

## Plumbing

- Add `InternalsVisibleTo Include="Api.Tests"` on `Infrastructure.csproj`
- Keep tests free of Postgres (except optional Redis on :6379)
