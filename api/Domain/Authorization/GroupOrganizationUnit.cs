namespace Domain.Authorization;

/// <summary>
/// Assigns an organization unit scope to a group. Members inherit access to the
/// assigned unit and all descendants.
/// </summary>
public sealed class GroupOrganizationUnit
{
    private GroupOrganizationUnit()
    {
    }

    private GroupOrganizationUnit(
        Guid groupId,
        Guid organizationUnitId,
        DateTimeOffset assignedAt)
    {
        GroupId = groupId;
        OrganizationUnitId = organizationUnitId;
        AssignedAt = assignedAt;
    }

    public Guid GroupId { get; private set; }

    public Guid OrganizationUnitId { get; private set; }

    public DateTimeOffset AssignedAt { get; private set; }

    public static GroupOrganizationUnit Create(
        Guid groupId,
        Guid organizationUnitId,
        DateTimeOffset assignedAt)
    {
        EnsureNonEmpty(groupId, nameof(groupId));
        EnsureNonEmpty(organizationUnitId, nameof(organizationUnitId));
        EnsureValidTimestamp(assignedAt);

        return new GroupOrganizationUnit(groupId, organizationUnitId, assignedAt);
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
