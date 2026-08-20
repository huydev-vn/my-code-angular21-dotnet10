using FluentValidation;

namespace Application.Features.Identity.Revoke;

internal sealed class RevokeRefreshTokenValidator : AbstractValidator<RevokeRefreshTokenRequest>
{
    public RevokeRefreshTokenValidator()
    {
        RuleFor(request => request.RefreshToken)
            .NotEmpty()
            .MaximumLength(512);
    }
}
