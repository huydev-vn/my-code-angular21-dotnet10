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
        Version = 1;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public string Code { get; private set; } = null!;

    public Guid? ParentId { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Application-managed optimistic concurrency token.</summary>
    public int Version { get; private set; }

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
        EnsureValidParentId(parentId);
        EnsureValidTimestamp(createdAt);

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
        BumpVersion();
    }

    /// <summary>
    /// Moves this unit under <paramref name="parentId"/>.
    /// Pass the current parent map so cycle detection can protect the invariant.
    /// </summary>
    public void Move(Guid? parentId, IReadOnlyDictionary<Guid, Guid?> parentById)
    {
        ArgumentNullException.ThrowIfNull(parentById);
        EnsureValidParentId(parentId);

        if (parentId == Id)
        {
            throw new InvalidOperationException(
                "An organization unit cannot be its own parent.");
        }

        if (OrganizationUnitHierarchy.WouldCreateCycle(Id, parentId, parentById))
        {
            throw new InvalidOperationException(
                "Moving the organization unit would create a cycle.");
        }

        ParentId = parentId;
        BumpVersion();
    }

    public void Activate()
    {
        IsActive = true;
        BumpVersion();
    }

    public void Deactivate()
    {
        IsActive = false;
        BumpVersion();
    }

    private void BumpVersion() => Version++;

    private static void EnsureValidParentId(Guid? parentId)
    {
        if (parentId == Guid.Empty)
        {
            throw new ArgumentException(
                "Parent id cannot be an empty GUID.",
                nameof(parentId));
        }
    }

    private static void EnsureValidTimestamp(DateTimeOffset createdAt)
    {
        if (createdAt == default)
        {
            throw new ArgumentException(
                "CreatedAt must be a valid timestamp.",
                nameof(createdAt));
        }
    }
}
