namespace Domain.Authorization;

/// <summary>
/// Danh mục quyền chức năng (permission catalog). Ví dụ: <c>users.read</c>,
/// <c>authorization.groups.write</c>. Đây là quyền thao tác chức năng, không phải nhóm user.
/// Catalog entries describe metadata for admins; they do not auto-create endpoints or policies.
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
        string? resource,
        PermissionScopeMode scopeMode,
        PermissionRiskLevel riskLevel,
        bool isSystemManaged,
        DateTimeOffset createdAt)
    {
        Id = id;
        Code = code;
        Name = name;
        Module = module;
        Action = action;
        Resource = resource;
        ScopeMode = scopeMode;
        RiskLevel = riskLevel;
        IsSystemManaged = isSystemManaged;
        IsActive = true;
        CreatedAt = createdAt;
        Version = 1;
    }

    public Guid Id { get; private set; }

    /// <summary>Mã ổn định dùng trong policy/handler, ví dụ users.read.</summary>
    public string Code { get; private set; } = null!;

    /// <summary>Tên hiển thị cho quản trị viên.</summary>
    public string Name { get; private set; } = null!;

    /// <summary>Display/grouping module (users, authorization, …). Kept for backward compatibility.</summary>
    public string? Module { get; private set; }

    /// <summary>Hành động trong module (read, write, create, export, …).</summary>
    public string? Action { get; private set; }

    /// <summary>
    /// Stable resource key for scope enforcement (e.g. users, authorization.permissions).
    /// Distinct from <see cref="Module"/> when a module owns multiple resources.
    /// </summary>
    public string? Resource { get; private set; }

    /// <summary>Expected scope mode when Agent B enforces data access.</summary>
    public PermissionScopeMode ScopeMode { get; private set; }

    /// <summary>Risk classification for assignment UX and privileged-only Critical grants.</summary>
    public PermissionRiskLevel RiskLevel { get; private set; }

    /// <summary>True for seeded system permissions; Code remains immutable either way.</summary>
    public bool IsSystemManaged { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Application-managed optimistic concurrency token.</summary>
    public int Version { get; private set; }

    public static PermissionDefinition Create(
        string code,
        string name,
        string? module,
        string? action,
        PermissionScopeMode scopeMode,
        DateTimeOffset createdAt,
        string? resource = null,
        PermissionRiskLevel riskLevel = PermissionRiskLevel.Medium,
        bool isSystemManaged = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (!Enum.IsDefined(scopeMode))
        {
            throw new ArgumentOutOfRangeException(nameof(scopeMode));
        }

        if (!Enum.IsDefined(riskLevel))
        {
            throw new ArgumentOutOfRangeException(nameof(riskLevel));
        }

        if (createdAt == default)
        {
            throw new ArgumentException(
                "CreatedAt must be a valid timestamp.",
                nameof(createdAt));
        }

        var normalizedModule = NormalizeOptional(module);
        var normalizedResource = NormalizeOptional(resource) ?? normalizedModule;

        return new PermissionDefinition(
            Guid.NewGuid(),
            PermissionMatcher.Normalize(code),
            name.Trim(),
            normalizedModule,
            NormalizeOptional(action),
            normalizedResource,
            scopeMode,
            riskLevel,
            isSystemManaged,
            createdAt);
    }

    /// <summary>
    /// Updates display/metadata fields. Code and IsSystemManaged are immutable after create.
    /// </summary>
    public void Update(
        string name,
        string? module,
        string? action,
        string? resource,
        PermissionScopeMode scopeMode,
        PermissionRiskLevel riskLevel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (!Enum.IsDefined(scopeMode))
        {
            throw new ArgumentOutOfRangeException(nameof(scopeMode));
        }

        if (!Enum.IsDefined(riskLevel))
        {
            throw new ArgumentOutOfRangeException(nameof(riskLevel));
        }

        Name = name.Trim();
        Module = NormalizeOptional(module);
        Action = NormalizeOptional(action);
        Resource = NormalizeOptional(resource) ?? Module;
        ScopeMode = scopeMode;
        RiskLevel = riskLevel;
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

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
