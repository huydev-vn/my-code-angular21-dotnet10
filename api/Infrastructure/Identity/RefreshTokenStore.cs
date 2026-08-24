using Application.Features.Identity.Abstractions;
using Domain.Identity;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Identity;

internal sealed class RefreshTokenStore(AppDbContext dbContext) : IRefreshTokenStore
{
    public async Task AddAsync(RefreshToken token, CancellationToken cancellationToken)
    {
        await dbContext.RefreshTokens.AddAsync(token, cancellationToken);
    }

    public Task<RefreshToken?> FindByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken) =>
        dbContext.RefreshTokens.SingleOrDefaultAsync(
            token => token.TokenHash == tokenHash,
            cancellationToken);

    public async Task RevokeFamilyAsync(
        Guid familyId,
        DateTimeOffset revokedAt,
        CancellationToken cancellationToken)
    {
        await dbContext.RefreshTokens
            .Where(token => token.FamilyId == familyId && token.RevokedAt == null)
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(token => token.RevokedAt, revokedAt),
                cancellationToken);
    }

    public async Task RevokeAllForUserAsync(
        Guid userId,
        DateTimeOffset revokedAt,
        CancellationToken cancellationToken)
    {
        await dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(token => token.RevokedAt, revokedAt),
                cancellationToken);
    }

    public async Task<bool> TryRotateAsync(
        RefreshToken current,
        RefreshToken next,
        DateTimeOffset revokedAt,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            cancellationToken);

        var affected = await dbContext.RefreshTokens
            .Where(token => token.Id == current.Id && token.RevokedAt == null)
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(token => token.RevokedAt, revokedAt)
                    .SetProperty(token => token.ReplacedByTokenId, next.Id),
                cancellationToken);

        if (affected != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await dbContext.RefreshTokens.AddAsync(next, cancellationToken);
        await DbUpdateConflict.SaveChangesAsync(dbContext, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<int> PurgeStaleAsync(
        DateTimeOffset olderThan,
        int batchSize,
        CancellationToken cancellationToken)
    {
        if (batchSize < 1)
        {
            return 0;
        }

        var ids = await dbContext.RefreshTokens
            .AsNoTracking()
            .Where(token =>
                (token.RevokedAt != null && token.RevokedAt < olderThan) ||
                (token.RevokedAt == null && token.ExpiresAt < olderThan))
            .OrderBy(token => token.ExpiresAt)
            .Select(token => token.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        if (ids.Count == 0)
        {
            return 0;
        }

        return await dbContext.RefreshTokens
            .Where(token => ids.Contains(token.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }
}
