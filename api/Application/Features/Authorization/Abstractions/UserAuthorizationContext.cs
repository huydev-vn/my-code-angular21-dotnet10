using Domain.Authorization;

namespace Application.Features.Authorization.Abstractions;

/// <summary>Effective authorization state resolved for a user at runtime.</summary>
/// <remarks>
/// Cache shape includes <see cref="PermissionScopeByCode"/> (Agent B). Older Redis entries
/// without that map expire via TTL; missing map is treated as empty (fail closed for OU scope).
/// </remarks>
public sealed record UserAuthorizationContext(
    Guid UserId,
    IReadOnlyList<string> GroupNames,
    IReadOnlyList<string> PermissionCodes,
    IReadOnlyList<Guid> AccessibleOrganizationUnitIds,
    IReadOnlyDictionary<string, PermissionScopeMode>? PermissionScopeByCode)
{
    public static readonly IReadOnlyDictionary<string, PermissionScopeMode> EmptyPermissionScopes =
        new Dictionary<string, PermissionScopeMode>(StringComparer.Ordinal);

    /// <summary>Normalizes a deserialized context that may lack the Agent B scope map.</summary>
    public UserAuthorizationContext WithNormalizedScopes() =>
        PermissionScopeByCode is null
            ? this with { PermissionScopeByCode = EmptyPermissionScopes }
            : this;

    public bool TryGetPermissionScopeMode(
        string permissionCode,
        out PermissionScopeMode scopeMode)
    {
        scopeMode = default;
        var scopes = PermissionScopeByCode ?? EmptyPermissionScopes;
        var normalized = PermissionMatcher.Normalize(permissionCode);
        return scopes.TryGetValue(normalized, out scopeMode);
    }
}
