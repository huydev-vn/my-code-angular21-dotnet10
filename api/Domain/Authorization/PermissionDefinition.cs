namespace Domain.Authorization;

/// <summary>
/// Danh mục quyền chức năng (permission catalog). Ví dụ: <c>users.read</c>,
/// <c>authorization.groups.write</c>. Đây là quyền thao tác chức năng, không phải nhóm user.
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
        Version = 1;
    }

    public Guid Id { get; private set; }

    /// <summary>Mã ổn định dùng trong policy/handler, ví dụ users.read.</summary>
    public string Code { get; private set; } = null!;

    /// <summary>Tên hiển thị cho quản trị viên.</summary>
    public string Name { get; private set; } = null!;

    /// <summary>Module nghiệp vụ (users, authorization, …).</summary>
    public string? Module { get; private set; }

    /// <summary>Hành động trong module (read, write, …).</summary>
    public string? Action { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Application-managed optimistic concurrency token.</summary>
    public int Version { get; private set; }

    public static PermissionDefinition Create(
        string code,
        string name,
        string? module,
        string? action,
        DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (createdAt == default)
        {
            throw new ArgumentException(
                "CreatedAt must be a valid timestamp.",
                nameof(createdAt));
        }

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
