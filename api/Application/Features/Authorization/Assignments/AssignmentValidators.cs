using FluentValidation;

namespace Application.Features.Authorization.Assignments;

internal sealed class AssignGroupPermissionValidator
    : AbstractValidator<AssignGroupPermissionRequest>
{
    public AssignGroupPermissionValidator()
    {
        RuleFor(request => request.GroupId).NotEmpty();
        RuleFor(request => request.PermissionId).NotEmpty();
    }
}

internal sealed class AssignUserToGroupValidator : AbstractValidator<AssignUserToGroupRequest>
{
    public AssignUserToGroupValidator()
    {
        RuleFor(request => request.GroupId).NotEmpty();
        RuleFor(request => request.UserId).NotEmpty();
    }
}

internal sealed class AssignGroupOrganizationUnitValidator
    : AbstractValidator<AssignGroupOrganizationUnitRequest>
{
    public AssignGroupOrganizationUnitValidator()
    {
        RuleFor(request => request.GroupId).NotEmpty();
        RuleFor(request => request.OrganizationUnitId).NotEmpty();
    }
}

internal sealed class RevokeGroupPermissionValidator
    : AbstractValidator<RevokeGroupPermissionRequest>
{
    public RevokeGroupPermissionValidator()
    {
        RuleFor(request => request.GroupId).NotEmpty();
        RuleFor(request => request.PermissionId).NotEmpty();
    }
}

internal sealed class RevokeUserFromGroupValidator : AbstractValidator<RevokeUserFromGroupRequest>
{
    public RevokeUserFromGroupValidator()
    {
        RuleFor(request => request.GroupId).NotEmpty();
        RuleFor(request => request.UserId).NotEmpty();
    }
}

internal sealed class RevokeGroupOrganizationUnitValidator
    : AbstractValidator<RevokeGroupOrganizationUnitRequest>
{
    public RevokeGroupOrganizationUnitValidator()
    {
        RuleFor(request => request.GroupId).NotEmpty();
        RuleFor(request => request.OrganizationUnitId).NotEmpty();
    }
}
