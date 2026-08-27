using Application.Common.Pagination;
using Application.Common.Persistence;
using Application.Common.Results;
using Application.Common.Security;
using Application.Features.Authorization.Abstractions;
using Application.Features.Authorization.Errors;
using Application.Features.Authorization.OrganizationUnits;
using Domain.Authorization;
using FluentValidation;

namespace Application.Tests.Features.Authorization;

public sealed class MoveOrganizationUnitTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 27, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Move_Succeeds_UnderNewParent()
    {
        var root = OrganizationUnit.CreateRoot("Root", "ROOT", Now);
        var child = OrganizationUnit.Create("Child", "CHILD", root.Id, Now);
        var sibling = OrganizationUnit.Create("Sibling", "SIB", root.Id, Now);
        var store = new FakeStore(root, child, sibling);
        var auditor = new FakeAuditor();
        var handler = CreateHandler(store, auditor);

        var result = await handler.HandleAsync(
            sibling.Id,
            new MoveOrganizationUnitRequest(child.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(child.Id, sibling.ParentId);
        Assert.Equal(child.Id, result.Value!.ParentId);
        Assert.Contains(
            auditor.Actions,
            action => action == AuthorizationAuditActions.OrganizationUnitMoved);
        Assert.Equal(1, store.SaveCount);
    }

    [Fact]
    public async Task Move_Succeeds_ToRoot()
    {
        var root = OrganizationUnit.CreateRoot("Root", "ROOT", Now);
        var child = OrganizationUnit.Create("Child", "CHILD", root.Id, Now);
        var store = new FakeStore(root, child);
        var handler = CreateHandler(store, new FakeAuditor());

        var result = await handler.HandleAsync(
            child.Id,
            new MoveOrganizationUnitRequest(null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(child.ParentId);
        Assert.Null(result.Value!.ParentId);
    }

    [Fact]
    public async Task Move_Denies_Cycle()
    {
        var root = OrganizationUnit.CreateRoot("Root", "ROOT", Now);
        var child = OrganizationUnit.Create("Child", "CHILD", root.Id, Now);
        var grandchild = OrganizationUnit.Create("Grand", "GRAND", child.Id, Now);
        var store = new FakeStore(root, child, grandchild);
        var handler = CreateHandler(store, new FakeAuditor());

        var result = await handler.HandleAsync(
            root.Id,
            new MoveOrganizationUnitRequest(grandchild.Id),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthorizationErrors.OrganizationUnitCycle.Code, result.Error!.Code);
        Assert.Null(root.ParentId);
    }

    [Fact]
    public async Task Move_ReturnsNotFound_WhenUnitMissing()
    {
        var store = new FakeStore();
        var handler = CreateHandler(store, new FakeAuditor());

        var result = await handler.HandleAsync(
            Guid.NewGuid(),
            new MoveOrganizationUnitRequest(null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthorizationErrors.OrganizationUnitNotFound.Code, result.Error!.Code);
    }

    [Fact]
    public async Task Move_ReturnsParentNotFound_WhenParentMissing()
    {
        var root = OrganizationUnit.CreateRoot("Root", "ROOT", Now);
        var store = new FakeStore(root);
        var handler = CreateHandler(store, new FakeAuditor());

        var result = await handler.HandleAsync(
            root.Id,
            new MoveOrganizationUnitRequest(Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            AuthorizationErrors.ParentOrganizationUnitNotFound.Code,
            result.Error!.Code);
    }

    private static MoveOrganizationUnit CreateHandler(
        FakeStore store,
        FakeAuditor auditor) =>
        new(
            store,
            auditor,
            new FakeUnitOfWork(store),
            new FakeActor(Guid.NewGuid()),
            new AllowAllDelegation(),
            new PassThroughMoveValidator());

    private sealed class PassThroughMoveValidator
        : AbstractValidator<MoveOrganizationUnitRequest>;

    private sealed class FakeActor(Guid userId) : ICurrentActor
    {
        public Guid? UserId { get; } = userId;

        public string? TraceId => "test";
    }

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

    private sealed class FakeAuditor : IAuthorizationAuditor
    {
        public List<string> Actions { get; } = [];

        public Task RecordAsync(
            string action,
            string entityType,
            Guid entityId,
            string? details,
            CancellationToken cancellationToken)
        {
            Actions.Add(action);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork(FakeStore store) : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            store.SaveCount++;
            return Task.FromResult(1);
        }

        public Task<IUnitOfWorkTransaction> BeginTransactionAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult<IUnitOfWorkTransaction>(new NoopTransaction());

        private sealed class NoopTransaction : IUnitOfWorkTransaction
        {
            public Task CommitAsync(CancellationToken cancellationToken) =>
                Task.CompletedTask;

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }

    private sealed class FakeStore : IAuthorizationAdminStore
    {
        private readonly Dictionary<Guid, OrganizationUnit> _units;

        public FakeStore(params OrganizationUnit[] units)
        {
            _units = units.ToDictionary(unit => unit.Id);
        }

        public int SaveCount { get; set; }

        public Task<OrganizationUnit?> FindOrganizationUnitByIdAsync(
            Guid id,
            CancellationToken cancellationToken) =>
            Task.FromResult(_units.TryGetValue(id, out var unit) ? unit : null);

        public Task<bool> WouldCreateOrganizationUnitCycleAsync(
            Guid organizationUnitId,
            Guid? newParentId,
            CancellationToken cancellationToken)
        {
            if (newParentId is null)
            {
                return Task.FromResult(false);
            }

            if (newParentId.Value == organizationUnitId)
            {
                return Task.FromResult(true);
            }

            var current = newParentId;
            var guard = 0;
            while (current is Guid parentId)
            {
                if (parentId == organizationUnitId)
                {
                    return Task.FromResult(true);
                }

                if (!_units.TryGetValue(parentId, out var unit))
                {
                    break;
                }

                current = unit.ParentId;
                if (++guard > 10_000)
                {
                    return Task.FromResult(true);
                }
            }

            return Task.FromResult(false);
        }

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

        public Task<OrganizationUnit?> FindOrganizationUnitByCodeAsync(
            string code,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

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

        public Task<bool> IsMemberOfAnyPrivilegedGroupAsync(
            Guid userId,
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

        public Task<MembershipRemoval> TryRemoveUserGroupMembershipAsync(
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

        public Task<IReadOnlyList<Guid>> ListGroupOrganizationUnitIdsAsync(
            Guid groupId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);

        public Task<UserOrganizationUnit?> FindUserOrganizationUnitAsync(
            Guid userId,
            Guid organizationUnitId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<UserOrganizationUnit?> FindActivePrimaryUserOrganizationUnitAsync(
            Guid userId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task AddUserOrganizationUnitAsync(
            UserOrganizationUnit membership,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<UserOrganizationUnit>> ListUserOrganizationUnitsAsync(
            Guid userId,
            bool activeOnly,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<PermissionDefinition>> ListActivePermissionsByCodesAsync(
            IReadOnlyCollection<string> codes,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Guid>> GetDescendantOrganizationUnitIdsAsync(
            Guid rootOrganizationUnitId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PageResult<AuthorizationAuditEvent>> ListAuditEventsAsync(
            PageRequest page,
            string? action,
            Guid? actorUserId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
