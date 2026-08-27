using Domain.Authorization;

namespace Application.Features.Authorization.Abstractions;

/// <summary>
/// Fail-closed organization-unit scope enforcement for list/get/mutate/create/bulk flows.
/// Permission grants still come from <see cref="IAuthorizationDecisionService"/>;
/// this service applies catalog <see cref="PermissionScopeMode"/> on top.
/// </summary>
public interface IAuthorizationScopeService
{
    /// <summary>Returns the caller's accessible OU ids (active roots + descendants). Empty when none.</summary>
    Task<IReadOnlyCollection<Guid>> GetAccessibleOrganizationUnitIdsAsync(
        Guid userId,
        CancellationToken cancellationToken);

    /// <summary>True only when the OU is in the accessible set. Missing context / unknown OU → false.</summary>
    Task<bool> CanAccessOrganizationUnitAsync(
        Guid userId,
        Guid organizationUnitId,
        CancellationToken cancellationToken);

    /// <summary>Same as <see cref="CanAccessOrganizationUnitAsync"/> but returns an <see cref="AuthorizationDecision"/>.</summary>
    Task<AuthorizationDecision> EnsureCanAccessOrganizationUnitAsync(
        Guid userId,
        Guid organizationUnitId,
        CancellationToken cancellationToken);

    /// <summary>
    /// SQL-translatable filter: keeps rows whose <see cref="IOrganizationUnitScoped.OrganizationUnitId"/>
    /// is in <paramref name="accessibleUnitIds"/>. Empty accessible set → no rows (fail closed).
    /// </summary>
    IQueryable<T> ApplyOrganizationUnitFilter<T>(
        IQueryable<T> query,
        IReadOnlyCollection<Guid> accessibleUnitIds)
        where T : IOrganizationUnitScoped;

    /// <summary>
    /// Authorizes a permission against a resource's OU.
    /// Global/None: permission grant only. OrganizationUnit: grant + resource OU in accessible set.
    /// Bulk callers should use <see cref="AuthorizePermissionOnResourcesAsync"/> (all-or-nothing).
    /// </summary>
    Task<AuthorizationDecision> AuthorizePermissionOnResourceAsync(
        Guid userId,
        string permissionCode,
        Guid resourceOrganizationUnitId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Validates create payloads: when ScopeMode is OrganizationUnit, the requested OU must be accessible.
    /// Global/None skip the OU check after the permission grant succeeds.
    /// </summary>
    Task<AuthorizationDecision> AuthorizePermissionForCreateAsync(
        Guid userId,
        string permissionCode,
        Guid requestedOrganizationUnitId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Bulk authorization: all-or-nothing. Any item outside scope (or empty Guid) denies the whole batch.
    /// Empty <paramref name="resourceOrganizationUnitIds"/> denies when ScopeMode is OrganizationUnit.
    /// </summary>
    Task<AuthorizationDecision> AuthorizePermissionOnResourcesAsync(
        Guid userId,
        string permissionCode,
        IReadOnlyCollection<Guid> resourceOrganizationUnitIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// Used by <c>[RequirePermissionOnUnit]</c>: Global/None succeed on grant alone (OU ignored);
    /// OrganizationUnit requires a present route/query OU inside the accessible set.
    /// </summary>
    Task<AuthorizationDecision> AuthorizePermissionWithOptionalUnitAsync(
        Guid userId,
        string permissionCode,
        Guid? organizationUnitId,
        CancellationToken cancellationToken);
}
