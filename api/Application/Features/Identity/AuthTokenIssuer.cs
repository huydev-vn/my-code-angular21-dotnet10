using Application.Common.Persistence;
using Application.Common.Time;
using Application.Features.Identity.Abstractions;
using Application.Features.Identity.Contracts;
using Domain.Identity;

namespace Application.Features.Identity;

public sealed class AuthTokenIssuer(
    ITokenService tokenService,
    IRefreshTokenStore refreshTokenStore,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<AuthResponse?> IssueAsync(
        UserAccount user,
        Guid? familyId,
        RefreshToken? current,
        CancellationToken cancellationToken)
    {
        var accessToken = tokenService.CreateAccessToken(user);
        var issuedRefreshToken = tokenService.CreateRefreshToken();
        var now = clock.UtcNow;
        var nextFamilyId = familyId ?? Guid.NewGuid();

        var next = RefreshToken.Issue(
            user.Id,
            issuedRefreshToken.Hash,
            nextFamilyId,
            now,
            issuedRefreshToken.ExpiresAt);

        if (current is not null)
        {
            var rotated = await refreshTokenStore.TryRotateAsync(
                current,
                next,
                now,
                cancellationToken);
            if (!rotated)
            {
                return null;
            }
        }
        else
        {
            await refreshTokenStore.AddAsync(next, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new AuthResponse(
            accessToken.Value,
            accessToken.ExpiresAt,
            issuedRefreshToken.PlainText,
            issuedRefreshToken.ExpiresAt);
    }
}
