using Application.Common.Pagination;
using Application.Common.Persistence;
using Application.Common.Results;
using Application.Common.Security;
using Application.Common.Time;
using Application.Features.Authorization.Abstractions;
using Application.Features.Authorization.Assignments;
using Application.Features.Authorization.Errors;
using Application.Features.Authorization.GetContext;
using Application.Features.Identity.Abstractions;
using Domain.Authorization;
using FluentValidation;

namespace Application.Tests.Features.Authorization;

public sealed class UserOrganizationUnitAndCapabilitiesTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 27, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Assign_CreatesPrimary_AndRejectsSecondPrimary()
    {
        var userId = Guid.NewGuid();
        var ouA = OrganizationUnit.CreateRoot("A", "A", Now);
        var ouB = OrganizationUnit.CreateRoot("B", "B", Now);
        var store = new FakeStore(ouA, ouB);
        var handler = new AssignUserOrganizationUnit(
            store,
            new FakeUsers(userId),
            new FakeAuditor(),
            new FakeUnitOfWork(),
            new FixedClock(Now),
            new FakeActor(Guid.NewGuid()),
            new AllowAllDelegation(),
            new PassThroughAssignValidator());

        var first = await handler.HandleAsync(
            new AssignUserOrganizationUnitRequest(
                userId,
                ouA.Id,
                OrganizationUnitRelationship.Primary),
            CancellationToken.None);
        Assert.True(first.IsSuccess);

        var second = await handler.HandleAsync(
            new AssignUserOrganizationUnitRequest(
                userId,
                ouB.Id,
                OrganizationUnitRelationship.Primary),
            CancellationToken.None);
        Assert.False(second.IsSuccess);
        Assert.Equal(
            AuthorizationErrors.PrimaryOrganizationUnitAlreadyAssigned.Code,
            second.Error!.Code);
    }

    [Fact]
    public async Task Assign_AllowsAdditional_AlongsidePrimary()
    {
        var userId = Guid.NewGuid();
        var ouA = OrganizationUnit.CreateRoot("A", "A", Now);
        var ouB = OrganizationUnit.CreateRoot("B", "B", Now);
        var store = new FakeStore(ouA, ouB);
        var handler = new AssignUserOrganizationUnit(
            store,
            new FakeUsers(userId),
            new FakeAuditor(),
            new FakeUnitOfWork(),
            new FixedClock(Now),
            new FakeActor(Guid.NewGuid()),
            new AllowAllDelegation(),
            new PassThroughAssignValidator());

        Assert.True((await handler.HandleAsync(
            new AssignUserOrganizationUnitRequest(
                userId, ouA.Id, OrganizationUnitRelationship.Primary),
            CancellationToken.None)).IsSuccess);

        var additional = await handler.HandleAsync(
            new AssignUserOrganizationUnitRequest(
                userId, ouB.Id, OrganizationUnitRelationship.Additional),
            CancellationToken.None);
        Assert.True(additional.IsSuccess);
        Assert.Equal(2, store.Memberships.Count);
    }

    [Fact]
    public async Task Revoke_DeactivatesMembership()
    {
        var userId = Guid.NewGuid();
        var ou = OrganizationUnit.CreateRoot("A", "A", Now);
        var store = new FakeStore(ou);
        var assign = new AssignUserOrganizationUnit(
            store,
            new FakeUsers(userId),
            new FakeAuditor(),
            new FakeUnitOfWork(),
            new FixedClock(Now),
            new FakeActor(Guid.NewGuid()),
            new AllowAllDelegation(),
            new PassThroughAssignValidator());
        await assign.HandleAsync(
            new AssignUserOrganizationUnitRequest(
                userId, ou.Id, OrganizationUnitRelationship.Primary),
            CancellationToken.None);

        var revoke = new RevokeUserOrganizationUnit(
            store,
            new FakeAuditor(),
            new FakeUnitOfWork(),
            new FakeActor(Guid.NewGuid()),
            new AllowAllDelegation(),
            new PassThroughRevokeValidator());
        var result = await revoke.HandleAsync(
            new RevokeUserOrganizationUnitRequest(userId, ou.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(store.Memberships.Single().IsActive);
    }

    [Fact]
    public async Task Capabilities_ReturnsMetadataForGrantedCodesOnly()
    {
        var userId = Guid.NewGuid();
        var granted = PermissionDefinition.Create(
            "users.read",
            "View users",
            "users",
            "read",
            PermissionScopeMode.Global,
            Now,
            "users",
            PermissionRiskLevel.Medium);
        var other = PermissionDefinition.Create(
            "users.write",
            "Manage users",
            "users",
            "write",
            PermissionScopeMode.Global,
            Now,
            "users",
            PermissionRiskLevel.High);

        var ouId = Guid.NewGuid();
        var membership = UserOrganizationUnit.Create(
            userId,
            ouId,
            OrganizationUnitRelationship.Primary,
            Now);

        var decision = new FakeDecision(
            new UserAuthorizationContext(
                userId,
                ["Editors"],
                [granted.Code],
                [Guid.NewGuid()],
                new Dictionary<string, PermissionScopeMode>(StringComparer.Ordinal)
                {
                    [granted.Code] = PermissionScopeMode.Global
                }));
        var store = new FakeStore();
        store.Permissions.Add(granted);
        store.Permissions.Add(other);
        store.Memberships.Add(membership);

        var result = await new GetUserCapabilities(decision, store)
            .HandleAsync(userId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var payload = result.Value!;
        Assert.Equal(["Editors"], payload.Groups);
        Assert.Single(payload.Permissions);
        Assert.Equal("users.read", payload.Permissions[0].Code);
        Assert.Equal("users", payload.Permissions[0].Resource);
        Assert.Equal(PermissionScopeMode.Global, payload.Permissions[0].ScopeMode);
        Assert.Equal(PermissionRiskLevel.Medium, payload.Permissions[0].RiskLevel);
        Assert.Single(payload.UserOrganizationUnits);
        Assert.Equal(ouId, payload.UserOrganizationUnits[0].OrganizationUnitId);
        Assert.Single(payload.AccessibleOrganizationUnitIds);
        Assert.DoesNotContain(
            payload.AccessibleOrganizationUnitIds,
            id => id == ouId);
    }

    private sealed class PassThroughAssignValidator
        : AbstractValidator<AssignUserOrganizationUnitRequest>;

    private sealed class PassThroughRevokeValidator
        : AbstractValidator<RevokeUserOrganizationUnitRequest>;

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow => utcNow;
    }

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
        public Task RecordAsync(
            string action,
            string entityType,
            Guid entityId,
            string? details,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
            Task.FromResult(1);

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

    private sealed class FakeDecision(UserAuthorizationContext context)
        : IAuthorizationDecisionService
    {
        public Task<UserAuthorizationContext?> GetContextAsync(
            Guid userId,
            CancellationToken cancellationToken) =>
            Task.FromResult<UserAuthorizationContext?>(context);

        public Task<AuthorizationDecision> HasPermissionAsync(
            Guid userId,
            string permissionCode,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<AuthorizationDecision> HasPermissionOnUnitAsync(
            Guid userId,
            string permissionCode,
            Guid organizationUnitId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> CanAccessOrganizationUnitAsync(
            Guid userId,
            Guid organizationUnitId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeUsers(Guid userId) : IUserAccountService
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
            Guid id,
            CancellationToken cancellationToken) =>
            Task.FromResult<UserAccount?>(
                id == userId
                    ? new UserAccount(userId, "u@example.com", Now, false, false)
                    : null);

        public Task<PageResult<UserAccount>> ListAsync(
            PageRequest page,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Result<AuthenticatorSetup>> BeginAuthenticatorSetupAsync(
            Guid id,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Result> ConfirmAuthenticatorSetupAsync(
            Guid id,
            string code,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Result> DisableAuthenticatorAsync(
            Guid id,
            string code,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Result> VerifyAuthenticatorCodeAsync(
            Guid id,
            string code,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeStore : IAuthorizationAdminStore
    {
        private readonly Dictionary<Guid, OrganizationUnit> _units;

        public FakeStore(params OrganizationUnit[] units)
        {
            _units = units.ToDictionary(unit => unit.Id);
        }

        public List<UserOrganizationUnit> Memberships { get; } = [];

        public List<PermissionDefinition> Permissions { get; } = [];

        public Task<OrganizationUnit?> FindOrganizationUnitByIdAsync(
            Guid id,
            CancellationToken cancellationToken) =>
            Task.FromResult(_units.TryGetValue(id, out var unit) ? unit : null);

        public Task<UserOrganizationUnit?> FindUserOrganizationUnitAsync(
            Guid userId,
            Guid organizationUnitId,
            CancellationToken cancellationToken) =>
            Task.FromResult(
                Memberships.FirstOrDefault(m =>
                    m.UserId == userId && m.OrganizationUnitId == organizationUnitId));

        public Task<UserOrganizationUnit?> FindActivePrimaryUserOrganizationUnitAsync(
            Guid userId,
            CancellationToken cancellationToken) =>
            Task.FromResult(
                Memberships.FirstOrDefault(m =>
                    m.UserId == userId &&
                    m.IsActive &&
                    m.Relationship == OrganizationUnitRelationship.Primary));

        public Task AddUserOrganizationUnitAsync(
            UserOrganizationUnit membership,
            CancellationToken cancellationToken)
        {
            Memberships.Add(membership);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<UserOrganizationUnit>> ListUserOrganizationUnitsAsync(
            Guid userId,
            bool activeOnly,
            CancellationToken cancellationToken)
        {
            IEnumerable<UserOrganizationUnit> query = Memberships.Where(m => m.UserId == userId);
            if (activeOnly)
            {
                query = query.Where(m => m.IsActive);
            }

            return Task.FromResult<IReadOnlyList<UserOrganizationUnit>>(query.ToArray());
        }

        public Task<IReadOnlyList<PermissionDefinition>> ListActivePermissionsByCodesAsync(
            IReadOnlyCollection<string> codes,
            CancellationToken cancellationToken)
        {
            var set = codes.ToHashSet(StringComparer.Ordinal);
            return Task.FromResult<IReadOnlyList<PermissionDefinition>>(
                Permissions.Where(p => p.IsActive && set.Contains(p.Code)).ToArray());
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
}
