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
        DateTimeOffset assignedAt) =>
        new(userId, groupId, assignedAt);
}
