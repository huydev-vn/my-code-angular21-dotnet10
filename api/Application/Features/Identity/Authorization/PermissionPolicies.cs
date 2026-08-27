namespace Application.Features.Identity.Authorization;

/// <summary>Authorization policy names for permission and organization-unit checks.</summary>
public static class PermissionPolicies
{
    public const string Prefix = "permission:";

    public const string UnitPrefix = "permission-unit:";

    public const string AnyPrefix = "permission-any:";

    public const string DefaultOrganizationUnitRouteKey = "organizationUnitId";

    public static string Name(string permission) => Prefix + permission;

    public static string ForUnit(
        string permission,
        string routeKey = DefaultOrganizationUnitRouteKey) =>
        $"{UnitPrefix}{permission}|{routeKey}";

    /// <summary>
    /// Policy that succeeds when the caller holds any of the listed permissions
    /// (e.g. system Critical write OR regional delegate).
    /// </summary>
    public static string Any(params string[] permissions)
    {
        ArgumentNullException.ThrowIfNull(permissions);
        if (permissions.Length == 0)
        {
            throw new ArgumentException(
                "At least one permission is required.",
                nameof(permissions));
        }

        return AnyPrefix + string.Join('|', permissions);
    }

    public static bool TryParseUnitPolicy(
        string policyName,
        out string permission,
        out string routeKey)
    {
        permission = string.Empty;
        routeKey = DefaultOrganizationUnitRouteKey;

        if (!policyName.StartsWith(UnitPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var remainder = policyName[UnitPrefix.Length..];
        var separator = remainder.LastIndexOf('|');
        if (separator < 0)
        {
            permission = remainder;
            return permission.Length > 0;
        }

        permission = remainder[..separator];
        routeKey = remainder[(separator + 1)..];
        return permission.Length > 0 && routeKey.Length > 0;
    }

    public static bool TryParseAnyPolicy(
        string policyName,
        out string[] permissions)
    {
        permissions = [];

        if (!policyName.StartsWith(AnyPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var remainder = policyName[AnyPrefix.Length..];
        permissions = remainder.Split(
            '|',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return permissions.Length > 0;
    }
}
