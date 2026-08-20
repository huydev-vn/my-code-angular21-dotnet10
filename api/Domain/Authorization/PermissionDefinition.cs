namespace Domain.Authorization;

/// <summary>
/// Dynamic permission entry in the catalog. Permissions are created at runtime
/// and referenced by stable codes such as <c>users.read</c> or <c>invoice.export.excel</c>.
/// </summary>
public sealed class PermissionDefinition
{
    private PermissionDefinition()
    {
    }

    private PermissionDefinition(
        Guid id,
        string code,
        string name,
        string? module,
        string? action,
        DateTimeOffset createdAt)
    {
        Id = id;
        Code = code;
        Name = name;
        Module = module;
        Action = action;
        IsActive = true;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string? Module { get; private set; }

    public string? Action { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static PermissionDefinition Create(
        string code,
        string name,
        string? module,
        string? action,
        DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new PermissionDefinition(
            Guid.NewGuid(),
            code.Trim().ToLowerInvariant(),
            name.Trim(),
            string.IsNullOrWhiteSpace(module) ? null : module.Trim(),
            string.IsNullOrWhiteSpace(action) ? null : action.Trim(),
            createdAt);
    }

    public void Update(string name, string? module, string? action)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Module = string.IsNullOrWhiteSpace(module) ? null : module.Trim();
        Action = string.IsNullOrWhiteSpace(action) ? null : action.Trim();
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
