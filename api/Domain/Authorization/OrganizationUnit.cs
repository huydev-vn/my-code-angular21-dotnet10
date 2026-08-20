namespace Domain.Authorization;

/// <summary>
/// Hierarchical organization unit. Data visibility is scoped to assigned units
/// and all of their descendants.
/// </summary>
public sealed class OrganizationUnit
{
    private OrganizationUnit()
    {
    }

    private OrganizationUnit(
        Guid id,
        string name,
        string code,
        Guid? parentId,
        DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        Code = code;
        ParentId = parentId;
        IsActive = true;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public string Code { get; private set; } = null!;

    public Guid? ParentId { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static OrganizationUnit CreateRoot(
        string name,
        string code,
        DateTimeOffset createdAt) =>
        Create(name, code, parentId: null, createdAt);

    public static OrganizationUnit Create(
        string name,
        string code,
        Guid? parentId,
        DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        return new OrganizationUnit(
            Guid.NewGuid(),
            name.Trim(),
            code.Trim().ToUpperInvariant(),
            parentId,
            createdAt);
    }

    public void Update(string name, string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        Name = name.Trim();
        Code = code.Trim().ToUpperInvariant();
    }

    public void Move(Guid? parentId) => ParentId = parentId;

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
