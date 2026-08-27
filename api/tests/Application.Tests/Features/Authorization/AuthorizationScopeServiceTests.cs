using Application.Features.Authorization;
using Application.Features.Authorization.Abstractions;
using Application.Features.Authorization.OrganizationUnits;
using Application.Common.Pagination;
using Domain.Authorization;

namespace Application.Tests.Features.Authorization;

public sealed class AuthorizationScopeServiceTests
{
    private static readonly Guid UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid RootA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ChildA = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid GrandchildA = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid SiblingB = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid RootC = Guid.Parse("55555555-5555-5555-5555-555555555555");

    private const string OuScopedPermission = "orders.read";
    private const string GlobalPermission = "authorization.organization-units.read";

    [Fact]
    public async Task Authorize_AllowsParentChildAndDescendant_DeniesSibling()
    {
        var accessible = OrganizationUnitHierarchy.CollectAccessibleIds(
            [RootA],
            [
                (RootA, null),
                (ChildA, RootA),
                (GrandchildA, ChildA),
                (SiblingB, null)
            ]);

        var service = CreateService(
            accessible,
            new Dictionary<string, PermissionScopeMode>
            {
                [OuScopedPermission] = PermissionScopeMode.OrganizationUnit
            },
            OuScopedPermission);

        Assert.True((await service.AuthorizePermissionOnResourceAsync(
            UserId, OuScopedPermission, RootA, CancellationToken.None)).IsAllowed);
        Assert.True((await service.AuthorizePermissionOnResourceAsync(
            UserId, OuScopedPermission, ChildA, CancellationToken.None)).IsAllowed);
        Assert.True((await service.AuthorizePermissionOnResourceAsync(
            UserId, OuScopedPermission, GrandchildA, CancellationToken.None)).IsAllowed);

        var sibling = await service.AuthorizePermissionOnResourceAsync(
            UserId, OuScopedPermission, SiblingB, CancellationToken.None);
        Assert.False(sibling.IsAllowed);
        Assert.Equal(AuthorizationDecisionReason.OutsideUnitScope, sibling.Reason);
    }

    [Fact]
    public async Task Authorize_UnionsMultipleOrganizationUnitRoots()
    {
        var accessible = OrganizationUnitHierarchy.CollectAccessibleIds(
            [RootA, RootC],
            [
                (RootA, null),
                (ChildA, RootA),
                (RootC, null),
                (SiblingB, null)
            ]);

        var service = CreateService(
            accessible,
            new Dictionary<string, PermissionScopeMode>
            {
                [OuScopedPermission] = PermissionScopeMode.OrganizationUnit
            },
            OuScopedPermission);

        Assert.True((await service.AuthorizePermissionOnResourceAsync(
            UserId, OuScopedPermission, ChildA, CancellationToken.None)).IsAllowed);
        Assert.True((await service.AuthorizePermissionOnResourceAsync(
            UserId, OuScopedPermission, RootC, CancellationToken.None)).IsAllowed);
        Assert.False((await service.AuthorizePermissionOnResourceAsync(
            UserId, OuScopedPermission, SiblingB, CancellationToken.None)).IsAllowed);
    }

    [Fact]
    public async Task Authorize_GlobalScopeMode_SkipsOrganizationUnitCheck()
    {
        var service = CreateService(
            accessibleUnitIds: [],
            scopes: new Dictionary<string, PermissionScopeMode>
            {
                [GlobalPermission] = PermissionScopeMode.Global
            },
            GlobalPermission);

        var decision = await service.AuthorizePermissionOnResourceAsync(
            UserId,
            GlobalPermission,
            SiblingB,
            CancellationToken.None);

        Assert.True(decision.IsAllowed);
    }

    [Fact]
    public async Task Authorize_NoneScopeMode_SkipsOrganizationUnitCheck()
    {
        const string capability = "ui.show-export";
        var service = CreateService(
            accessibleUnitIds: [],
            scopes: new Dictionary<string, PermissionScopeMode>
            {
                [capability] = PermissionScopeMode.None
            },
            capability);

        var decision = await service.AuthorizePermissionWithOptionalUnitAsync(
            UserId,
            capability,
            organizationUnitId: null,
            CancellationToken.None);

        Assert.True(decision.IsAllowed);
    }

    [Fact]
    public async Task Authorize_OrganizationUnitScope_WithEmptyAccessible_Denies()
    {
        var service = CreateService(
            accessibleUnitIds: [],
            scopes: new Dictionary<string, PermissionScopeMode>
            {
                [OuScopedPermission] = PermissionScopeMode.OrganizationUnit
            },
            OuScopedPermission);

        var decision = await service.AuthorizePermissionOnResourceAsync(
            UserId,
            OuScopedPermission,
            RootA,
            CancellationToken.None);

        Assert.False(decision.IsAllowed);
        Assert.Equal(AuthorizationDecisionReason.OutsideUnitScope, decision.Reason);
    }

    [Fact]
    public async Task Authorize_OrganizationUnitScope_MissingRouteUnit_Denies()
    {
        var service = CreateService(
            [RootA],
            new Dictionary<string, PermissionScopeMode>
            {
                [OuScopedPermission] = PermissionScopeMode.OrganizationUnit
            },
            OuScopedPermission);

        var decision = await service.AuthorizePermissionWithOptionalUnitAsync(
            UserId,
            OuScopedPermission,
            organizationUnitId: null,
            CancellationToken.None);

        Assert.False(decision.IsAllowed);
        Assert.Equal(AuthorizationDecisionReason.OutsideUnitScope, decision.Reason);
    }

    [Fact]
    public async Task Authorize_MissingPermission_Denies()
    {
        var service = CreateService(
            [RootA],
            new Dictionary<string, PermissionScopeMode>
            {
                [OuScopedPermission] = PermissionScopeMode.OrganizationUnit
            },
            grantedPermissionCodes: OuScopedPermission);

        var decision = await service.AuthorizePermissionOnResourceAsync(
            UserId,
            "orders.write",
            RootA,
            CancellationToken.None);

        Assert.False(decision.IsAllowed);
        Assert.Equal(AuthorizationDecisionReason.MissingPermission, decision.Reason);
    }

    [Fact]
    public async Task AuthorizeBulk_AllOrNothing_RejectsWhenAnyOutOfScope()
    {
        var service = CreateService(
            [RootA, ChildA],
            new Dictionary<string, PermissionScopeMode>
            {
                [OuScopedPermission] = PermissionScopeMode.OrganizationUnit
            },
            OuScopedPermission);

        var allowed = await service.AuthorizePermissionOnResourcesAsync(
            UserId,
            OuScopedPermission,
            [RootA, ChildA],
            CancellationToken.None);
        Assert.True(allowed.IsAllowed);

        var denied = await service.AuthorizePermissionOnResourcesAsync(
            UserId,
            OuScopedPermission,
            [RootA, SiblingB],
            CancellationToken.None);
        Assert.False(denied.IsAllowed);
        Assert.Equal(AuthorizationDecisionReason.OutsideUnitScope, denied.Reason);
    }

    [Fact]
    public async Task AuthorizeForCreate_RequiresAccessibleOrganizationUnit()
    {
        var service = CreateService(
            [RootA],
            new Dictionary<string, PermissionScopeMode>
            {
                [OuScopedPermission] = PermissionScopeMode.OrganizationUnit
            },
            OuScopedPermission);

        Assert.True((await service.AuthorizePermissionForCreateAsync(
            UserId, OuScopedPermission, RootA, CancellationToken.None)).IsAllowed);
        Assert.False((await service.AuthorizePermissionForCreateAsync(
            UserId, OuScopedPermission, SiblingB, CancellationToken.None)).IsAllowed);
        Assert.False((await service.AuthorizePermissionForCreateAsync(
            UserId, OuScopedPermission, Guid.Empty, CancellationToken.None)).IsAllowed);
    }

    [Fact]
    public void ApplyOrganizationUnitFilter_KeepsOnlyInScopeRows()
    {
        var service = new AuthorizationScopeService(new FakeDecisionService(
            new UserAuthorizationContext(
                UserId,
                [],
                [],
                [RootA, ChildA],
                UserAuthorizationContext.EmptyPermissionScopes)));

        var rows = new[]
        {
            new ScopedRow(RootA),
            new ScopedRow(ChildA),
            new ScopedRow(SiblingB)
        }.AsQueryable();

        var filtered = service.ApplyOrganizationUnitFilter(rows, [RootA, ChildA]).ToArray();

        Assert.Equal(2, filtered.Length);
        Assert.DoesNotContain(filtered, row => row.OrganizationUnitId == SiblingB);
    }

    [Fact]
    public void ApplyOrganizationUnitFilter_EmptyAccessible_ReturnsNoRows()
    {
        var service = new AuthorizationScopeService(new FakeDecisionService(null));
        var rows = new[] { new ScopedRow(RootA) }.AsQueryable();

        var filtered = service.ApplyOrganizationUnitFilter(rows, []).ToArray();

        Assert.Empty(filtered);
    }

    [Fact]
    public async Task ListAccessibleOrganizationUnits_FailClosed_WhenNoAccessibleUnits()
    {
        var store = new FakeScopedAdminStore([]);
        var scope = CreateService(
            accessibleUnitIds: [],
            scopes: UserAuthorizationContext.EmptyPermissionScopes);
        var handler = new ListAccessibleOrganizationUnits(store, scope);

        var result = await handler.HandleAsync(
            UserId,
            PageRequest.Create(1, 20),
            isActive: true,
            CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.False(store.ListByIdsCalled);
    }

    [Fact]
    public async Task ListAccessibleOrganizationUnits_FiltersToAccessibleIds()
    {
        var now = new DateTimeOffset(2026, 8, 27, 0, 0, 0, TimeSpan.Zero);
        var root = OrganizationUnit.CreateRoot("Root", "ROOT", now);
        var other = OrganizationUnit.CreateRoot("Other", "OTHER", now);
        var store = new FakeScopedAdminStore([root, other]);
        var scope = CreateService(
            [root.Id],
            UserAuthorizationContext.EmptyPermissionScopes);
        var handler = new ListAccessibleOrganizationUnits(store, scope);

        var result = await handler.HandleAsync(
            UserId,
            PageRequest.Create(1, 20),
            isActive: null,
            CancellationToken.None);

        Assert.True(store.ListByIdsCalled);
        Assert.Single(result.Items);
        Assert.Equal(root.Id, result.Items[0].Id);
    }

    private static AuthorizationScopeService CreateService(
        IReadOnlyList<Guid> accessibleUnitIds,
        IReadOnlyDictionary<string, PermissionScopeMode> scopes,
        params string[] grantedPermissionCodes)
    {
        var context = new UserAuthorizationContext(
            UserId,
            ["editors"],
            grantedPermissionCodes,
            accessibleUnitIds,
            scopes);

        return new AuthorizationScopeService(new FakeDecisionService(context));
    }

    private sealed record ScopedRow(Guid OrganizationUnitId) : IOrganizationUnitScoped;

    private sealed class FakeDecisionService(UserAuthorizationContext? context)
        : IAuthorizationDecisionService
    {
        public Task<UserAuthorizationContext?> GetContextAsync(
            Guid userId,
            CancellationToken cancellationToken) =>
            Task.FromResult(context);

        public Task<AuthorizationDecision> HasPermissionAsync(
            Guid userId,
            string permissionCode,
            CancellationToken cancellationToken)
        {
            if (context is null)
            {
                return Task.FromResult(AuthorizationDecision.Unauthenticated());
            }

            return Task.FromResult(
                PermissionMatcher.Grants(context.PermissionCodes, permissionCode)
                    ? AuthorizationDecision.Allowed()
                    : AuthorizationDecision.MissingPermission());
        }

        public async Task<AuthorizationDecision> HasPermissionOnUnitAsync(
            Guid userId,
            string permissionCode,
            Guid organizationUnitId,
            CancellationToken cancellationToken)
        {
            var permission = await HasPermissionAsync(userId, permissionCode, cancellationToken);
            if (!permission.IsAllowed)
            {
                return permission;
            }

            var canAccess = await CanAccessOrganizationUnitAsync(
                userId,
                organizationUnitId,
                cancellationToken);
            return canAccess
                ? AuthorizationDecision.Allowed()
                : AuthorizationDecision.OutsideUnitScope();
        }

        public Task<bool> CanAccessOrganizationUnitAsync(
            Guid userId,
            Guid organizationUnitId,
            CancellationToken cancellationToken) =>
            Task.FromResult(
                context?.AccessibleOrganizationUnitIds.Contains(organizationUnitId) == true);
    }

    private sealed class FakeScopedAdminStore(IReadOnlyList<OrganizationUnit> units)
        : IAuthorizationAdminStore
    {
        public bool ListByIdsCalled { get; private set; }

        public Task<PageResult<OrganizationUnit>> ListOrganizationUnitsByIdsAsync(
            PageRequest page,
            IReadOnlyCollection<Guid> organizationUnitIds,
            bool? isActive,
            CancellationToken cancellationToken)
        {
            ListByIdsCalled = true;
            var idSet = organizationUnitIds.ToHashSet();
            var filtered = units.Where(unit => idSet.Contains(unit.Id)).ToArray();
            return Task.FromResult(
                new PageResult<OrganizationUnit>(
                    filtered,
                    filtered.Length,
                    page.Page,
                    page.PageSize));
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
