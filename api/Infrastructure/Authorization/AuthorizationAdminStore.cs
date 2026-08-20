using Application.Features.Authorization.Abstractions;
using Domain.Authorization;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Authorization;

internal sealed class AuthorizationAdminStore(AppDbContext dbContext) : IAuthorizationAdminStore
{
    public Task<PermissionDefinition?> FindPermissionByIdAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        dbContext.PermissionDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(permission => permission.Id == id, cancellationToken);

    public Task<PermissionDefinition?> FindPermissionByCodeAsync(
        string code,
        CancellationToken cancellationToken) =>
        dbContext.PermissionDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                permission => permission.Code == code,
                cancellationToken);

    public async Task<IReadOnlyList<PermissionDefinition>> ListPermissionsAsync(
        CancellationToken cancellationToken) =>
        await dbContext.PermissionDefinitions
            .AsNoTracking()
            .OrderBy(permission => permission.Code)
            .ToListAsync(cancellationToken);

    public Task AddPermissionAsync(
        PermissionDefinition permission,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        dbContext.PermissionDefinitions.Add(permission);
        return Task.CompletedTask;
    }

    public Task<UserGroup?> FindGroupByIdAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        dbContext.UserGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(group => group.Id == id, cancellationToken);

    public Task<UserGroup?> FindGroupByNameAsync(
        string name,
        CancellationToken cancellationToken) =>
        dbContext.UserGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(group => group.Name == name, cancellationToken);

    public async Task<IReadOnlyList<UserGroup>> ListGroupsAsync(
        CancellationToken cancellationToken) =>
        await dbContext.UserGroups
            .AsNoTracking()
            .OrderBy(group => group.Name)
            .ToListAsync(cancellationToken);

    public Task AddGroupAsync(UserGroup group, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        dbContext.UserGroups.Add(group);
        return Task.CompletedTask;
    }

    public Task<OrganizationUnit?> FindOrganizationUnitByIdAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        dbContext.OrganizationUnits
            .AsNoTracking()
            .FirstOrDefaultAsync(unit => unit.Id == id, cancellationToken);

    public Task<OrganizationUnit?> FindOrganizationUnitByCodeAsync(
        string code,
        CancellationToken cancellationToken) =>
        dbContext.OrganizationUnits
            .AsNoTracking()
            .FirstOrDefaultAsync(unit => unit.Code == code, cancellationToken);

    public async Task<IReadOnlyList<OrganizationUnit>> ListOrganizationUnitsAsync(
        CancellationToken cancellationToken) =>
        await dbContext.OrganizationUnits
            .AsNoTracking()
            .OrderBy(unit => unit.Name)
            .ToListAsync(cancellationToken);

    public Task AddOrganizationUnitAsync(
        OrganizationUnit unit,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        dbContext.OrganizationUnits.Add(unit);
        return Task.CompletedTask;
    }

    public Task<bool> GroupPermissionExistsAsync(
        Guid groupId,
        Guid permissionId,
        CancellationToken cancellationToken) =>
        dbContext.GroupPermissions
            .AsNoTracking()
            .AnyAsync(
                assignment =>
                    assignment.GroupId == groupId &&
                    assignment.PermissionId == permissionId,
                cancellationToken);

    public Task AddGroupPermissionAsync(
        GroupPermission assignment,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        dbContext.GroupPermissions.Add(assignment);
        return Task.CompletedTask;
    }

    public Task<bool> UserGroupMembershipExistsAsync(
        Guid userId,
        Guid groupId,
        CancellationToken cancellationToken) =>
        dbContext.UserGroupMemberships
            .AsNoTracking()
            .AnyAsync(
                membership =>
                    membership.UserId == userId &&
                    membership.GroupId == groupId,
                cancellationToken);

    public Task AddUserGroupMembershipAsync(
        UserGroupMembership membership,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        dbContext.UserGroupMemberships.Add(membership);
        return Task.CompletedTask;
    }

    public Task<bool> GroupOrganizationUnitExistsAsync(
        Guid groupId,
        Guid organizationUnitId,
        CancellationToken cancellationToken) =>
        dbContext.GroupOrganizationUnits
            .AsNoTracking()
            .AnyAsync(
                assignment =>
                    assignment.GroupId == groupId &&
                    assignment.OrganizationUnitId == organizationUnitId,
                cancellationToken);

    public Task AddGroupOrganizationUnitAsync(
        GroupOrganizationUnit assignment,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        dbContext.GroupOrganizationUnits.Add(assignment);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<Guid>> GetDescendantOrganizationUnitIdsAsync(
        Guid rootOrganizationUnitId,
        CancellationToken cancellationToken)
    {
        var units = await dbContext.OrganizationUnits
            .AsNoTracking()
            .Select(unit => new { unit.Id, unit.ParentId })
            .ToListAsync(cancellationToken);

        return OrganizationUnitHierarchy.CollectAccessibleIds(
            [rootOrganizationUnitId],
            units.Select(unit => (unit.Id, unit.ParentId)).ToArray());
    }

    public async Task<bool> WouldCreateOrganizationUnitCycleAsync(
        Guid organizationUnitId,
        Guid? newParentId,
        CancellationToken cancellationToken)
    {
        var units = await dbContext.OrganizationUnits
            .AsNoTracking()
            .Select(unit => new { unit.Id, unit.ParentId })
            .ToListAsync(cancellationToken);

        var parentById = units.ToDictionary(unit => unit.Id, unit => unit.ParentId);
        return OrganizationUnitHierarchy.WouldCreateCycle(
            organizationUnitId,
            newParentId,
            parentById);
    }
}
