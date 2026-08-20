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

    public static readonly Error OrganizationUnitCycle =
        Error.Validation(
            "authorization.organization_unit_cycle",
            "Moving the organization unit would create a cycle.");

    public static readonly Error ParentOrganizationUnitNotFound =
        Error.NotFound(
            "authorization.parent_organization_unit_not_found",
            "Parent organization unit was not found.");
}
