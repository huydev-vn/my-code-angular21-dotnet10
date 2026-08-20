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

    Task<bool> TryRotateAsync(
        RefreshToken current,
        RefreshToken next,
        DateTimeOffset revokedAt,
        CancellationToken cancellationToken);
}
