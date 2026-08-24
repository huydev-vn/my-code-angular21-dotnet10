using Application.Common.Pagination;
using Application.Common.Persistence;
using Application.Common.Results;
using Application.Common.Time;
using Application.Features.Authorization.Abstractions;
using Application.Features.Identity;
using Application.Features.Identity.Abstractions;
using Application.Features.Identity.Errors;
using Application.Features.Identity.Mfa;
using Domain.Authorization;
using Domain.Identity;
using FluentValidation;

namespace Application.Tests.Features.Identity;

public sealed class VerifyMfaLoginTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_WhenTicketMissing_ReturnsInvalidMfaTicket()
    {
        var store = new FakeMfaStore(userId: null);
        var metrics = new CountingAuthMetrics();
        var handler = CreateHandler(store, new FakeUsers(twoFactorEnabled: true), metrics);

        var result = await handler.HandleAsync(
            new VerifyMfaLoginRequest { MfaToken = "missing", Code = "123456" },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.InvalidMfaTicket, result.Error);
        Assert.Equal(1, metrics.MfaFailedCount);
        Assert.Equal(0, metrics.MfaSucceededCount);
    }

    [Fact]
    public async Task HandleAsync_WhenCodeInvalid_ConsumesTicketAndFails()
    {
        var userId = Guid.NewGuid();
        var store = new FakeMfaStore(userId);
        var users = new FakeUsers(twoFactorEnabled: true, verifySucceeds: false);
        var metrics = new CountingAuthMetrics();
        var handler = CreateHandler(store, users, metrics);

        var result = await handler.HandleAsync(
            new VerifyMfaLoginRequest { MfaToken = "ticket", Code = "000000" },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.True(store.Consumed);
        Assert.Equal(1, metrics.MfaFailedCount);
    }

    [Fact]
    public async Task HandleAsync_WhenCodeValid_IssuesTokens()
    {
        var userId = Guid.NewGuid();
        var store = new FakeMfaStore(userId);
        var users = new FakeUsers(twoFactorEnabled: true, verifySucceeds: true, userId: userId);
        var metrics = new CountingAuthMetrics();
        var handler = CreateHandler(store, users, metrics);

        var result = await handler.HandleAsync(
            new VerifyMfaLoginRequest { MfaToken = "ticket", Code = "123456" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.Value!.AccessToken));
        Assert.Equal(1, metrics.MfaSucceededCount);
    }

    [Fact]
    public async Task DisableAuthenticator_WhenPrivilegedAndRequired_ReturnsConflict()
    {
        var userId = Guid.NewGuid();
        var handler = new DisableAuthenticator(
            new PassThroughDisableValidator(),
            new FakeUsers(twoFactorEnabled: true, userId: userId),
            new PrivilegedOnlyAdminStore(isPrivileged: true),
            new FixedIdentitySettings(requireMfaForPrivileged: true));

        var result = await handler.HandleAsync(
            userId,
            new DisableAuthenticatorRequest { Code = "123456" },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.PrivilegedMfaRequired, result.Error);
    }

    private static VerifyMfaLogin CreateHandler(
        FakeMfaStore store,
        FakeUsers users,
        CountingAuthMetrics metrics)
    {
        var tokenService = new FakeTokenService();
        var refreshStore = new FakeRefreshTokenStore();
        var issuer = new AuthTokenIssuer(
            tokenService,
            refreshStore,
            new FakeUnitOfWork(),
            new FixedClock(Now));

        return new VerifyMfaLogin(
            new PassThroughVerifyValidator(),
            store,
            users,
            issuer,
            metrics);
    }

    private sealed class PassThroughVerifyValidator : AbstractValidator<VerifyMfaLoginRequest>;

    private sealed class PassThroughDisableValidator : AbstractValidator<DisableAuthenticatorRequest>;

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class FixedIdentitySettings(bool requireMfaForPrivileged)
        : Application.Common.Settings.IIdentitySettings
    {
        public bool AllowRegistration => false;

        public bool RunSeeders => false;

        public bool ConfirmEmailOnProvision => true;

        public bool RequireMfaForPrivileged => requireMfaForPrivileged;

        public int MfaChallengeMinutes => 5;

        public string AuthenticatorIssuer => "test";
    }

    private sealed class CountingAuthMetrics : IAuthMetrics
    {
        public int MfaSucceededCount { get; private set; }

        public int MfaFailedCount { get; private set; }

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

        public void MfaSucceeded() => MfaSucceededCount++;

        public void MfaFailed() => MfaFailedCount++;
    }

    private sealed class FakeMfaStore(Guid? userId) : IMfaChallengeStore
    {
        public bool Consumed { get; private set; }

        public Task StoreAsync(
            string ticket,
            Guid userId,
            DateTimeOffset expiresAt,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<Guid?> ConsumeAsync(string ticket, CancellationToken cancellationToken)
        {
            Consumed = true;
            return Task.FromResult(userId);
        }
    }

    private sealed class FakeTokenService : ITokenService
    {
        public AccessToken CreateAccessToken(UserAccount user) =>
            new("access", Now.AddMinutes(15));

        public IssuedRefreshToken CreateRefreshToken() =>
            new("next-plain", "next-hash", Now.AddDays(14));

        public string HashRefreshToken(string refreshToken) => "hash";
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

    private sealed class FakeRefreshTokenStore : IRefreshTokenStore
    {
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
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

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

    private sealed class PrivilegedOnlyAdminStore(bool isPrivileged) : IAuthorizationAdminStore
    {
        public Task<bool> IsMemberOfAnyPrivilegedGroupAsync(
            Guid userId,
            CancellationToken cancellationToken) =>
            Task.FromResult(isPrivileged);

        public Task<PermissionDefinition?> FindPermissionByIdAsync(
            Guid id,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PermissionDefinition?> FindPermissionByCodeAsync(
            string code,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PageResult<PermissionDefinition>> ListPermissionsAsync(
            PageRequest page,
            bool? isActive,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task AddPermissionAsync(
            PermissionDefinition permission,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<UserGroup?> FindGroupByIdAsync(
            Guid id,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<UserGroup?> FindGroupByNameAsync(
            string name,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PageResult<UserGroup>> ListGroupsAsync(
            PageRequest page,
            bool? isActive,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task AddGroupAsync(UserGroup group, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<OrganizationUnit?> FindOrganizationUnitByIdAsync(
            Guid id,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<OrganizationUnit?> FindOrganizationUnitByCodeAsync(
            string code,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PageResult<OrganizationUnit>> ListOrganizationUnitsAsync(
            PageRequest page,
            bool? isActive,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task AddOrganizationUnitAsync(
            OrganizationUnit unit,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> GroupPermissionExistsAsync(
            Guid groupId,
            Guid permissionId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task AddGroupPermissionAsync(
            GroupPermission assignment,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> RemoveGroupPermissionAsync(
            Guid groupId,
            Guid permissionId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> UserGroupMembershipExistsAsync(
            Guid userId,
            Guid groupId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<int> CountActiveMembersInGroupAsync(
            Guid groupId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task AddUserGroupMembershipAsync(
            UserGroupMembership membership,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> RemoveUserGroupMembershipAsync(
            Guid userId,
            Guid groupId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> GroupOrganizationUnitExistsAsync(
            Guid groupId,
            Guid organizationUnitId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task AddGroupOrganizationUnitAsync(
            GroupOrganizationUnit assignment,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> RemoveGroupOrganizationUnitAsync(
            Guid groupId,
            Guid organizationUnitId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Guid>> GetDescendantOrganizationUnitIdsAsync(
            Guid rootOrganizationUnitId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> WouldCreateOrganizationUnitCycleAsync(
            Guid organizationUnitId,
            Guid? newParentId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PageResult<AuthorizationAuditEvent>> ListAuditEventsAsync(
            PageRequest page,
            string? action,
            Guid? actorUserId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeUsers(
        bool twoFactorEnabled,
        bool verifySucceeds = true,
        Guid? userId = null) : IUserAccountService
    {
        private readonly Guid _userId = userId ?? Guid.NewGuid();

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
            Task.FromResult<UserAccount?>(
                new UserAccount(
                    _userId,
                    "user@example.com",
                    Now.AddYears(-1),
                    IsLockedOut: false,
                    TwoFactorEnabled: twoFactorEnabled));

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
            Task.FromResult(Result.Success());

        public Task<Result> VerifyAuthenticatorCodeAsync(
            Guid userId,
            string code,
            CancellationToken cancellationToken) =>
            Task.FromResult(
                verifySucceeds
                    ? Result.Success()
                    : Result.Failure(IdentityErrors.InvalidMfaCode));
    }
}
