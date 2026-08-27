using Application.Common.Pagination;
using Application.Common.Persistence;
using Application.Common.Results;
using Application.Common.Security;
using Application.Common.Time;
using Application.Features.Authorization;
using Application.Features.Authorization.Abstractions;
using Application.Features.Authorization.Assignments;
using Application.Features.Authorization.Errors;
using Application.Features.Identity.Abstractions;
using Domain.Authorization;
using FluentValidation;

namespace Application.Tests.Features.Authorization;

/// <summary>Agent D — delegated admin grant + OU containment.</summary>
public sealed class DelegationContainmentTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 27, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task PrivilegedActor_CanAssignNonHeldPermission_AndOutsideOu()
    {
        var actorId = Guid.NewGuid();
        var ouOutside = OrganizationUnit.CreateRoot("Outside", "OUT", Now);
        var group = UserGroup.Create("Editors", null, Now);
        var permission = CreatePermission("invoice.read", PermissionRiskLevel.Medium);

        var store = new FakeStore(group, ouOutside, permission, actorIsPrivileged: true);
        var decision = new FakeDecision(actorId, [], []);
        var delegation = CreateDelegation(decision, store);

        var assignPermission = new AssignGroupPermission(
            store,
            new FakeAuditor(),
            new FakeUnitOfWork(),
            new FixedClock(Now),
            new FakeActor(actorId),
            delegation,
            new PassThroughAssignPermissionValidator());
        Assert.True((await assignPermission.HandleAsync(
            new AssignGroupPermissionRequest(group.Id, permission.Id),
            CancellationToken.None)).IsSuccess);

        var assignOu = new AssignGroupOrganizationUnit(
            store,
            new FakeAuditor(),
            new FakeUnitOfWork(),
            new FixedClock(Now),
            new FakeActor(actorId),
            delegation,
            new PassThroughAssignOuValidator());
        Assert.True((await assignOu.HandleAsync(
            new AssignGroupOrganizationUnitRequest(group.Id, ouOutside.Id),
            CancellationToken.None)).IsSuccess);
    }

    [Fact]
    public async Task NonPrivileged_WithHeldPermission_CanAssignWithinAccessibleOu()
    {
        var actorId = Guid.NewGuid();
        var ouA = OrganizationUnit.CreateRoot("A", "A", Now);
        var group = UserGroup.Create("Regional Editors", null, Now);
        var permission = CreatePermission("invoice.read", PermissionRiskLevel.Medium);

        var store = new FakeStore(group, ouA, permission, actorIsPrivileged: false);
        var decision = new FakeDecision(
            actorId,
            [permission.Code, SystemPermissions.AuthorizationAssignmentsDelegate],
            [ouA.Id]);
        var delegation = CreateDelegation(decision, store);

        var assignPermission = new AssignGroupPermission(
            store,
            new FakeAuditor(),
            new FakeUnitOfWork(),
            new FixedClock(Now),
            new FakeActor(actorId),
            delegation,
            new PassThroughAssignPermissionValidator());
        Assert.True((await assignPermission.HandleAsync(
            new AssignGroupPermissionRequest(group.Id, permission.Id),
            CancellationToken.None)).IsSuccess);

        var assignOu = new AssignGroupOrganizationUnit(
            store,
            new FakeAuditor(),
            new FakeUnitOfWork(),
            new FixedClock(Now),
            new FakeActor(actorId),
            delegation,
            new PassThroughAssignOuValidator());
        Assert.True((await assignOu.HandleAsync(
            new AssignGroupOrganizationUnitRequest(group.Id, ouA.Id),
            CancellationToken.None)).IsSuccess);
    }

    [Fact]
    public async Task NonPrivileged_CannotAttachSiblingOu()
    {
        var actorId = Guid.NewGuid();
        var ouA = OrganizationUnit.CreateRoot("A", "A", Now);
        var ouB = OrganizationUnit.CreateRoot("B", "B", Now);
        var group = UserGroup.Create("Regional", null, Now);

        var store = new FakeStore(group, ouB, permission: null, actorIsPrivileged: false);
        store.Units[ouA.Id] = ouA;
        var decision = new FakeDecision(actorId, ["invoice.read"], [ouA.Id]);
        var delegation = CreateDelegation(decision, store);

        var assignOu = new AssignGroupOrganizationUnit(
            store,
            new FakeAuditor(),
            new FakeUnitOfWork(),
            new FixedClock(Now),
            new FakeActor(actorId),
            delegation,
            new PassThroughAssignOuValidator());
        var result = await assignOu.HandleAsync(
            new AssignGroupOrganizationUnitRequest(group.Id, ouB.Id),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthorizationErrors.DelegationScopeForbidden, result.Error);
    }

    [Fact]
    public async Task NonPrivileged_CannotRevokeSiblingOu()
    {
        var actorId = Guid.NewGuid();
        var ouA = OrganizationUnit.CreateRoot("A", "A", Now);
        var ouB = OrganizationUnit.CreateRoot("B", "B", Now);
        var group = UserGroup.Create("Regional", null, Now);

        var store = new FakeStore(group, ouB, permission: null, actorIsPrivileged: false);
        store.Units[ouA.Id] = ouA;
        store.GroupOuRoots[group.Id] = [ouB.Id];
        var decision = new FakeDecision(actorId, ["invoice.read"], [ouA.Id]);
        var delegation = CreateDelegation(decision, store);

        var revokeOu = new RevokeGroupOrganizationUnit(
            store,
            new FakeAuditor(),
            new FakeUnitOfWork(),
            new FakeActor(actorId),
            delegation,
            new PassThroughRevokeOuValidator());
        var result = await revokeOu.HandleAsync(
            new RevokeGroupOrganizationUnitRequest(group.Id, ouB.Id),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthorizationErrors.DelegationScopeForbidden, result.Error);
        Assert.False(store.GroupOuRemoved);
    }

    [Fact]
    public async Task NonPrivileged_CanRevokeWithinAccessibleOu()
    {
        var actorId = Guid.NewGuid();
        var ouA = OrganizationUnit.CreateRoot("A", "A", Now);
        var group = UserGroup.Create("Regional Editors", null, Now);

        var store = new FakeStore(group, ouA, permission: null, actorIsPrivileged: false);
        store.GroupOuRoots[group.Id] = [ouA.Id];
        var decision = new FakeDecision(
            actorId,
            [SystemPermissions.AuthorizationAssignmentsDelegate],
            [ouA.Id]);
        var delegation = CreateDelegation(decision, store);

        var revokeOu = new RevokeGroupOrganizationUnit(
            store,
            new FakeAuditor(),
            new FakeUnitOfWork(),
            new FakeActor(actorId),
            delegation,
            new PassThroughRevokeOuValidator());
        var result = await revokeOu.HandleAsync(
            new RevokeGroupOrganizationUnitRequest(group.Id, ouA.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(store.GroupOuRemoved);
    }

    [Fact]
    public async Task NonPrivileged_CannotAssignCriticalPermission()
    {
        var actorId = Guid.NewGuid();
        var ouA = OrganizationUnit.CreateRoot("A", "A", Now);
        var group = UserGroup.Create("Regional", null, Now);
        var critical = PermissionDefinition.Create(
            SystemPermissions.AuthorizationGroupsWrite,
            "Manage groups",
            "authorization",
            "groups.write",
            PermissionScopeMode.Global,
            Now,
            "authorization.groups",
            PermissionRiskLevel.Critical,
            isSystemManaged: true);

        var store = new FakeStore(group, ouA, critical, actorIsPrivileged: false);
        // Even if somehow listed in codes, Critical must be rejected.
        var decision = new FakeDecision(actorId, [critical.Code], [ouA.Id]);
        var delegation = CreateDelegation(decision, store);

        var handler = new AssignGroupPermission(
            store,
            new FakeAuditor(),
            new FakeUnitOfWork(),
            new FixedClock(Now),
            new FakeActor(actorId),
            delegation,
            new PassThroughAssignPermissionValidator());
        var result = await handler.HandleAsync(
            new AssignGroupPermissionRequest(group.Id, critical.Id),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            AuthorizationErrors.PrivilegedPermissionRequiresPrivilegedGroup,
            result.Error);
    }

    [Fact]
    public async Task NonPrivileged_CannotModifyPrivilegedGroup()
    {
        var actorId = Guid.NewGuid();
        var ouA = OrganizationUnit.CreateRoot("A", "A", Now);
        var privileged = UserGroup.CreatePrivileged("System Administrators", null, Now);
        var permission = CreatePermission("invoice.read", PermissionRiskLevel.Medium);

        var store = new FakeStore(privileged, ouA, permission, actorIsPrivileged: false);
        store.GroupOuRoots[privileged.Id] = [ouA.Id];
        var decision = new FakeDecision(actorId, [permission.Code], [ouA.Id]);
        var delegation = CreateDelegation(decision, store);

        var assignPermission = new AssignGroupPermission(
            store,
            new FakeAuditor(),
            new FakeUnitOfWork(),
            new FixedClock(Now),
            new FakeActor(actorId),
            delegation,
            new PassThroughAssignPermissionValidator());
        var permissionResult = await assignPermission.HandleAsync(
            new AssignGroupPermissionRequest(privileged.Id, permission.Id),
            CancellationToken.None);
        Assert.True(permissionResult.IsFailure);
        Assert.Equal(AuthorizationErrors.PrivilegedGroupMutationForbidden, permissionResult.Error);

        var assignUser = new AssignUserToGroup(
            store,
            new FakeUsers(),
            new FakeAuditor(),
            new FakeUnitOfWork(),
            new FixedClock(Now),
            new FakeActor(actorId),
            delegation,
            new PassThroughAssignUserValidator());
        var userResult = await assignUser.HandleAsync(
            new AssignUserToGroupRequest(privileged.Id, Guid.NewGuid()),
            CancellationToken.None);
        Assert.True(userResult.IsFailure);
        Assert.Equal(AuthorizationErrors.PrivilegedGroupMutationForbidden, userResult.Error);
    }

    [Fact]
    public async Task EmptyAccessibleOus_FailClosedOnScopeAssign()
    {
        var actorId = Guid.NewGuid();
        var ouA = OrganizationUnit.CreateRoot("A", "A", Now);
        var group = UserGroup.Create("Regional", null, Now);

        var store = new FakeStore(group, ouA, permission: null, actorIsPrivileged: false);
        var decision = new FakeDecision(actorId, ["invoice.read"], []);
        var delegation = CreateDelegation(decision, store);

        var assignOu = new AssignGroupOrganizationUnit(
            store,
            new FakeAuditor(),
            new FakeUnitOfWork(),
            new FixedClock(Now),
            new FakeActor(actorId),
            delegation,
            new PassThroughAssignOuValidator());
        var result = await assignOu.HandleAsync(
            new AssignGroupOrganizationUnitRequest(group.Id, ouA.Id),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthorizationErrors.DelegationScopeForbidden, result.Error);
    }

    [Fact]
    public async Task UserOrganizationUnit_OutsideScope_Denied()
    {
        var actorId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var ouA = OrganizationUnit.CreateRoot("A", "A", Now);
        var ouB = OrganizationUnit.CreateRoot("B", "B", Now);
        var group = UserGroup.Create("Regional", null, Now);

        var store = new FakeStore(group, ouB, permission: null, actorIsPrivileged: false);
        store.Units[ouA.Id] = ouA;
        var decision = new FakeDecision(actorId, ["x"], [ouA.Id]);
        var delegation = CreateDelegation(decision, store);

        var handler = new AssignUserOrganizationUnit(
            store,
            new FakeUsers(targetUserId),
            new FakeAuditor(),
            new FakeUnitOfWork(),
            new FixedClock(Now),
            new FakeActor(actorId),
            delegation,
            new PassThroughUserOuAssignValidator());
        var result = await handler.HandleAsync(
            new AssignUserOrganizationUnitRequest(
                targetUserId,
                ouB.Id,
                OrganizationUnitRelationship.Primary),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthorizationErrors.DelegationScopeForbidden.Code, result.Error!.Code);
    }

    [Fact]
    public async Task GrantContainment_CannotAssignPermissionActorDoesNotHold()
    {
        var actorId = Guid.NewGuid();
        var ouA = OrganizationUnit.CreateRoot("A", "A", Now);
        var group = UserGroup.Create("Regional", null, Now);
        var held = CreatePermission("invoice.read", PermissionRiskLevel.Medium);
        var notHeld = CreatePermission("invoice.write", PermissionRiskLevel.High);

        var store = new FakeStore(group, ouA, notHeld, actorIsPrivileged: false);
        store.Permissions[held.Id] = held;
        var decision = new FakeDecision(actorId, [held.Code], [ouA.Id]);
        var delegation = CreateDelegation(decision, store);

        var handler = new AssignGroupPermission(
            store,
            new FakeAuditor(),
            new FakeUnitOfWork(),
            new FixedClock(Now),
            new FakeActor(actorId),
            delegation,
            new PassThroughAssignPermissionValidator());
        var result = await handler.HandleAsync(
            new AssignGroupPermissionRequest(group.Id, notHeld.Id),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthorizationErrors.DelegationPermissionForbidden, result.Error);
    }

    [Fact]
    public async Task NonPrivileged_CannotAssignUserToGroupWithoutOuRoots()
    {
        var actorId = Guid.NewGuid();
        var ouA = OrganizationUnit.CreateRoot("A", "A", Now);
        var group = UserGroup.Create("Unscoped", null, Now);

        var store = new FakeStore(group, ouA, permission: null, actorIsPrivileged: false);
        var decision = new FakeDecision(actorId, ["invoice.read"], [ouA.Id]);
        var delegation = CreateDelegation(decision, store);

        var handler = new AssignUserToGroup(
            store,
            new FakeUsers(),
            new FakeAuditor(),
            new FakeUnitOfWork(),
            new FixedClock(Now),
            new FakeActor(actorId),
            delegation,
            new PassThroughAssignUserValidator());
        var result = await handler.HandleAsync(
            new AssignUserToGroupRequest(group.Id, Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthorizationErrors.DelegationGroupForbidden, result.Error);
    }

    private static DelegationAuthorityService CreateDelegation(
        IAuthorizationDecisionService decision,
        IAuthorizationAdminStore store) =>
        new(decision, store);

    private static PermissionDefinition CreatePermission(
        string code,
        PermissionRiskLevel risk) =>
        PermissionDefinition.Create(
            code,
            code,
            "invoice",
            code.Contains("write", StringComparison.Ordinal) ? "write" : "read",
            PermissionScopeMode.OrganizationUnit,
            Now,
            "invoice",
            risk);

    private sealed class PassThroughAssignPermissionValidator
        : AbstractValidator<AssignGroupPermissionRequest>;

    private sealed class PassThroughAssignOuValidator
        : AbstractValidator<AssignGroupOrganizationUnitRequest>;

    private sealed class PassThroughRevokeOuValidator
        : AbstractValidator<RevokeGroupOrganizationUnitRequest>;

    private sealed class PassThroughAssignUserValidator
        : AbstractValidator<AssignUserToGroupRequest>;

    private sealed class PassThroughUserOuAssignValidator
        : AbstractValidator<AssignUserOrganizationUnitRequest>;

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class FakeActor(Guid userId) : ICurrentActor
    {
        public Guid? UserId { get; } = userId;

        public string? TraceId => "test";
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

    private sealed class FakeUsers(Guid? knownUserId = null) : IUserAccountService
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
                knownUserId is null || knownUserId == userId
                    ? new UserAccount(userId, "u@example.com", Now, false, false)
                    : null);

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

    private sealed class FakeDecision(
        Guid userId,
        IReadOnlyList<string> permissionCodes,
        IReadOnlyList<Guid> accessibleOus) : IAuthorizationDecisionService
    {
        public Task<UserAuthorizationContext?> GetContextAsync(
            Guid id,
            CancellationToken cancellationToken) =>
            Task.FromResult<UserAuthorizationContext?>(
                id == userId
                    ? new UserAuthorizationContext(
                        userId,
                        [],
                        permissionCodes,
                        accessibleOus,
                        UserAuthorizationContext.EmptyPermissionScopes)
                    : null);

        public Task<AuthorizationDecision> HasPermissionAsync(
            Guid id,
            string permissionCode,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<AuthorizationDecision> HasPermissionOnUnitAsync(
            Guid id,
            string permissionCode,
            Guid organizationUnitId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> CanAccessOrganizationUnitAsync(
            Guid id,
            Guid organizationUnitId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeStore(
        UserGroup group,
        OrganizationUnit unit,
        PermissionDefinition? permission,
        bool actorIsPrivileged) : IAuthorizationAdminStore
    {
        public Dictionary<Guid, OrganizationUnit> Units { get; } = new() { [unit.Id] = unit };

        public Dictionary<Guid, PermissionDefinition> Permissions { get; } = permission is null
            ? new()
            : new() { [permission.Id] = permission };

        public Dictionary<Guid, List<Guid>> GroupOuRoots { get; } = new();

        public bool GroupPermissionAdded { get; private set; }

        public bool GroupOuAdded { get; private set; }

        public bool GroupOuRemoved { get; private set; }

        public Task<PermissionDefinition?> FindPermissionByIdAsync(
            Guid id,
            CancellationToken cancellationToken) =>
            Task.FromResult(Permissions.GetValueOrDefault(id));

        public Task<PermissionDefinition?> FindPermissionByCodeAsync(
            string code,
            CancellationToken cancellationToken) =>
            Task.FromResult(
                Permissions.Values.FirstOrDefault(p => p.Code == code));

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
            Task.FromResult(Units.GetValueOrDefault(id));

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
            OrganizationUnit organizationUnit,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<bool> GroupPermissionExistsAsync(
            Guid groupId,
            Guid permissionId,
            CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task AddGroupPermissionAsync(
            GroupPermission assignment,
            CancellationToken cancellationToken)
        {
            GroupPermissionAdded = true;
            return Task.CompletedTask;
        }

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
            Task.FromResult(2);

        public Task AddUserGroupMembershipAsync(
            UserGroupMembership membership,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<MembershipRemoval> TryRemoveUserGroupMembershipAsync(
            Guid userId,
            Guid groupId,
            CancellationToken cancellationToken) =>
            Task.FromResult(MembershipRemoval.Removed);

        public Task<bool> GroupOrganizationUnitExistsAsync(
            Guid groupId,
            Guid organizationUnitId,
            CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task AddGroupOrganizationUnitAsync(
            GroupOrganizationUnit assignment,
            CancellationToken cancellationToken)
        {
            GroupOuAdded = true;
            if (!GroupOuRoots.TryGetValue(assignment.GroupId, out var list))
            {
                list = [];
                GroupOuRoots[assignment.GroupId] = list;
            }

            list.Add(assignment.OrganizationUnitId);
            return Task.CompletedTask;
        }

        public Task<bool> RemoveGroupOrganizationUnitAsync(
            Guid groupId,
            Guid organizationUnitId,
            CancellationToken cancellationToken)
        {
            if (!GroupOuRoots.TryGetValue(groupId, out var roots) ||
                !roots.Remove(organizationUnitId))
            {
                return Task.FromResult(false);
            }

            GroupOuRemoved = true;
            return Task.FromResult(true);
        }

        public Task<IReadOnlyList<Guid>> ListGroupOrganizationUnitIdsAsync(
            Guid groupId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Guid>>(
                GroupOuRoots.TryGetValue(groupId, out var roots) ? roots : []);

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
