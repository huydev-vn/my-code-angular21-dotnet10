namespace Domain.Authorization;

/// <summary>Bootstrap permission codes seeded on first run only.</summary>
public static class SystemPermissions
{
    public const string UsersRead = "users.read";
    public const string UsersWrite = "users.write";

    public const string AuthorizationPermissionsRead = "authorization.permissions.read";
    public const string AuthorizationPermissionsWrite = "authorization.permissions.write";
    public const string AuthorizationGroupsRead = "authorization.groups.read";
    public const string AuthorizationGroupsWrite = "authorization.groups.write";
    public const string AuthorizationOrganizationUnitsRead = "authorization.organization-units.read";
    public const string AuthorizationOrganizationUnitsWrite = "authorization.organization-units.write";

    public static IReadOnlyList<(string Code, string Name, string Module, string Action)> All { get; } =
    [
        (UsersRead, "View users", "users", "read"),
        (UsersWrite, "Manage users", "users", "write"),
        (AuthorizationPermissionsRead, "View permission catalog", "authorization", "permissions.read"),
        (AuthorizationPermissionsWrite, "Manage permission catalog", "authorization", "permissions.write"),
        (AuthorizationGroupsRead, "View user groups", "authorization", "groups.read"),
        (AuthorizationGroupsWrite, "Manage user groups", "authorization", "groups.write"),
        (AuthorizationOrganizationUnitsRead, "View organization units", "authorization", "organization-units.read"),
        (AuthorizationOrganizationUnitsWrite, "Manage organization units", "authorization", "organization-units.write")
    ];
}
