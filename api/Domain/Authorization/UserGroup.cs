namespace Domain.Authorization;

/// <summary>
/// Nhóm phân quyền nghiệp vụ. Tập hợp permission (chức năng) và phạm vi đơn vị tổ chức.
/// Khác với role ASP.NET Identity; đây là cơ chế authorization chính của hệ thống.
/// Privileged groups (e.g. System Administrators) are global bootstrap admins and
/// must not receive organization-unit scope.
/// </summary>
public sealed class UserGroup
{
    private UserGroup()
    {
    }

    private UserGroup(
        Guid id,
        string name,
        string? description,
        bool isPrivileged,
        DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        Description = description;
        IsPrivileged = isPrivileged;
        IsActive = true;
        CreatedAt = createdAt;
        Version = 1;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    /// <summary>
    /// When true, membership and high-risk permission assignments require an actor
    /// who already belongs to a privileged group. Privileged groups are always global.
    /// </summary>
    public bool IsPrivileged { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Application-managed optimistic concurrency token.</summary>
    public int Version { get; private set; }

    public static UserGroup Create(
        string name,
        string? description,
        DateTimeOffset createdAt) =>
        CreateCore(name, description, isPrivileged: false, createdAt);

    /// <summary>Creates a bootstrap privileged group (seeder only).</summary>
    public static UserGroup CreatePrivileged(
        string name,
        string? description,
        DateTimeOffset createdAt) =>
        CreateCore(name, description, isPrivileged: true, createdAt);

    public void Update(string name, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Version++;
    }

    public void Activate()
    {
        IsActive = true;
        Version++;
    }

    /// <summary>Promotes an existing group to privileged (seeder upgrade path only).</summary>
    public void MarkPrivileged()
    {
        if (IsPrivileged)
        {
            return;
        }

        IsPrivileged = true;
        Version++;
    }

    public void Deactivate()
    {
        if (IsPrivileged)
        {
            throw new InvalidOperationException(
                "Privileged groups cannot be deactivated.");
        }

        IsActive = false;
        Version++;
    }

    private static UserGroup CreateCore(
        string name,
        string? description,
        bool isPrivileged,
        DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (createdAt == default)
        {
            throw new ArgumentException(
                "CreatedAt must be a valid timestamp.",
                nameof(createdAt));
        }

        return new UserGroup(
            Guid.NewGuid(),
            name.Trim(),
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            isPrivileged,
            createdAt);
    }
}
