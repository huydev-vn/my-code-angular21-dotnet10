using FluentValidation;

namespace Application.Features.Authorization.Groups;

internal sealed class CreateUserGroupValidator : AbstractValidator<CreateUserGroupRequest>
{
    public CreateUserGroupValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(request => request.Description)
            .MaximumLength(1024);
    }
}
