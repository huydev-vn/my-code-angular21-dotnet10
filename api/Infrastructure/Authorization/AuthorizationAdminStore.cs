using Application.Common.Pagination;
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
            .FirstOrDefaultAsync(permission => permission.Id == id, cancellationToken);

    public Task<PermissionDefinition?> FindPermissionByCodeAsync(
        string code,
        CancellationToken cancellationToken) =>
        dbContext.PermissionDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                permission => permission.Code == code,
                cancellationToken);

    public async Task<PageResult<PermissionDefinition>> ListPermissionsAsync(
        PageRequest page,
        bool? isActive,
        CancellationToken cancellationToken)
    {
        var query = dbContext.PermissionDefinitions.AsNoTracking();
        if (isActive is not null)
        {
            query = query.Where(permission => permission.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(permission => permission.Code)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return new PageResult<PermissionDefinition>(
            items,
            totalCount,
            page.Page,
            page.PageSize);
    }

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
            .FirstOrDefaultAsync(group => group.Id == id, cancellationToken);

    public Task<UserGroup?> FindGroupByNameAsync(
        string name,
        CancellationToken cancellationToken) =>
        dbContext.UserGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(group => group.Name == name, cancellationToken);

    public async Task<PageResult<UserGroup>> ListGroupsAsync(
        PageRequest page,
        bool? isActive,
        CancellationToken cancellationToken)
    {
        var query = dbContext.UserGroups.AsNoTracking();
        if (isActive is not null)
        {
            query = query.Where(group => group.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(group => group.Name)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return new PageResult<UserGroup>(items, totalCount, page.Page, page.PageSize);
    }

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
            .FirstOrDefaultAsync(unit => unit.Id == id, cancellationToken);

    public Task<OrganizationUnit?> FindOrganizationUnitByCodeAsync(
        string code,
        CancellationToken cancellationToken) =>
        dbContext.OrganizationUnits
            .AsNoTracking()
            .FirstOrDefaultAsync(unit => unit.Code == code, cancellationToken);

    public async Task<PageResult<OrganizationUnit>> ListOrganizationUnitsAsync(
        PageRequest page,
        bool? isActive,
        CancellationToken cancellationToken)
    {
        var query = dbContext.OrganizationUnits.AsNoTracking();
        if (isActive is not null)
        {
            query = query.Where(unit => unit.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(unit => unit.Name)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return new PageResult<OrganizationUnit>(
            items,
            totalCount,
            page.Page,
            page.PageSize);
    }

    public async Task<PageResult<OrganizationUnit>> ListOrganizationUnitsByIdsAsync(
        PageRequest page,
        IReadOnlyCollection<Guid> organizationUnitIds,
        bool? isActive,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(organizationUnitIds);

        if (organizationUnitIds.Count == 0)
        {
            return new PageResult<OrganizationUnit>(
                [],
                0,
                page.Page,
                page.PageSize);
        }

        var ids = organizationUnitIds as Guid[] ?? organizationUnitIds.ToArray();
        var query = dbContext.OrganizationUnits
            .AsNoTracking()
            .Where(unit => ids.Contains(unit.Id));

        if (isActive is not null)
        {
            query = query.Where(unit => unit.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(unit => unit.Name)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return new PageResult<OrganizationUnit>(
            items,
            totalCount,
            page.Page,
            page.PageSize);
    }

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

    public async Task<bool> RemoveGroupPermissionAsync(
        Guid groupId,
        Guid permissionId,
        CancellationToken cancellationToken)
    {
        var assignment = await dbContext.GroupPermissions
            .FirstOrDefaultAsync(
                entry =>
                    entry.GroupId == groupId &&
                    entry.PermissionId == permissionId,
                cancellationToken);
        if (assignment is null)
        {
            return false;
        }

        dbContext.GroupPermissions.Remove(assignment);
        return true;
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

    public Task<bool> IsMemberOfAnyPrivilegedGroupAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        dbContext.UserGroupMemberships
            .AsNoTracking()
            .AnyAsync(
                membership =>
                    membership.UserId == userId &&
                    dbContext.UserGroups.Any(group =>
                        group.Id == membership.GroupId &&
                        group.IsPrivileged &&
                        group.IsActive),
                cancellationToken);

    public Task<int> CountActiveMembersInGroupAsync(
        Guid groupId,
        CancellationToken cancellationToken) =>
        dbContext.UserGroupMemberships
            .AsNoTracking()
            .CountAsync(
                membership => membership.GroupId == groupId,
                cancellationToken);

    public Task AddUserGroupMembershipAsync(
        UserGroupMembership membership,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        dbContext.UserGroupMemberships.Add(membership);
        return Task.CompletedTask;
    }

    public async Task<MembershipRemoval> TryRemoveUserGroupMembershipAsync(
        Guid userId,
        Guid groupId,
        CancellationToken cancellationToken)
    {
        // Caller must own an open transaction so FOR UPDATE is held through SaveChanges.
        if (dbContext.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "TryRemoveUserGroupMembershipAsync requires an open database transaction.");
        }

        // Serialize membership changes for this group so concurrent revokes cannot
        // both observe count > 1 and remove the last privileged members.
        await dbContext.Database.ExecuteSqlAsync(
            $"SELECT 1 FROM \"UserGroups\" WHERE \"Id\" = {groupId} FOR UPDATE",
            cancellationToken);

        var group = await dbContext.UserGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == groupId, cancellationToken);
        if (group is null)
        {
            return MembershipRemoval.NotFound;
        }

        var membership = await dbContext.UserGroupMemberships
            .FirstOrDefaultAsync(
                entry => entry.UserId == userId && entry.GroupId == groupId,
                cancellationToken);
        if (membership is null)
        {
            return MembershipRemoval.NotFound;
        }

        if (group.IsPrivileged && group.IsActive)
        {
            var activeMembers = await dbContext.UserGroupMemberships
                .CountAsync(entry => entry.GroupId == groupId, cancellationToken);
            if (activeMembers <= 1)
            {
                return MembershipRemoval.LastPrivilegedMember;
            }
        }

        dbContext.UserGroupMemberships.Remove(membership);
        return MembershipRemoval.Removed;
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

    public async Task<bool> RemoveGroupOrganizationUnitAsync(
        Guid groupId,
        Guid organizationUnitId,
        CancellationToken cancellationToken)
    {
        var assignment = await dbContext.GroupOrganizationUnits
            .FirstOrDefaultAsync(
                entry =>
                    entry.GroupId == groupId &&
                    entry.OrganizationUnitId == organizationUnitId,
                cancellationToken);
        if (assignment is null)
        {
            return false;
        }

        dbContext.GroupOrganizationUnits.Remove(assignment);
        return true;
    }

    public async Task<IReadOnlyList<Guid>> ListGroupOrganizationUnitIdsAsync(
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var ids = await dbContext.GroupOrganizationUnits
            .AsNoTracking()
            .Where(assignment => assignment.GroupId == groupId)
            .Select(assignment => assignment.OrganizationUnitId)
            .ToListAsync(cancellationToken);
        return ids;
    }

    // Agent C
    public Task<UserOrganizationUnit?> FindUserOrganizationUnitAsync(
        Guid userId,
        Guid organizationUnitId,
        CancellationToken cancellationToken) =>
        dbContext.UserOrganizationUnits
            .FirstOrDefaultAsync(
                membership =>
                    membership.UserId == userId &&
                    membership.OrganizationUnitId == organizationUnitId,
                cancellationToken);

    public Task<UserOrganizationUnit?> FindActivePrimaryUserOrganizationUnitAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        dbContext.UserOrganizationUnits
            .FirstOrDefaultAsync(
                membership =>
                    membership.UserId == userId &&
                    membership.IsActive &&
                    membership.Relationship == OrganizationUnitRelationship.Primary,
                cancellationToken);

    public Task AddUserOrganizationUnitAsync(
        UserOrganizationUnit membership,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        dbContext.UserOrganizationUnits.Add(membership);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<UserOrganizationUnit>> ListUserOrganizationUnitsAsync(
        Guid userId,
        bool activeOnly,
        CancellationToken cancellationToken)
    {
        var query = dbContext.UserOrganizationUnits
            .AsNoTracking()
            .Where(membership => membership.UserId == userId);

        if (activeOnly)
        {
            query = query.Where(membership => membership.IsActive);
        }

        return await query
            .OrderBy(membership => membership.Relationship)
            .ThenBy(membership => membership.AssignedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PermissionDefinition>> ListActivePermissionsByCodesAsync(
        IReadOnlyCollection<string> codes,
        CancellationToken cancellationToken)
    {
        if (codes.Count == 0)
        {
            return [];
        }

        var normalized = codes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(PermissionMatcher.Normalize)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (normalized.Length == 0)
        {
            return [];
        }

        return await dbContext.PermissionDefinitions
            .AsNoTracking()
            .Where(permission =>
                permission.IsActive &&
                normalized.Contains(permission.Code))
            .OrderBy(permission => permission.Code)
            .ToListAsync(cancellationToken);
    }

    public Task<IReadOnlyList<Guid>> GetDescendantOrganizationUnitIdsAsync(
        Guid rootOrganizationUnitId,
        CancellationToken cancellationToken) =>
        OrganizationUnitQueries.CollectAccessibleIdsAsync(
            dbContext,
            [rootOrganizationUnitId],
            activeOnly: false,
            cancellationToken);

    public async Task<bool> WouldCreateOrganizationUnitCycleAsync(
        Guid organizationUnitId,
        Guid? newParentId,
        CancellationToken cancellationToken)
    {
        if (newParentId is null)
        {
            return false;
        }

        if (newParentId.Value == organizationUnitId)
        {
            return true;
        }

        var current = newParentId;
        var guard = 0;
        while (current is Guid parentId)
        {
            if (parentId == organizationUnitId)
            {
                return true;
            }

            current = await dbContext.OrganizationUnits
                .AsNoTracking()
                .Where(unit => unit.Id == parentId)
                .Select(unit => unit.ParentId)
                .FirstOrDefaultAsync(cancellationToken);

            if (++guard > 10_000)
            {
                return true;
            }
        }

        return false;
    }

    public async Task<PageResult<AuthorizationAuditEvent>> ListAuditEventsAsync(
        PageRequest page,
        string? action,
        Guid? actorUserId,
        CancellationToken cancellationToken)
    {
        var query = dbContext.AuthorizationAuditEvents.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(action))
        {
            var normalized = action.Trim();
            query = query.Where(entry => entry.Action == normalized);
        }

        if (actorUserId is not null)
        {
            query = query.Where(entry => entry.ActorUserId == actorUserId);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(entry => entry.OccurredAt)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return new PageResult<AuthorizationAuditEvent>(
            items,
            totalCount,
            page.Page,
            page.PageSize);
    }
}
