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
    public const string AuthorizationAuditRead = "authorization.audit.read";

    /// <summary>
    /// Delegated (regional) assignment: group permissions, group↔OU scope, and user↔group
    /// within the actor's grant + OU containment. Not Critical; not privileged-only.
    /// </summary>
    public const string AuthorizationAssignmentsDelegate = "authorization.assignments.delegate";

    /// <summary>
    /// Delegated manage of user↔OU membership metadata within actor accessible OUs.
    /// Does not grant data access. Not Critical; not privileged-only.
    /// </summary>
    public const string AuthorizationUsersOrganizationUnitsManage =
        "authorization.users-organization-units.manage";

    /// <summary>
    /// Hard-coded high-risk bootstrap write codes; assignable only to privileged groups.
    /// Delegated regional codes are intentionally excluded.
    /// </summary>
    public static bool IsPrivilegedCatalogPermission(string code) =>
        code is AuthorizationPermissionsWrite
            or AuthorizationGroupsWrite
            or AuthorizationOrganizationUnitsWrite;

    /// <summary>
    /// Privileged when the code is in the bootstrap write list or RiskLevel is Critical,
    /// so new critical catalog entries are covered without forgetting the hard-code.
    /// </summary>
    public static bool IsPrivilegedCatalogPermission(PermissionDefinition permission)
    {
        ArgumentNullException.ThrowIfNull(permission);
        return IsPrivilegedCatalogPermission(permission.Code)
            || permission.RiskLevel == PermissionRiskLevel.Critical;
    }

    /// <summary>
    /// Seed catalog: Module is display grouping; Resource is the stable enforcement key.
    /// users.* stay Global today because ListUsers uses [RequirePermission] (not OU-scoped);
    /// Agent B may later switch them to OrganizationUnit when users become OU-filtered.
    /// Owner scope mode is omitted until Agent B wires owner filtering.
    /// </summary>
    public static IReadOnlyList<(
        string Code,
        string Name,
        string Module,
        string Action,
        string Resource,
        PermissionScopeMode ScopeMode,
        PermissionRiskLevel RiskLevel)> All { get; } =
    [
        (UsersRead, "View users", "users", "read", "users",
            PermissionScopeMode.Global, PermissionRiskLevel.Medium),
        (UsersWrite, "Manage users", "users", "write", "users",
            PermissionScopeMode.Global, PermissionRiskLevel.High),
        (AuthorizationPermissionsRead, "View permission catalog", "authorization", "permissions.read",
            "authorization.permissions", PermissionScopeMode.Global, PermissionRiskLevel.Medium),
        (AuthorizationPermissionsWrite, "Manage permission catalog", "authorization", "permissions.write",
            "authorization.permissions", PermissionScopeMode.Global, PermissionRiskLevel.Critical),
        (AuthorizationGroupsRead, "View user groups", "authorization", "groups.read",
            "authorization.groups", PermissionScopeMode.Global, PermissionRiskLevel.Medium),
        (AuthorizationGroupsWrite, "Manage user groups", "authorization", "groups.write",
            "authorization.groups", PermissionScopeMode.Global, PermissionRiskLevel.Critical),
        (AuthorizationOrganizationUnitsRead, "View organization units", "authorization", "organization-units.read",
            "authorization.organization-units", PermissionScopeMode.Global, PermissionRiskLevel.Medium),
        (AuthorizationOrganizationUnitsWrite, "Manage organization units", "authorization", "organization-units.write",
            "authorization.organization-units", PermissionScopeMode.Global, PermissionRiskLevel.Critical),
        (AuthorizationAuditRead, "View authorization audit log", "authorization", "audit.read",
            "authorization.audit", PermissionScopeMode.Global, PermissionRiskLevel.Medium),
        (AuthorizationAssignmentsDelegate, "Delegate authorization assignments within scope",
            "authorization", "assignments.delegate", "authorization.assignments",
            PermissionScopeMode.OrganizationUnit, PermissionRiskLevel.High),
        (AuthorizationUsersOrganizationUnitsManage, "Manage user organization-unit membership within scope",
            "authorization", "users-organization-units.manage", "authorization.users-organization-units",
            PermissionScopeMode.OrganizationUnit, PermissionRiskLevel.High)
    ];
}
