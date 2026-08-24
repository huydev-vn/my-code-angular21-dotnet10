using Application.Common.Results;
using Application.Common.Time;
using Application.Features.Identity.Abstractions;

namespace Application.Features.Identity.Revoke;

/// <summary>
/// Revokes every active refresh-token family for a user (logout everywhere).
/// Existing access JWTs remain valid until their short lifetime expires.
/// </summary>
public sealed class RevokeAllSessions(
    IRefreshTokenStore refreshTokenStore,
    IClock clock)
{
    public async Task<Result> HandleAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            return Result.Failure(
                Application.Common.Errors.Error.Validation(
                    "identity.user_id_required",
                    "A valid user id is required."));
        }

        await refreshTokenStore.RevokeAllForUserAsync(
            userId,
            clock.UtcNow,
            cancellationToken);

        return Result.Success();
    }
}
