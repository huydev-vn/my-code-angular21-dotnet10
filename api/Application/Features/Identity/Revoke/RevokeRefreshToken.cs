using Application.Common.Results;
using Application.Common.Time;
using Application.Common.Validation;
using Application.Features.Identity.Abstractions;
using FluentValidation;

namespace Application.Features.Identity.Revoke;

public sealed class RevokeRefreshToken(
    IValidator<RevokeRefreshTokenRequest> validator,
    ITokenService tokenService,
    IRefreshTokenStore refreshTokenStore,
    IClock clock)
{
    public async Task<Result> HandleAsync(
        RevokeRefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var validationFailure = (await validator.ValidateAsync(request, cancellationToken))
            .ToFailure();
        if (validationFailure is not null)
        {
            return validationFailure;
        }

        var tokenHash = tokenService.HashRefreshToken(request.RefreshToken);
        var stored = await refreshTokenStore.FindByHashAsync(tokenHash, cancellationToken);
        if (stored is null)
        {
            return Result.Success();
        }

        await refreshTokenStore.RevokeFamilyAsync(
            stored.FamilyId,
            clock.UtcNow,
            cancellationToken);

        return Result.Success();
    }
}
