using FluentValidation;

namespace Application.Features.Identity.Refresh;

internal sealed class RefreshTokensValidator : AbstractValidator<RefreshTokensRequest>
{
    public RefreshTokensValidator()
    {
        RuleFor(request => request.RefreshToken)
            .NotEmpty()
            .MaximumLength(512);
    }
}
