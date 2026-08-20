using Application.Features.Authorization.Abstractions;
using Domain.Authorization;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Authorization;

internal sealed class AuthorizationDecisionService(AppDbContext dbContext)
    : IAuthorizationDecisionService
{
    private Guid? _cachedUserId;
    private UserAuthorizationContext? _cachedContext;
    private bool _cachedMissingUser;

    public async Task<UserAuthorizationContext?> GetContextAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (_cachedUserId == userId)
        {
            return _cachedMissingUser ? null : _cachedContext;
        }

        var context = await LoadContextAsync(userId, cancellationToken);
        _cachedUserId = userId;
        _cachedMissingUser = context is null;
        _cachedContext = context;
        return context;
    }

    public async Task<AuthorizationDecision> HasPermissionAsync(
        Guid userId,
        string permissionCode,
        CancellationToken cancellationToken)
    {
        var context = await GetContextAsync(userId, cancellationToken);
        if (context is null)
        {
            return AuthorizationDecision.Unauthenticated();
        }

        return PermissionMatcher.Grants(context.PermissionCodes, permissionCode)
            ? AuthorizationDecision.Allowed()
            : AuthorizationDecision.MissingPermission();
    }

    public async Task<AuthorizationDecision> HasPermissionOnUnitAsync(
        Guid userId,
        string permissionCode,
        Guid organizationUnitId,
        CancellationToken cancellationToken)
    {
        var permissionDecision = await HasPermissionAsync(
            userId,
            permissionCode,
            cancellationToken);

        if (!permissionDecision.IsAllowed)
        {
            return permissionDecision;
        }

        var canAccessUnit = await CanAccessOrganizationUnitAsync(
            userId,
            organizationUnitId,
            cancellationToken);

        return canAccessUnit
            ? AuthorizationDecision.Allowed()
            : AuthorizationDecision.OutsideUnitScope();
    }

    public async Task<bool> CanAccessOrganizationUnitAsync(
        Guid userId,
        Guid organizationUnitId,
        CancellationToken cancellationToken)
    {
        var context = await GetContextAsync(userId, cancellationToken);
        return context?.AccessibleOrganizationUnitIds.Contains(organizationUnitId) == true;
    }

    private async Task<UserAuthorizationContext?> LoadContextAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var userExists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == userId, cancellationToken);

        if (!userExists)
        {
            return null;
        }

        var memberships = await dbContext.UserGroupMemberships
            .AsNoTracking()
            .Where(membership => membership.UserId == userId)
            .Select(membership => membership.GroupId)
            .ToListAsync(cancellationToken);

        if (memberships.Count == 0)
        {
            return new UserAuthorizationContext(userId, [], [], []);
        }

        var groups = await dbContext.UserGroups
            .AsNoTracking()
            .Where(group => memberships.Contains(group.Id) && group.IsActive)
            .Select(group => new { group.Id, group.Name })
            .ToListAsync(cancellationToken);

        var activeGroupIds = groups.Select(group => group.Id).ToArray();
        if (activeGroupIds.Length == 0)
        {
            return new UserAuthorizationContext(userId, [], [], []);
        }

        var permissions = await dbContext.GroupPermissions
            .AsNoTracking()
            .Where(assignment => activeGroupIds.Contains(assignment.GroupId))
            .Join(
                dbContext.PermissionDefinitions.AsNoTracking()
                    .Where(permission => permission.IsActive),
                assignment => assignment.PermissionId,
                permission => permission.Id,
                (_, permission) => permission.Code)
            .Distinct()
            .OrderBy(code => code)
            .ToListAsync(cancellationToken);

        var scopeRoots = await dbContext.GroupOrganizationUnits
            .AsNoTracking()
            .Where(assignment => activeGroupIds.Contains(assignment.GroupId))
            .Select(assignment => assignment.OrganizationUnitId)
            .Distinct()
            .ToListAsync(cancellationToken);

        IReadOnlyList<Guid> accessibleUnitIds = [];
        if (scopeRoots.Count > 0)
        {
            accessibleUnitIds = await OrganizationUnitQueries.CollectAccessibleIdsAsync(
                dbContext,
                scopeRoots,
                activeOnly: true,
                cancellationToken);
        }

        return new UserAuthorizationContext(
            userId,
            groups.Select(group => group.Name).OrderBy(name => name).ToArray(),
            permissions,
            accessibleUnitIds);
    }
}
