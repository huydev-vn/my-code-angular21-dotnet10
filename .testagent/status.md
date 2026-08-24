# Security test matrix — status

## Validation

```text
dotnet test api/api.slnx
Passed! Domain.Tests 10, Application.Tests 14, Api.Tests 22 (total 46)
```

## Assertion / gap review

- JWT cases assert HTTP 401 for signature/issuer/audience/expiry — no tautologies.
- Cookie tests assert concrete Set-Cookie attributes (HttpOnly, Path, SameSite, Secure).
- CSRF matrix covers MFA, trusted Origin, body-token skip, cookie-without-Origin.
- Refresh race asserts family revoke + reuse metric, not only failure.
- Redis multi-replica tests require live Redis and assert cross-reader version / shared limit.
- Production startup asserts exception message contains the failing config key.

## Fixes applied during review

- Redis coordination tests fail with an actionable message when Redis is down (no silent pass).
- `Infrastructure` gained `InternalsVisibleTo("Api.Tests")` for version types.
