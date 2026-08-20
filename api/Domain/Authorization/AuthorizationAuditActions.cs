namespace Domain.Authorization;

public static class AuthorizationAuditActions
{
    public const string PermissionCreated = "permission.created";
    public const string GroupCreated = "group.created";
    public const string OrganizationUnitCreated = "organization-unit.created";
    public const string GroupPermissionAssigned = "group.permission.assigned";
    public const string UserGroupAssigned = "group.user.assigned";
    public const string GroupOrganizationUnitAssigned = "group.organization-unit.assigned";
}
