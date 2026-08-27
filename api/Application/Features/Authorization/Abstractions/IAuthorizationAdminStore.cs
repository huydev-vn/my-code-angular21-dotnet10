using Application.Common.Pagination;
using Domain.Authorization;

namespace Application.Features.Authorization.Abstractions;

public interface IAuthorizationAdminStore
{
    Task<PermissionDefinition?> FindPermissionByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<PermissionDefinition?> FindPermissionByCodeAsync(
        string code,
        CancellationToken cancellationToken);

    Task<PageResult<PermissionDefinition>> ListPermissionsAsync(
        PageRequest page,
        bool? isActive,
        CancellationToken cancellationToken);

    Task AddPermissionAsync(
        PermissionDefinition permission,
        CancellationToken cancellationToken);

    Task<UserGroup?> FindGroupByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<UserGroup?> FindGroupByNameAsync(
        string name,
        CancellationToken cancellationToken);

    Task<PageResult<UserGroup>> ListGroupsAsync(
        PageRequest page,
        bool? isActive,
        CancellationToken cancellationToken);

    Task AddGroupAsync(UserGroup group, CancellationToken cancellationToken);

    Task<OrganizationUnit?> FindOrganizationUnitByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<OrganizationUnit?> FindOrganizationUnitByCodeAsync(
        string code,
        CancellationToken cancellationToken);

    Task<PageResult<OrganizationUnit>> ListOrganizationUnitsAsync(
        PageRequest page,
        bool? isActive,
        CancellationToken cancellationToken);

    /// <summary>
    /// Paged OU list restricted to the given ids (Agent B accessible-set listing).
    /// Empty <paramref name="organizationUnitIds"/> returns an empty page without querying.
    /// </summary>
    Task<PageResult<OrganizationUnit>> ListOrganizationUnitsByIdsAsync(
        PageRequest page,
        IReadOnlyCollection<Guid> organizationUnitIds,
        bool? isActive,
        CancellationToken cancellationToken);

    Task AddOrganizationUnitAsync(
        OrganizationUnit unit,
        CancellationToken cancellationToken);

    Task<bool> GroupPermissionExistsAsync(
        Guid groupId,
        Guid permissionId,
        CancellationToken cancellationToken);

    Task AddGroupPermissionAsync(
        GroupPermission assignment,
        CancellationToken cancellationToken);

    Task<bool> RemoveGroupPermissionAsync(
        Guid groupId,
        Guid permissionId,
        CancellationToken cancellationToken);

    Task<bool> UserGroupMembershipExistsAsync(
        Guid userId,
        Guid groupId,
        CancellationToken cancellationToken);

    Task<bool> IsMemberOfAnyPrivilegedGroupAsync(
        Guid userId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Counts active memberships in a privileged group (group must be privileged and active).
    /// </summary>
    Task<int> CountActiveMembersInGroupAsync(
        Guid groupId,
        CancellationToken cancellationToken);

    Task AddUserGroupMembershipAsync(
        UserGroupMembership membership,
        CancellationToken cancellationToken);

    /// <summary>
    /// Atomically removes a membership. For active privileged groups, serializes
    /// on the group row and refuses when removal would leave zero members.
    /// </summary>
    Task<MembershipRemoval> TryRemoveUserGroupMembershipAsync(
        Guid userId,
        Guid groupId,
        CancellationToken cancellationToken);

    Task<bool> GroupOrganizationUnitExistsAsync(
        Guid groupId,
        Guid organizationUnitId,
        CancellationToken cancellationToken);

    Task AddGroupOrganizationUnitAsync(
        GroupOrganizationUnit assignment,
        CancellationToken cancellationToken);

    Task<bool> RemoveGroupOrganizationUnitAsync(
        Guid groupId,
        Guid organizationUnitId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns organization-unit root ids currently attached to the group (group→OU scope).
    /// Empty when the group has no OU scope (global-style group).
    /// </summary>
    Task<IReadOnlyList<Guid>> ListGroupOrganizationUnitIdsAsync(
        Guid groupId,
        CancellationToken cancellationToken);

    // Agent C — user↔OU membership (organizational metadata; does not grant data access)
    Task<UserOrganizationUnit?> FindUserOrganizationUnitAsync(
        Guid userId,
        Guid organizationUnitId,
        CancellationToken cancellationToken);

    Task<UserOrganizationUnit?> FindActivePrimaryUserOrganizationUnitAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task AddUserOrganizationUnitAsync(
        UserOrganizationUnit membership,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<UserOrganizationUnit>> ListUserOrganizationUnitsAsync(
        Guid userId,
        bool activeOnly,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PermissionDefinition>> ListActivePermissionsByCodesAsync(
        IReadOnlyCollection<string> codes,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> GetDescendantOrganizationUnitIdsAsync(
        Guid rootOrganizationUnitId,
        CancellationToken cancellationToken);

    Task<bool> WouldCreateOrganizationUnitCycleAsync(
        Guid organizationUnitId,
        Guid? newParentId,
        CancellationToken cancellationToken);

    Task<PageResult<AuthorizationAuditEvent>> ListAuditEventsAsync(
        PageRequest page,
        string? action,
        Guid? actorUserId,
        CancellationToken cancellationToken);
}
