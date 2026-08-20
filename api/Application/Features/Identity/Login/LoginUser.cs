using Application.Common.Results;
using Application.Common.Validation;
using Application.Features.Identity.Abstractions;
using Application.Features.Identity.Contracts;
using FluentValidation;

namespace Application.Features.Identity.Login;

public sealed class LoginUser(
    IValidator<LoginUserRequest> validator,
    IUserAccountService userAccountService,
    AuthTokenIssuer tokenIssuer)
{
    public async Task<Result<AuthResponse>> HandleAsync(
        LoginUserRequest request,
        CancellationToken cancellationToken)
    {
        var validationFailure = (await validator.ValidateAsync(request, cancellationToken))
            .ToFailure<AuthResponse>();
        if (validationFailure is not null)
        {
            return validationFailure;
        }

        var account = await userAccountService.AuthenticateAsync(
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
