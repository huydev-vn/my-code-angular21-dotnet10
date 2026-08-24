using Domain.Identity;

namespace Application.Features.Identity.Abstractions;

public interface IRefreshTokenStore
{
    Task AddAsync(RefreshToken token, CancellationToken cancellationToken);

    Task<RefreshToken?> FindByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken);

    Task RevokeFamilyAsync(
        Guid familyId,
        DateTimeOffset revokedAt,
        CancellationToken cancellationToken);

    /// <summary>Revokes every active refresh-token family for the user.</summary>
    Task RevokeAllForUserAsync(
        Guid userId,
        DateTimeOffset revokedAt,
        CancellationToken cancellationToken);

    Task<bool> TryRotateAsync(
        RefreshToken current,
        RefreshToken next,
        DateTimeOffset revokedAt,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes revoked or expired tokens older than <paramref name="olderThan"/>,
    /// up to <paramref name="batchSize"/> rows.
    /// </summary>
    Task<int> PurgeStaleAsync(
        DateTimeOffset olderThan,
        int batchSize,
        CancellationToken cancellationToken);
}
