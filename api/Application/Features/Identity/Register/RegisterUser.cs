using Application.Common.Results;
using Application.Common.Settings;
using Application.Common.Validation;
using Application.Features.Identity.Abstractions;
using Application.Features.Identity.Contracts;
using Application.Features.Identity.Errors;
using FluentValidation;

namespace Application.Features.Identity.Register;

public sealed class RegisterUser(
    IValidator<RegisterUserRequest> validator,
    IIdentitySettings identitySettings,
    IUserAccountService userAccountService,
    AuthTokenIssuer tokenIssuer)
{
    public async Task<Result<AuthResponse>> HandleAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!identitySettings.AllowRegistration)
        {
            return Result<AuthResponse>.Failure(IdentityErrors.RegistrationDisabled);
        }

        var validationFailure = (await validator.ValidateAsync(request, cancellationToken))
            .ToFailure<AuthResponse>();
        if (validationFailure is not null)
        {
            return validationFailure;
        }

        var account = await userAccountService.RegisterAsync(
            request.Email,
            request.Password,
            cancellationToken);
        if (account.IsFailure)
        {
            return Result<AuthResponse>.Failure(account.Error!);
        }

        var tokens = await tokenIssuer.IssueAsync(
            account.Value!,
            familyId: null,
            current: null,
            cancellationToken);
        if (tokens is null)
        {
            throw new InvalidOperationException("Access token issuance failed.");
        }

        return Result<AuthResponse>.Success(tokens);
    }
}
