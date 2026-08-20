namespace Domain.Authorization;

/// <summary>Links an authenticated user to a business user group.</summary>
public sealed class UserGroupMembership
{
    private UserGroupMembership()
    {
    }

    private UserGroupMembership(Guid userId, Guid groupId, DateTimeOffset assignedAt)
    {
        UserId = userId;
        GroupId = groupId;
        AssignedAt = assignedAt;
    }

    public Guid UserId { get; private set; }

    public Guid GroupId { get; private set; }

    public DateTimeOffset AssignedAt { get; private set; }

    public static UserGroupMembership Create(
        Guid userId,
        Guid groupId,
        DateTimeOffset assignedAt)
    {
        EnsureNonEmpty(userId, nameof(userId));
        EnsureNonEmpty(groupId, nameof(groupId));
        EnsureValidTimestamp(assignedAt);

        return new UserGroupMembership(userId, groupId, assignedAt);
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
