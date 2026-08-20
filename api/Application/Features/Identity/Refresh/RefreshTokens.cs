using Application.Common.Results;
using Application.Common.Time;
using Application.Common.Validation;
using Application.Features.Identity.Abstractions;
using Application.Features.Identity.Contracts;
using Application.Features.Identity.Errors;
using FluentValidation;

namespace Application.Features.Identity.Refresh;

public sealed class RefreshTokens(
    IValidator<RefreshTokensRequest> validator,
    ITokenService tokenService,
    IRefreshTokenStore refreshTokenStore,
    IUserAccountService userAccountService,
    AuthTokenIssuer tokenIssuer,
    IClock clock)
{
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
            return Result<AuthResponse>.Failure(IdentityErrors.InvalidRefreshToken);
        }

        if (stored.IsRevoked)
        {
            await refreshTokenStore.RevokeFamilyAsync(stored.FamilyId, now, cancellationToken);
            return Result<AuthResponse>.Failure(IdentityErrors.InvalidRefreshToken);
        }

        if (stored.IsExpired(now))
        {
            await refreshTokenStore.RevokeFamilyAsync(stored.FamilyId, now, cancellationToken);
            return Result<AuthResponse>.Failure(IdentityErrors.InvalidRefreshToken);
        }

        var user = await userAccountService.FindByIdAsync(stored.UserId, cancellationToken);
        if (user is null)
        {
            await refreshTokenStore.RevokeFamilyAsync(stored.FamilyId, now, cancellationToken);
            return Result<AuthResponse>.Failure(IdentityErrors.InvalidRefreshToken);
        }

        var tokens = await tokenIssuer.IssueAsync(
            user,
            stored.FamilyId,
            stored,
            cancellationToken);
        if (tokens is null)
        {
            return Result<AuthResponse>.Failure(IdentityErrors.InvalidRefreshToken);
        }

        return Result<AuthResponse>.Success(tokens);
    }
}
