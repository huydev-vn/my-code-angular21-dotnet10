namespace Domain.Authorization;

public static class PermissionMatcher
{
    public static string Normalize(string permissionCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionCode);
        return permissionCode.Trim().ToLowerInvariant();
    }

    public static bool Grants(
        IEnumerable<string> grantedCodes,
        string requestedPermissionCode)
    {
        ArgumentNullException.ThrowIfNull(grantedCodes);
        var normalized = Normalize(requestedPermissionCode);
        return grantedCodes.Contains(normalized, StringComparer.Ordinal);
    }
}
