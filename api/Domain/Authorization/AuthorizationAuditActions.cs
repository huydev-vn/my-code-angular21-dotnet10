namespace Domain.Authorization;

public static class AuthorizationAuditActions
{
    public const string PermissionCreated = "permission.created";
    public const string PermissionUpdated = "permission.updated";
    public const string PermissionActivated = "permission.activated";
    public const string PermissionDeactivated = "permission.deactivated";
    public const string GroupCreated = "group.created";
    public const string GroupUpdated = "group.updated";
    public const string GroupActivated = "group.activated";
    public const string GroupDeactivated = "group.deactivated";
    public const string OrganizationUnitCreated = "organization-unit.created";
    public const string OrganizationUnitUpdated = "organization-unit.updated";
    public const string OrganizationUnitActivated = "organization-unit.activated";
    public const string OrganizationUnitDeactivated = "organization-unit.deactivated";
    public const string GroupPermissionAssigned = "group.permission.assigned";
    public const string GroupPermissionRevoked = "group.permission.revoked";
    public const string UserGroupAssigned = "group.user.assigned";
    public const string UserGroupRevoked = "group.user.revoked";
    public const string GroupOrganizationUnitAssigned = "group.organization-unit.assigned";
    public const string GroupOrganizationUnitRevoked = "group.organization-unit.revoked";
}
