using Application.Common.Pagination;
using Application.Common.Persistence;
using Application.Common.Results;
using Application.Common.Time;
using Application.Features.Identity;
using Application.Features.Identity.Abstractions;
using Application.Features.Identity.Contracts;
using Application.Features.Identity.Errors;
using Application.Features.Identity.Refresh;
using Domain.Identity;
using FluentValidation;

namespace Application.Tests.Features.Identity;

public sealed class RefreshTokensTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_WhenUserLockedOut_RevokesFamilyAndReturnsInvalidRefreshToken()
    {
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var stored = RefreshToken.Issue(
            userId,
            tokenHash: "abc",
            familyId,
            Now.AddDays(-1),
            Now.AddDays(7));

        var store = new FakeRefreshTokenStore(stored);
        var users = new FakeUserAccountService(
            new UserAccount(userId, "user@example.com", Now.AddYears(-1), IsLockedOut: true, TwoFactorEnabled: false));
        var handler = CreateHandler(store, users);

        var result = await handler.HandleAsync(
            new RefreshTokensRequest { RefreshToken = "plain-token" },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.InvalidRefreshToken, result.Error);
        Assert.Equal(familyId, store.RevokedFamilyId);
        Assert.Equal(Now, store.RevokedAt);
        Assert.False(store.TryRotateCalled);
    }

    [Fact]
    public async Task HandleAsync_WhenTokenAlreadyRevoked_RevokesFamily()
    {
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var stored = RefreshToken.Issue(
            userId,
            tokenHash: "abc",
            familyId,
            Now.AddDays(-1),
            Now.AddDays(7));
        stored.Revoke(Now.AddMinutes(-5));

        var store = new FakeRefreshTokenStore(stored);
        var users = new FakeUserAccountService(
            new UserAccount(userId, "user@example.com", Now.AddYears(-1), IsLockedOut: false, TwoFactorEnabled: false));
        var handler = CreateHandler(store, users);

        var result = await handler.HandleAsync(
            new RefreshTokensRequest { RefreshToken = "plain-token" },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.InvalidRefreshToken, result.Error);
        Assert.Equal(familyId, store.RevokedFamilyId);
    }

    [Fact]
    public async Task HandleAsync_WhenRotationLosesRaceWithoutReplacement_RevokesFamilyAndSignalsReuse()
    {
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var stored = RefreshToken.Issue(
            userId,
            tokenHash: "abc",
            familyId,
            Now.AddDays(-1),
            Now.AddDays(7));

        var store = new FakeRefreshTokenStore(stored, rotateSucceeds: false);
        var users = new FakeUserAccountService(
            new UserAccount(userId, "user@example.com", Now.AddYears(-1), IsLockedOut: false, TwoFactorEnabled: false));
        var metrics = new CountingAuthMetrics();
        var handler = CreateHandler(store, users, metrics);

        var result = await handler.HandleAsync(
            new RefreshTokensRequest { RefreshToken = "plain-token" },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.InvalidRefreshToken, result.Error);
        Assert.Equal(familyId, store.RevokedFamilyId);
        Assert.True(store.TryRotateCalled);
        Assert.Equal(1, metrics.RefreshReuseDetectedCount);
        Assert.Equal(1, metrics.RefreshFailedCount);
    }

    [Fact]
    public async Task HandleAsync_WhenConcurrentRotationWithinGrace_DoesNotRevokeFamily()
    {
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var stored = RefreshToken.Issue(
            userId,
            tokenHash: "abc",
            familyId,
            Now.AddDays(-1),
            Now.AddDays(7));
        stored.Revoke(Now.AddSeconds(-1), replacedByTokenId: Guid.NewGuid());

        var store = new FakeRefreshTokenStore(stored, rotateSucceeds: false);
        var users = new FakeUserAccountService(
            new UserAccount(userId, "user@example.com", Now.AddYears(-1), IsLockedOut: false, TwoFactorEnabled: false));
        var metrics = new CountingAuthMetrics();
        var handler = CreateHandler(store, users, metrics);

        var result = await handler.HandleAsync(
            new RefreshTokensRequest { RefreshToken = "plain-token" },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.InvalidRefreshToken, result.Error);
        Assert.Null(store.RevokedFamilyId);
        Assert.Equal(0, metrics.RefreshReuseDetectedCount);
        Assert.Equal(1, metrics.RefreshFailedCount);
    }

    [Fact]
    public async Task HandleAsync_WhenRotationLosesRaceToConcurrentWinner_DoesNotRevokeFamily()
    {
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var stored = RefreshToken.Issue(
            userId,
            tokenHash: "abc",
            familyId,
            Now.AddDays(-1),
            Now.AddDays(7));

        var store = new FakeRefreshTokenStore(
            stored,
            rotateSucceeds: false,
            markReplacedOnFailedRotate: true);
        var users = new FakeUserAccountService(
            new UserAccount(userId, "user@example.com", Now.AddYears(-1), IsLockedOut: false, TwoFactorEnabled: false));
        var metrics = new CountingAuthMetrics();
        var handler = CreateHandler(store, users, metrics);

        var result = await handler.HandleAsync(
            new RefreshTokensRequest { RefreshToken = "plain-token" },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Null(store.RevokedFamilyId);
        Assert.Equal(0, metrics.RefreshReuseDetectedCount);
        Assert.Equal(1, metrics.RefreshFailedCount);
    }

    private static RefreshTokens CreateHandler(
        FakeRefreshTokenStore store,
        FakeUserAccountService users,
        IAuthMetrics? metrics = null)
    {
        var tokenService = new FakeTokenService();
        var clock = new FixedClock(Now);
        var issuer = new AuthTokenIssuer(
            tokenService,
            store,
            new FakeUnitOfWork(),
            clock);

        return new RefreshTokens(
            new PassThroughRefreshValidator(),
            tokenService,
            store,
            users,
            issuer,
            clock,
            metrics ?? new NoOpAuthMetrics());
    }

    private sealed class CountingAuthMetrics : IAuthMetrics
    {
        public int RefreshFailedCount { get; private set; }

        public int RefreshReuseDetectedCount { get; private set; }

        public void LoginSucceeded()
        {
        }

        public void LoginFailed()
        {
        }

        public void RefreshSucceeded()
        {
        }

        public void RefreshFailed() => RefreshFailedCount++;

        public void RefreshReuseDetected() => RefreshReuseDetectedCount++;

        public void RateLimited()
        {
        }

        public void MfaChallengeIssued()
        {
        }

        public void MfaSucceeded()
        {
        }

        public void MfaFailed()
        {
        }
    }

    private sealed class NoOpAuthMetrics : IAuthMetrics
    {
        public void LoginSucceeded()
        {
        }

        public void LoginFailed()
        {
        }

        public void RefreshSucceeded()
        {
        }

        public void RefreshFailed()
        {
        }

        public void RefreshReuseDetected()
        {
        }

        public void RateLimited()
        {
        }

        public void MfaChallengeIssued()
        {
        }

        public void MfaSucceeded()
        {
        }

        public void MfaFailed()
        {
        }
    }

    private sealed class PassThroughRefreshValidator : AbstractValidator<RefreshTokensRequest>;

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<IUnitOfWorkTransaction> BeginTransactionAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult<IUnitOfWorkTransaction>(new FakeTransaction());

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
            Task.FromResult(0);

        private sealed class FakeTransaction : IUnitOfWorkTransaction
        {
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;

            public Task CommitAsync(CancellationToken cancellationToken) =>
                Task.CompletedTask;
        }
    }

    private sealed class FakeTokenService : ITokenService
    {
        public AccessToken CreateAccessToken(UserAccount user) =>
            new("access", Now.AddMinutes(15));

        public IssuedRefreshToken CreateRefreshToken() =>
            new("next-plain", "next-hash", Now.AddDays(14));

        public string HashRefreshToken(string refreshToken) => "abc";
    }

    private sealed class FakeUserAccountService(UserAccount? user) : IUserAccountService
    {
        public Task<Result<UserAccount>> RegisterAsync(
            string email,
            string password,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Result<UserAccount>> AuthenticateAsync(
            string email,
            string password,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<UserAccount?> FindByIdAsync(
            Guid userId,
            CancellationToken cancellationToken) =>
            Task.FromResult(user);

        public Task<PageResult<UserAccount>> ListAsync(
            PageRequest page,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Result<AuthenticatorSetup>> BeginAuthenticatorSetupAsync(
            Guid userId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Result> ConfirmAuthenticatorSetupAsync(
            Guid userId,
            string code,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Result> DisableAuthenticatorAsync(
            Guid userId,
            string code,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Result> VerifyAuthenticatorCodeAsync(
            Guid userId,
            string code,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeRefreshTokenStore(
        RefreshToken? stored,
        bool rotateSucceeds = true,
        bool markReplacedOnFailedRotate = false) : IRefreshTokenStore
    {
        public Guid? RevokedFamilyId { get; private set; }

        public DateTimeOffset? RevokedAt { get; private set; }

        public bool TryRotateCalled { get; private set; }

        public Task AddAsync(RefreshToken token, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<RefreshToken?> FindByHashAsync(
            string tokenHash,
            CancellationToken cancellationToken) =>
            Task.FromResult(stored);

        public Task RevokeFamilyAsync(
            Guid familyId,
            DateTimeOffset revokedAt,
            CancellationToken cancellationToken)
        {
            RevokedFamilyId = familyId;
            RevokedAt = revokedAt;
            return Task.CompletedTask;
        }

        public Task RevokeAllForUserAsync(
            Guid userId,
            DateTimeOffset revokedAt,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<bool> TryRotateAsync(
            RefreshToken current,
            RefreshToken next,
            DateTimeOffset revokedAt,
            CancellationToken cancellationToken)
        {
            TryRotateCalled = true;
            if (!rotateSucceeds && markReplacedOnFailedRotate && stored is not null && !stored.IsRevoked)
            {
                stored.Revoke(revokedAt, replacedByTokenId: next.Id);
            }

            return Task.FromResult(rotateSucceeds);
        }

        public Task<int> PurgeStaleAsync(
            DateTimeOffset olderThan,
            int batchSize,
            CancellationToken cancellationToken) =>
            Task.FromResult(0);
    }
}
