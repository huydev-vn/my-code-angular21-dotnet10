using FluentValidation;

namespace Application.Features.Authorization.OrganizationUnits;

internal sealed class CreateOrganizationUnitValidator
    : AbstractValidator<CreateOrganizationUnitRequest>
{
    public CreateOrganizationUnitValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(request => request.Code)
            .NotEmpty()
            .MaximumLength(64)
            .Matches("^[A-Za-z0-9_-]+$")
            .WithMessage("Code may only contain letters, numbers, underscores, and hyphens.");
    }
}
