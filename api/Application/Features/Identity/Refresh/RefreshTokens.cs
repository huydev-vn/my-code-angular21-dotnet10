using Application.Common.Results;
using Application.Common.Time;
using Application.Common.Validation;
using Application.Features.Identity.Abstractions;
using Application.Features.Identity.Contracts;
using Application.Features.Identity.Errors;
using Domain.Identity;
using FluentValidation;

namespace Application.Features.Identity.Refresh;

public sealed class RefreshTokens(
    IValidator<RefreshTokensRequest> validator,
    ITokenService tokenService,
    IRefreshTokenStore refreshTokenStore,
    IUserAccountService userAccountService,
    AuthTokenIssuer tokenIssuer,
    IClock clock,
    IAuthMetrics authMetrics)
{
    /// <summary>
    /// Concurrent browser retries that lose the rotation race present as a revoked
    /// token with ReplacedByTokenId. Within this window we fail the loser without
    /// treating it as theft (no family revoke).
    /// </summary>
    internal static readonly TimeSpan ConcurrentRotationGrace = TimeSpan.FromSeconds(10);

    public async Task<Result<AuthResponse>> HandleAsync(
        RefreshTokensRequest request,
        CancellationToken cancellationToken)
    {
        var validationFailure = (await validator.ValidateAsync(request, cancellationToken))
            .ToFailure<AuthResponse>();
        if (validationFailure is not null)
        {
            return validationFailure;
        }

        var now = clock.UtcNow;
        var tokenHash = tokenService.HashRefreshToken(request.RefreshToken);
        var stored = await refreshTokenStore.FindByHashAsync(tokenHash, cancellationToken);
        if (stored is null)
        {
            authMetrics.RefreshFailed();
            return Result<AuthResponse>.Failure(IdentityErrors.InvalidRefreshToken);
        }

        if (stored.IsRevoked)
        {
            if (IsConcurrentRotationRetry(stored, now))
            {
                authMetrics.RefreshFailed();
                return Result<AuthResponse>.Failure(IdentityErrors.InvalidRefreshToken);
            }

            await refreshTokenStore.RevokeFamilyAsync(stored.FamilyId, now, cancellationToken);
            authMetrics.RefreshReuseDetected();
            authMetrics.RefreshFailed();
            return Result<AuthResponse>.Failure(IdentityErrors.InvalidRefreshToken);
        }

        if (stored.IsExpired(now))
        {
            await refreshTokenStore.RevokeFamilyAsync(stored.FamilyId, now, cancellationToken);
            authMetrics.RefreshFailed();
            return Result<AuthResponse>.Failure(IdentityErrors.InvalidRefreshToken);
        }

        var user = await userAccountService.FindByIdAsync(stored.UserId, cancellationToken);
        if (user is null)
        {
            await refreshTokenStore.RevokeFamilyAsync(stored.FamilyId, now, cancellationToken);
            authMetrics.RefreshFailed();
            return Result<AuthResponse>.Failure(IdentityErrors.InvalidRefreshToken);
        }

        // Lockout must stop refresh chains, not only password login.
        if (user.IsLockedOut)
        {
            await refreshTokenStore.RevokeFamilyAsync(stored.FamilyId, now, cancellationToken);
            authMetrics.RefreshFailed();
            return Result<AuthResponse>.Failure(IdentityErrors.InvalidRefreshToken);
        }

        var tokens = await tokenIssuer.IssueAsync(
            user,
            stored.FamilyId,
            stored,
            cancellationToken);
        if (tokens is null)
        {
            // Rotation lost a race. Re-read: concurrent legitimate refresh leaves a
            // replacement link; true reuse / logout conflict does not within grace.
            var latest = await refreshTokenStore.FindByHashAsync(tokenHash, cancellationToken);
            if (latest is not null && IsConcurrentRotationRetry(latest, now))
            {
                authMetrics.RefreshFailed();
                return Result<AuthResponse>.Failure(IdentityErrors.InvalidRefreshToken);
            }

            await refreshTokenStore.RevokeFamilyAsync(stored.FamilyId, now, cancellationToken);
            authMetrics.RefreshReuseDetected();
            authMetrics.RefreshFailed();
            return Result<AuthResponse>.Failure(IdentityErrors.InvalidRefreshToken);
        }

        authMetrics.RefreshSucceeded();
        return Result<AuthResponse>.Success(tokens);
    }

    internal static bool IsConcurrentRotationRetry(RefreshToken token, DateTimeOffset utcNow) =>
        token.ReplacedByTokenId is not null &&
        token.RevokedAt is DateTimeOffset revokedAt &&
        utcNow - revokedAt <= ConcurrentRotationGrace;
}
