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

    Task<IReadOnlyList<PermissionDefinition>> ListPermissionsAsync(
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

    Task<IReadOnlyList<UserGroup>> ListGroupsAsync(
        CancellationToken cancellationToken);

    Task AddGroupAsync(UserGroup group, CancellationToken cancellationToken);

    Task<OrganizationUnit?> FindOrganizationUnitByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<OrganizationUnit?> FindOrganizationUnitByCodeAsync(
        string code,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<OrganizationUnit>> ListOrganizationUnitsAsync(
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

    Task<bool> UserGroupMembershipExistsAsync(
        Guid userId,
        Guid groupId,
        CancellationToken cancellationToken);

    Task AddUserGroupMembershipAsync(
        UserGroupMembership membership,
        CancellationToken cancellationToken);

    Task<bool> GroupOrganizationUnitExistsAsync(
        Guid groupId,
        Guid organizationUnitId,
        CancellationToken cancellationToken);

    Task AddGroupOrganizationUnitAsync(
        GroupOrganizationUnit assignment,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> GetDescendantOrganizationUnitIdsAsync(
        Guid rootOrganizationUnitId,
        CancellationToken cancellationToken);

    Task<bool> WouldCreateOrganizationUnitCycleAsync(
        Guid organizationUnitId,
        Guid? newParentId,
        CancellationToken cancellationToken);
}
