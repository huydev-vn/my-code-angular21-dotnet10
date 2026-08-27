using Application.Common.Results;
using Domain.Authorization;

namespace Application.Features.Authorization.Abstractions;

/// <summary>
/// Grant + OU containment for delegated (non-privileged) authorization admins.
/// Privileged actors bypass containment and retain full admin authority.
/// </summary>
public interface IDelegationAuthorityService
{
    /// <summary>
    /// Ensures the actor may assign or revoke <paramref name="permission"/> on a group.
    /// Privileged: allowed. Non-privileged: must hold the permission, and it must not be
    /// privileged/Critical; revoke uses the same rule for symmetry.
    /// </summary>
    Task<Result?> EnsureCanDelegatePermissionAsync(
        Guid? actorUserId,
        PermissionDefinition permission,
        CancellationToken cancellationToken);

    /// <summary>
    /// Ensures the actor may attach or revoke a group→OU root (or user↔OU membership target).
    /// Privileged: allowed when the unit exists/active (caller still validates that).
    /// Non-privileged: <paramref name="organizationUnitId"/> must be in the actor's accessible set
    /// (fail closed when the set is empty).
    /// </summary>
    Task<Result?> EnsureCanAssignOrganizationUnitScopeAsync(
        Guid? actorUserId,
        Guid organizationUnitId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Ensures the actor may assign/revoke users on <paramref name="group"/>.
    /// Privileged groups: privileged actors only (also enforced by <c>PrivilegedGroupGuard</c>).
    /// Non-privileged: group must have at least one OU root and every root must lie inside
    /// the actor's accessible OU set.
    /// </summary>
    Task<Result?> EnsureCanManageGroupUserAssignmentAsync(
        Guid? actorUserId,
        UserGroup group,
        CancellationToken cancellationToken);
}
