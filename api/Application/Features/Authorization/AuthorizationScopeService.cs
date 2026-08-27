using Application.Features.Authorization.Abstractions;
using Domain.Authorization;

namespace Application.Features.Authorization;

/// <summary>
/// Fail-closed organization-unit scope enforcement. Never trusts client-supplied OU alone —
/// always intersects with the caller's accessible set from <see cref="IAuthorizationDecisionService"/>.
/// </summary>
public sealed class AuthorizationScopeService(IAuthorizationDecisionService decisionService)
    : IAuthorizationScopeService
{
    public async Task<IReadOnlyCollection<Guid>> GetAccessibleOrganizationUnitIdsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var context = await decisionService.GetContextAsync(userId, cancellationToken);
        if (context is null)
        {
            return [];
        }

        return context.AccessibleOrganizationUnitIds;
    }

    public Task<bool> CanAccessOrganizationUnitAsync(
        Guid userId,
        Guid organizationUnitId,
        CancellationToken cancellationToken) =>
        decisionService.CanAccessOrganizationUnitAsync(
            userId,
            organizationUnitId,
            cancellationToken);

    public async Task<AuthorizationDecision> EnsureCanAccessOrganizationUnitAsync(
        Guid userId,
        Guid organizationUnitId,
        CancellationToken cancellationToken)
    {
        if (organizationUnitId == Guid.Empty)
        {
            return AuthorizationDecision.OutsideUnitScope();
        }

        var context = await decisionService.GetContextAsync(userId, cancellationToken);
        if (context is null)
        {
            return AuthorizationDecision.Unauthenticated();
        }

        return context.AccessibleOrganizationUnitIds.Contains(organizationUnitId)
            ? AuthorizationDecision.Allowed()
            : AuthorizationDecision.OutsideUnitScope();
    }

    public IQueryable<T> ApplyOrganizationUnitFilter<T>(
        IQueryable<T> query,
        IReadOnlyCollection<Guid> accessibleUnitIds)
        where T : IOrganizationUnitScoped
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(accessibleUnitIds);

        if (accessibleUnitIds.Count == 0)
        {
            return query.Where(_ => false);
        }

        var ids = accessibleUnitIds as Guid[] ?? accessibleUnitIds.ToArray();
        return query.Where(entity => ids.Contains(entity.OrganizationUnitId));
    }

    public Task<AuthorizationDecision> AuthorizePermissionOnResourceAsync(
        Guid userId,
        string permissionCode,
        Guid resourceOrganizationUnitId,
        CancellationToken cancellationToken) =>
        AuthorizeCoreAsync(
            userId,
            permissionCode,
            resourceOrganizationUnitId,
            requireOrganizationUnitWhenScoped: true,
            cancellationToken);

    public Task<AuthorizationDecision> AuthorizePermissionForCreateAsync(
        Guid userId,
        string permissionCode,
        Guid requestedOrganizationUnitId,
        CancellationToken cancellationToken) =>
        AuthorizePermissionOnResourceAsync(
            userId,
            permissionCode,
            requestedOrganizationUnitId,
            cancellationToken);

    public async Task<AuthorizationDecision> AuthorizePermissionOnResourcesAsync(
        Guid userId,
        string permissionCode,
        IReadOnlyCollection<Guid> resourceOrganizationUnitIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resourceOrganizationUnitIds);

        var context = await decisionService.GetContextAsync(userId, cancellationToken);
        if (context is null)
        {
            return AuthorizationDecision.Unauthenticated();
        }

        context = context.WithNormalizedScopes();

        if (!PermissionMatcher.Grants(context.PermissionCodes, permissionCode))
        {
            return AuthorizationDecision.MissingPermission();
        }

        if (!context.TryGetPermissionScopeMode(permissionCode, out var scopeMode))
        {
            // Granted code without catalog metadata — fail closed.
            return AuthorizationDecision.MissingPermission();
        }

        if (scopeMode is PermissionScopeMode.Global or PermissionScopeMode.None)
        {
            return AuthorizationDecision.Allowed();
        }

        // ScopeMode.OrganizationUnit — all-or-nothing.
        if (resourceOrganizationUnitIds.Count == 0)
        {
            return AuthorizationDecision.OutsideUnitScope();
        }

        if (context.AccessibleOrganizationUnitIds.Count == 0)
        {
            return AuthorizationDecision.OutsideUnitScope();
        }

        var accessible = context.AccessibleOrganizationUnitIds as HashSet<Guid>
            ?? context.AccessibleOrganizationUnitIds.ToHashSet();

        foreach (var unitId in resourceOrganizationUnitIds)
        {
            if (unitId == Guid.Empty || !accessible.Contains(unitId))
            {
                return AuthorizationDecision.OutsideUnitScope();
            }
        }

        return AuthorizationDecision.Allowed();
    }

    public Task<AuthorizationDecision> AuthorizePermissionWithOptionalUnitAsync(
        Guid userId,
        string permissionCode,
        Guid? organizationUnitId,
        CancellationToken cancellationToken) =>
        AuthorizeCoreAsync(
            userId,
            permissionCode,
            organizationUnitId,
            requireOrganizationUnitWhenScoped: true,
            cancellationToken);

    private async Task<AuthorizationDecision> AuthorizeCoreAsync(
        Guid userId,
        string permissionCode,
        Guid? organizationUnitId,
        bool requireOrganizationUnitWhenScoped,
        CancellationToken cancellationToken)
    {
        var context = await decisionService.GetContextAsync(userId, cancellationToken);
        if (context is null)
        {
            return AuthorizationDecision.Unauthenticated();
        }

        context = context.WithNormalizedScopes();

        if (!PermissionMatcher.Grants(context.PermissionCodes, permissionCode))
        {
            return AuthorizationDecision.MissingPermission();
        }

        if (!context.TryGetPermissionScopeMode(permissionCode, out var scopeMode))
        {
            return AuthorizationDecision.MissingPermission();
        }

        if (scopeMode is PermissionScopeMode.Global or PermissionScopeMode.None)
        {
            return AuthorizationDecision.Allowed();
        }

        // PermissionScopeMode.OrganizationUnit
        if (requireOrganizationUnitWhenScoped && organizationUnitId is null)
        {
            return AuthorizationDecision.OutsideUnitScope();
        }

        if (organizationUnitId is null || organizationUnitId.Value == Guid.Empty)
        {
            return AuthorizationDecision.OutsideUnitScope();
        }

        if (context.AccessibleOrganizationUnitIds.Count == 0)
        {
            return AuthorizationDecision.OutsideUnitScope();
        }

        return context.AccessibleOrganizationUnitIds.Contains(organizationUnitId.Value)
            ? AuthorizationDecision.Allowed()
            : AuthorizationDecision.OutsideUnitScope();
    }
}
