namespace Domain.Authorization;

/// <summary>
/// Explicit user ↔ organization-unit membership (Primary / Additional).
/// Organizational metadata only: assigning a user to an OU does <strong>not</strong>
/// grant permissions or auto-merge into accessible data scope.
/// Runtime data access comes from group → OU assignments (<see cref="GroupOrganizationUnit"/>),
/// not from this table.
/// </summary>
public sealed class UserOrganizationUnit
{
    private UserOrganizationUnit()
    {
    }

    private UserOrganizationUnit(
        Guid userId,
        Guid organizationUnitId,
        OrganizationUnitRelationship relationship,
        DateTimeOffset assignedAt)
    {
        UserId = userId;
        OrganizationUnitId = organizationUnitId;
        Relationship = relationship;
        IsActive = true;
        AssignedAt = assignedAt;
    }

    public Guid UserId { get; private set; }

    public Guid OrganizationUnitId { get; private set; }

    public OrganizationUnitRelationship Relationship { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset AssignedAt { get; private set; }

    public static UserOrganizationUnit Create(
        Guid userId,
        Guid organizationUnitId,
        OrganizationUnitRelationship relationship,
        DateTimeOffset assignedAt)
    {
        EnsureNonEmpty(userId, nameof(userId));
        EnsureNonEmpty(organizationUnitId, nameof(organizationUnitId));
        EnsureDefined(relationship);
        EnsureValidTimestamp(assignedAt);

        return new UserOrganizationUnit(userId, organizationUnitId, relationship, assignedAt);
    }

    /// <summary>
    /// Returns true when making <paramref name="organizationUnitId"/> the user's Primary
    /// would leave more than one active Primary among <paramref name="activeMemberships"/>.
    /// </summary>
    public static bool WouldViolateSinglePrimary(
        IEnumerable<UserOrganizationUnit> activeMemberships,
        Guid organizationUnitId,
        OrganizationUnitRelationship relationship)
    {
        ArgumentNullException.ThrowIfNull(activeMemberships);
        if (relationship != OrganizationUnitRelationship.Primary)
        {
            return false;
        }

        return activeMemberships.Any(membership =>
            membership.IsActive &&
            membership.Relationship == OrganizationUnitRelationship.Primary &&
            membership.OrganizationUnitId != organizationUnitId);
    }

    public void SetRelationship(OrganizationUnitRelationship relationship)
    {
        EnsureDefined(relationship);
        Relationship = relationship;
    }

    /// <summary>Reactivates a previously deactivated membership and refreshes AssignedAt.</summary>
    public void Reactivate(OrganizationUnitRelationship relationship, DateTimeOffset assignedAt)
    {
        EnsureDefined(relationship);
        EnsureValidTimestamp(assignedAt);
        Relationship = relationship;
        IsActive = true;
        AssignedAt = assignedAt;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    private static void EnsureNonEmpty(Guid value, string paramName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Id cannot be an empty GUID.", paramName);
        }
    }

    private static void EnsureDefined(OrganizationUnitRelationship relationship)
    {
        if (!Enum.IsDefined(relationship))
        {
            throw new ArgumentOutOfRangeException(nameof(relationship));
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
