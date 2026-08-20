using FluentValidation;

namespace Application.Features.Authorization.Permissions;

internal sealed class CreatePermissionDefinitionValidator
    : AbstractValidator<CreatePermissionDefinitionRequest>
{
    public CreatePermissionDefinitionValidator()
    {
        RuleFor(request => request.Code)
            .NotEmpty()
            .MaximumLength(128)
            .Matches("^[a-z0-9]+([.-][a-z0-9]+)*$")
            .WithMessage("Code must use lowercase segments separated by '.' or '-'.");

        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(request => request.Module)
            .MaximumLength(128);

        RuleFor(request => request.Action)
            .MaximumLength(128);
    }
}
