namespace Domain.Authorization;

/// <summary>Business user group used to assign permissions and data scope.</summary>
public sealed class UserGroup
{
    private UserGroup()
    {
    }

    private UserGroup(
        Guid id,
        string name,
        string? description,
        DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        Description = description;
        IsActive = true;
        CreatedAt = createdAt;
        Version = 1;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Application-managed optimistic concurrency token.</summary>
    public int Version { get; private set; }

    public static UserGroup Create(
        string name,
        string? description,
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
            createdAt);
    }

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

    public void Deactivate()
    {
        IsActive = false;
        Version++;
    }
}
