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
        DateTimeOffset assignedAt) =>
        new(groupId, organizationUnitId, assignedAt);
}
