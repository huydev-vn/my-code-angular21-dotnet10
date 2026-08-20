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
        DateTimeOffset assignedAt)
    {
        EnsureNonEmpty(groupId, nameof(groupId));
        EnsureNonEmpty(permissionId, nameof(permissionId));
        EnsureValidTimestamp(assignedAt);

        return new GroupPermission(groupId, permissionId, assignedAt);
    }

    private static void EnsureNonEmpty(Guid value, string paramName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Id cannot be an empty GUID.", paramName);
        }
    }

    private static void EnsureValidTimestamp(DateTimeOffset assignedAt)
    {
        if (assignedAt == default)
        {
            throw new ArgumentException(
                "AssignedAt must be a valid timestamp.",
                nameof(assignedAt));
        }
    }
}
