namespace Domain.Authorization;

/// <summary>Grants a permission from the catalog to a user group.</summary>
public sealed class GroupPermission
{
    private GroupPermission()
    {
    }

    private GroupPermission(Guid groupId, Guid permissionId, DateTimeOffset assignedAt)
    {
        GroupId = groupId;
        PermissionId = permissionId;
        AssignedAt = assignedAt;
    }

    public Guid GroupId { get; private set; }

    public Guid PermissionId { get; private set; }

    public DateTimeOffset AssignedAt { get; private set; }

    public static GroupPermission Create(
        Guid groupId,
        Guid permissionId,
        DateTimeOffset assignedAt) =>
        new(groupId, permissionId, assignedAt);
}
