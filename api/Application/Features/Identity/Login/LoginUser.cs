using System.Security.Cryptography;
using Application.Common.Results;
using Application.Common.Settings;
using Application.Common.Time;
using Application.Common.Validation;
using Application.Features.Identity.Abstractions;
using Application.Features.Identity.Contracts;
using Application.Features.Identity.Errors;
using FluentValidation;

namespace Application.Features.Identity.Login;

public sealed class LoginUser(
    IValidator<LoginUserRequest> validator,
    IUserAccountService userAccountService,
    AuthTokenIssuer tokenIssuer,
    IMfaChallengeStore mfaChallengeStore,
    IIdentitySettings identitySettings,
    IClock clock,
    IAuthMetrics authMetrics)
{
    public async Task<Result<LoginResult>> HandleAsync(
        LoginUserRequest request,
        CancellationToken cancellationToken)
    {
        var validationFailure = (await validator.ValidateAsync(request, cancellationToken))
            .ToFailure<LoginResult>();
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
            authMetrics.LoginFailed();
            return Result<LoginResult>.Failure(account.Error!);
        }

        var user = account.Value!;
        if (user.TwoFactorEnabled)
        {
            var minutes = Math.Clamp(identitySettings.MfaChallengeMinutes, 1, 30);
            var expiresAt = clock.UtcNow.AddMinutes(minutes);
            var ticket = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            await mfaChallengeStore.StoreAsync(ticket, user.Id, expiresAt, cancellationToken);
            authMetrics.MfaChallengeIssued();
            return Result<LoginResult>.Success(LoginResult.Challenge(ticket, expiresAt));
        }

        var tokens = await tokenIssuer.IssueAsync(
            user,
            familyId: null,
            current: null,
            cancellationToken);
        if (tokens is null)
        {
            authMetrics.LoginFailed();
            return Result<LoginResult>.Failure(IdentityErrors.TokenIssuanceFailed);
        }

        authMetrics.LoginSucceeded();
        return Result<LoginResult>.Success(LoginResult.Succeeded(tokens));
    }
}
