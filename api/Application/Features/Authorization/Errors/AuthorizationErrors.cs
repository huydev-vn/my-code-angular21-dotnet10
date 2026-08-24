namespace Application.Features.Authorization.Errors;

using Application.Common.Errors;

public static class AuthorizationErrors
{
    public static readonly Error PermissionNotFound =
        Error.NotFound("authorization.permission_not_found", "Permission was not found.");

    public static readonly Error PermissionCodeTaken =
        Error.Conflict("authorization.permission_code_taken", "Permission code already exists.");

    public static readonly Error GroupNotFound =
        Error.NotFound("authorization.group_not_found", "User group was not found.");

    public static readonly Error GroupNameTaken =
        Error.Conflict("authorization.group_name_taken", "User group name already exists.");

    public static readonly Error OrganizationUnitNotFound =
        Error.NotFound(
            "authorization.organization_unit_not_found",
            "Organization unit was not found.");

    public static readonly Error OrganizationUnitCodeTaken =
        Error.Conflict(
            "authorization.organization_unit_code_taken",
            "Organization unit code already exists.");

    public static readonly Error AssignmentAlreadyExists =
        Error.Conflict(
            "authorization.assignment_exists",
            "The assignment already exists.");

    public static readonly Error AssignmentNotFound =
        Error.NotFound(
            "authorization.assignment_not_found",
            "The assignment was not found.");

    public static readonly Error OrganizationUnitCycle =
        Error.Validation(
            "authorization.organization_unit_cycle",
            "Moving the organization unit would create a cycle.");

    public static readonly Error ParentOrganizationUnitNotFound =
        Error.NotFound(
            "authorization.parent_organization_unit_not_found",
            "Parent organization unit was not found.");

    public static readonly Error PrivilegedGroupMutationForbidden =
        Error.Forbidden(
            "authorization.privileged_group_forbidden",
            "Only members of a privileged group may modify privileged group membership or high-risk permissions.");

    public static readonly Error PrivilegedGroupDeactivateForbidden =
        Error.Forbidden(
            "authorization.privileged_group_deactivate_forbidden",
            "Privileged groups cannot be deactivated.");

    public static readonly Error PrivilegedGroupOrganizationUnitForbidden =
        Error.Forbidden(
            "authorization.privileged_group_ou_forbidden",
            "Privileged groups are global and cannot be scoped to organization units.");

    public static readonly Error PrivilegedPermissionRequiresPrivilegedGroup =
        Error.Forbidden(
            "authorization.privileged_permission_requires_privileged_group",
            "High-risk authorization permissions can only be assigned to privileged groups.");

    public static readonly Error LastPrivilegedMemberRequired =
        Error.Conflict(
            "authorization.last_privileged_member_required",
            "Cannot remove the last active member of a privileged group.");

    public static readonly Error GroupInactive =
        Error.Validation(
            "authorization.group_inactive",
            "Assignments cannot target an inactive user group.");

    public static readonly Error PermissionInactive =
        Error.Validation(
            "authorization.permission_inactive",
            "Assignments cannot target an inactive permission.");

    public static readonly Error OrganizationUnitInactive =
        Error.Validation(
            "authorization.organization_unit_inactive",
            "Assignments cannot target an inactive organization unit.");
}
