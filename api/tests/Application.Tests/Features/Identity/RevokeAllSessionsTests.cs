using Application.Common.Time;
using Application.Features.Identity.Abstractions;
using Application.Features.Identity.Revoke;
using Domain.Identity;

namespace Application.Tests.Features.Identity;

public sealed class RevokeAllSessionsTests
{
    [Fact]
    public async Task HandleAsync_RevokesAllActiveTokensForUser()
    {
        var userId = Guid.NewGuid();
        var store = new FakeRefreshTokenStore();
        var clock = new FixedClock(new DateTimeOffset(2026, 8, 21, 12, 0, 0, TimeSpan.Zero));
        var handler = new RevokeAllSessions(store, clock);

        var result = await handler.HandleAsync(userId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(userId, store.RevokedUserId);
        Assert.Equal(clock.UtcNow, store.RevokedAt);
    }

    [Fact]
    public async Task HandleAsync_WhenUserIdEmpty_ReturnsValidationError()
    {
        var handler = new RevokeAllSessions(new FakeRefreshTokenStore(), new FixedClock(DateTimeOffset.UtcNow));

        var result = await handler.HandleAsync(Guid.Empty, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("identity.user_id_required", result.Error!.Code);
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class FakeRefreshTokenStore : IRefreshTokenStore
    {
        public Guid? RevokedUserId { get; private set; }

        public DateTimeOffset? RevokedAt { get; private set; }

        public Task AddAsync(RefreshToken token, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<RefreshToken?> FindByHashAsync(
            string tokenHash,
            CancellationToken cancellationToken) =>
            Task.FromResult<RefreshToken?>(null);

        public Task RevokeFamilyAsync(
            Guid familyId,
            DateTimeOffset revokedAt,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task RevokeAllForUserAsync(
            Guid userId,
            DateTimeOffset revokedAt,
            CancellationToken cancellationToken)
        {
            RevokedUserId = userId;
            RevokedAt = revokedAt;
            return Task.CompletedTask;
        }

        public Task<bool> TryRotateAsync(
            RefreshToken current,
            RefreshToken next,
            DateTimeOffset revokedAt,
            CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task<int> PurgeStaleAsync(
            DateTimeOffset olderThan,
            int batchSize,
            CancellationToken cancellationToken) =>
            Task.FromResult(0);
    }
}
