namespace Application.Features.Identity.Authorization;

/// <summary>Authorization policy names for permission and organization-unit checks.</summary>
public static class PermissionPolicies
{
    public const string Prefix = "permission:";

    public const string UnitPrefix = "permission-unit:";

    public const string DefaultOrganizationUnitRouteKey = "organizationUnitId";

    public static string Name(string permission) => Prefix + permission;

    public static string ForUnit(
        string permission,
        string routeKey = DefaultOrganizationUnitRouteKey) =>
        $"{UnitPrefix}{permission}|{routeKey}";

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
}
