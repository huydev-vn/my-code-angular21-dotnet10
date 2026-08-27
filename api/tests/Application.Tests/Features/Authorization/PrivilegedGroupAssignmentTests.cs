using Application.Common.Pagination;
using Application.Common.Persistence;
using Application.Common.Results;
using Application.Common.Security;
using Application.Common.Time;
using Application.Features.Authorization.Abstractions;
using Application.Features.Authorization.Assignments;
using Application.Features.Authorization.Errors;
using Application.Features.Identity.Abstractions;
using Domain.Authorization;
using FluentValidation;

namespace Application.Tests.Features.Authorization;

public sealed class PrivilegedGroupAssignmentTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AssignUserToGroup_WhenTargetPrivilegedAndActorNotPrivileged_ReturnsForbidden()
    {
        var privilegedGroup = UserGroup.CreatePrivileged(
            "System Administrators",
            "admins",
            Now);
        var store = new FakeAdminStore(privilegedGroup, actorIsPrivileged: false);
        var handler = new AssignUserToGroup(
            store,
            new FakeUsers(),
            new FakeAuditor(),
            new FakeUnitOfWork(),
            new FixedClock(Now),
            new FakeActor(Guid.NewGuid()),
            new AllowAllDelegation(),
            new PassThroughAssignUserValidator());

        var result = await handler.HandleAsync(
            new AssignUserToGroupRequest(privilegedGroup.Id, Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthorizationErrors.PrivilegedGroupMutationForbidden, result.Error);
        Assert.False(store.MembershipAdded);
    }

    [Fact]
    public async Task AssignGroupPermission_WhenHighRiskPermissionOnNonPrivilegedGroup_ReturnsForbidden()
    {
        var group = UserGroup.Create("Editors", null, Now);
        var permission = PermissionDefinition.Create(
            SystemPermissions.AuthorizationGroupsWrite,
            "Manage groups",
            "authorization",
            "groups.write",
            PermissionScopeMode.Global,
            Now,
            "authorization.groups",
            PermissionRiskLevel.Critical,
            isSystemManaged: true);
        var store = new FakeAdminStore(group, actorIsPrivileged: true, permission);
        var handler = new AssignGroupPermission(
            store,
            new FakeAuditor(),
            new FakeUnitOfWork(),
            new FixedClock(Now),
            new FakeActor(Guid.NewGuid()),
            new AllowAllDelegation(),
            new PassThroughAssignPermissionValidator());

        var result = await handler.HandleAsync(
            new AssignGroupPermissionRequest(group.Id, permission.Id),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            AuthorizationErrors.PrivilegedPermissionRequiresPrivilegedGroup,
            result.Error);
    }

    [Fact]
    public async Task AssignGroupOrganizationUnit_WhenGroupPrivileged_ReturnsForbidden()
    {
        var group = UserGroup.CreatePrivileged("System Administrators", null, Now);
        var store = new FakeAdminStore(group, actorIsPrivileged: true);
        var handler = new AssignGroupOrganizationUnit(
            store,
            new FakeAuditor(),
            new FakeUnitOfWork(),
            new FixedClock(Now),
            new FakeActor(Guid.NewGuid()),
            new AllowAllDelegation(),
            new PassThroughAssignOuValidator());

        var result = await handler.HandleAsync(
            new AssignGroupOrganizationUnitRequest(group.Id, Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            AuthorizationErrors.PrivilegedGroupOrganizationUnitForbidden,
            result.Error);
    }

    [Fact]
    public async Task RevokeGroupOrganizationUnit_WhenGroupPrivileged_ReturnsForbidden()
    {
        var group = UserGroup.CreatePrivileged("System Administrators", null, Now);
        var store = new FakeAdminStore(group, actorIsPrivileged: true);
        var handler = new RevokeGroupOrganizationUnit(
            store,
            new FakeAuditor(),
            new FakeUnitOfWork(),
            new FakeActor(Guid.NewGuid()),
            new AllowAllDelegation(),
            new PassThroughRevokeOuValidator());

        var result = await handler.HandleAsync(
            new RevokeGroupOrganizationUnitRequest(group.Id, Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            AuthorizationErrors.PrivilegedGroupOrganizationUnitForbidden,
            result.Error);
        Assert.False(store.GroupOuRemoved);
    }

    [Fact]
    public async Task RevokeUserFromGroup_WhenLastPrivilegedMember_ReturnsConflict()
    {
        var privilegedGroup = UserGroup.CreatePrivileged(
            "System Administrators",
            "admins",
            Now);
        var store = new FakeAdminStore(
            privilegedGroup,
            actorIsPrivileged: true,
            activeMemberCount: 1);
        var userId = Guid.NewGuid();
        var handler = new RevokeUserFromGroup(
            store,
            new FakeAuditor(),
            new FakeUnitOfWork(),
            new FakeActor(Guid.NewGuid()),
            new AllowAllDelegation(),
            new PassThroughRevokeUserValidator());

        var result = await handler.HandleAsync(
            new RevokeUserFromGroupRequest(privilegedGroup.Id, userId),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthorizationErrors.LastPrivilegedMemberRequired, result.Error);
        Assert.False(store.MembershipRemoved);
    }

    private sealed class PassThroughAssignUserValidator
        : AbstractValidator<AssignUserToGroupRequest>;

    private sealed class PassThroughAssignPermissionValidator
        : AbstractValidator<AssignGroupPermissionRequest>;

    private sealed class PassThroughAssignOuValidator
        : AbstractValidator<AssignGroupOrganizationUnitRequest>;

    private sealed class PassThroughRevokeOuValidator
        : AbstractValidator<RevokeGroupOrganizationUnitRequest>;

    private sealed class PassThroughRevokeUserValidator
        : AbstractValidator<RevokeUserFromGroupRequest>;

    private sealed class AllowAllDelegation : IDelegationAuthorityService
    {
        public Task<Result?> EnsureCanDelegatePermissionAsync(
            Guid? actorUserId,
            PermissionDefinition permission,
            CancellationToken cancellationToken) =>
            Task.FromResult<Result?>(null);

        public Task<Result?> EnsureCanAssignOrganizationUnitScopeAsync(
            Guid? actorUserId,
            Guid organizationUnitId,
            CancellationToken cancellationToken) =>
            Task.FromResult<Result?>(null);

        public Task<Result?> EnsureCanManageGroupUserAssignmentAsync(
            Guid? actorUserId,
            UserGroup group,
            CancellationToken cancellationToken) =>
            Task.FromResult<Result?>(null);
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class FakeActor(Guid userId) : ICurrentActor
    {
        public Guid? UserId { get; } = userId;

        public string? TraceId => "test";
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

    private sealed class FakeAuditor : IAuthorizationAuditor
    {
        public Task RecordAsync(
            string action,
            string entityType,
            Guid entityId,
            string? data,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class FakeUsers : IUserAccountService
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
            Task.FromResult<UserAccount?>(
                new UserAccount(userId, "u@example.com", Now, IsLockedOut: false, TwoFactorEnabled: false));

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

    private sealed class FakeAdminStore(
        UserGroup group,
        bool actorIsPrivileged,
        PermissionDefinition? permission = null,
        int activeMemberCount = 2) : IAuthorizationAdminStore
    {
        public bool MembershipAdded { get; private set; }

        public bool MembershipRemoved { get; private set; }

        public bool GroupOuRemoved { get; private set; }

        public Task<PermissionDefinition?> FindPermissionByIdAsync(
            Guid id,
            CancellationToken cancellationToken) =>
            Task.FromResult(permission is not null && permission.Id == id ? permission : null);

        public Task<PermissionDefinition?> FindPermissionByCodeAsync(
            string code,
            CancellationToken cancellationToken) =>
            Task.FromResult<PermissionDefinition?>(null);

        public Task<PageResult<PermissionDefinition>> ListPermissionsAsync(
            PageRequest page,
            bool? isActive,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task AddPermissionAsync(
            PermissionDefinition permissionDefinition,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<UserGroup?> FindGroupByIdAsync(
            Guid id,
            CancellationToken cancellationToken) =>
            Task.FromResult<UserGroup?>(group.Id == id ? group : null);

        public Task<UserGroup?> FindGroupByNameAsync(
            string name,
            CancellationToken cancellationToken) =>
            Task.FromResult<UserGroup?>(null);

        public Task<PageResult<UserGroup>> ListGroupsAsync(
            PageRequest page,
            bool? isActive,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task AddGroupAsync(UserGroup userGroup, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<OrganizationUnit?> FindOrganizationUnitByIdAsync(
            Guid id,
            CancellationToken cancellationToken) =>
            Task.FromResult<OrganizationUnit?>(null);

        public Task<OrganizationUnit?> FindOrganizationUnitByCodeAsync(
            string code,
            CancellationToken cancellationToken) =>
            Task.FromResult<OrganizationUnit?>(null);

        public Task<PageResult<OrganizationUnit>> ListOrganizationUnitsAsync(
            PageRequest page,
            bool? isActive,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PageResult<OrganizationUnit>> ListOrganizationUnitsByIdsAsync(
            PageRequest page,
            IReadOnlyCollection<Guid> organizationUnitIds,
            bool? isActive,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task AddOrganizationUnitAsync(
            OrganizationUnit unit,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<bool> GroupPermissionExistsAsync(
            Guid groupId,
            Guid permissionId,
            CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task AddGroupPermissionAsync(
            GroupPermission assignment,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<bool> RemoveGroupPermissionAsync(
            Guid groupId,
            Guid permissionId,
            CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> UserGroupMembershipExistsAsync(
            Guid userId,
            Guid groupId,
            CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> IsMemberOfAnyPrivilegedGroupAsync(
            Guid userId,
            CancellationToken cancellationToken) =>
            Task.FromResult(actorIsPrivileged);

        public Task<int> CountActiveMembersInGroupAsync(
            Guid groupId,
            CancellationToken cancellationToken) =>
            Task.FromResult(activeMemberCount);

        public Task AddUserGroupMembershipAsync(
            UserGroupMembership membership,
            CancellationToken cancellationToken)
        {
            MembershipAdded = true;
            return Task.CompletedTask;
        }

        public Task<MembershipRemoval> TryRemoveUserGroupMembershipAsync(
            Guid userId,
            Guid groupId,
            CancellationToken cancellationToken)
        {
            if (activeMemberCount <= 1 && group.IsPrivileged && group.IsActive)
            {
                return Task.FromResult(MembershipRemoval.LastPrivilegedMember);
            }

            MembershipRemoved = true;
            return Task.FromResult(MembershipRemoval.Removed);
        }
        public Task<bool> GroupOrganizationUnitExistsAsync(
            Guid groupId,
            Guid organizationUnitId,
            CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task AddGroupOrganizationUnitAsync(
            GroupOrganizationUnit assignment,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<bool> RemoveGroupOrganizationUnitAsync(
            Guid groupId,
            Guid organizationUnitId,
            CancellationToken cancellationToken)
        {
            GroupOuRemoved = true;
            return Task.FromResult(true);
        }

        public Task<IReadOnlyList<Guid>> ListGroupOrganizationUnitIdsAsync(
            Guid groupId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);

        public Task<UserOrganizationUnit?> FindUserOrganizationUnitAsync(
            Guid userId,
            Guid organizationUnitId,
            CancellationToken cancellationToken) =>
            Task.FromResult<UserOrganizationUnit?>(null);

        public Task<UserOrganizationUnit?> FindActivePrimaryUserOrganizationUnitAsync(
            Guid userId,
            CancellationToken cancellationToken) =>
            Task.FromResult<UserOrganizationUnit?>(null);

        public Task AddUserOrganizationUnitAsync(
            UserOrganizationUnit membership,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<UserOrganizationUnit>> ListUserOrganizationUnitsAsync(
            Guid userId,
            bool activeOnly,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<UserOrganizationUnit>>([]);

        public Task<IReadOnlyList<PermissionDefinition>> ListActivePermissionsByCodesAsync(
            IReadOnlyCollection<string> codes,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<PermissionDefinition>>([]);

        public Task<IReadOnlyList<Guid>> GetDescendantOrganizationUnitIdsAsync(
            Guid rootOrganizationUnitId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);

        public Task<bool> WouldCreateOrganizationUnitCycleAsync(
            Guid organizationUnitId,
            Guid? newParentId,
            CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<PageResult<AuthorizationAuditEvent>> ListAuditEventsAsync(
            PageRequest page,
            string? action,
            Guid? actorUserId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
