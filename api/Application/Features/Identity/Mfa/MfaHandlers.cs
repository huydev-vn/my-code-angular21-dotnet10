using Application.Common.Results;
using Application.Common.Validation;
using Application.Features.Identity.Abstractions;
using Application.Features.Identity.Contracts;
using Application.Features.Identity.Errors;
using FluentValidation;

namespace Application.Features.Identity.Mfa;

public sealed record VerifyMfaLoginRequest
{
    public required string MfaToken { get; init; }

    public required string Code { get; init; }
}

public sealed class VerifyMfaLoginRequestValidator : AbstractValidator<VerifyMfaLoginRequest>
{
    public VerifyMfaLoginRequestValidator()
    {
        RuleFor(request => request.MfaToken).NotEmpty().MaximumLength(512);
        RuleFor(request => request.Code).NotEmpty().Length(6, 8);
    }
}

public sealed class VerifyMfaLogin(
    IValidator<VerifyMfaLoginRequest> validator,
    IMfaChallengeStore mfaChallengeStore,
    IUserAccountService userAccountService,
    AuthTokenIssuer tokenIssuer,
    IAuthMetrics authMetrics)
{
    public async Task<Result<AuthResponse>> HandleAsync(
        VerifyMfaLoginRequest request,
        CancellationToken cancellationToken)
    {
        var validationFailure = (await validator.ValidateAsync(request, cancellationToken))
            .ToFailure<AuthResponse>();
        if (validationFailure is not null)
        {
            return validationFailure;
        }

        var userId = await mfaChallengeStore.ConsumeAsync(request.MfaToken, cancellationToken);
        if (userId is null)
        {
            authMetrics.MfaFailed();
            return Result<AuthResponse>.Failure(IdentityErrors.InvalidMfaTicket);
        }

        var verified = await userAccountService.VerifyAuthenticatorCodeAsync(
            userId.Value,
            request.Code,
            cancellationToken);
        if (verified.IsFailure)
        {
            authMetrics.MfaFailed();
            return Result<AuthResponse>.Failure(verified.Error!);
        }

        var user = await userAccountService.FindByIdAsync(userId.Value, cancellationToken);
        if (user is null || user.IsLockedOut)
        {
            authMetrics.MfaFailed();
            return Result<AuthResponse>.Failure(IdentityErrors.InvalidMfaTicket);
        }

        var tokens = await tokenIssuer.IssueAsync(
            user,
            familyId: null,
            current: null,
            cancellationToken);
        if (tokens is null)
        {
            authMetrics.MfaFailed();
            return Result<AuthResponse>.Failure(IdentityErrors.TokenIssuanceFailed);
        }

        authMetrics.MfaSucceeded();
        return Result<AuthResponse>.Success(tokens);
    }
}

public sealed record ConfirmAuthenticatorSetupRequest
{
    public required string Code { get; init; }
}

public sealed class ConfirmAuthenticatorSetupRequestValidator
    : AbstractValidator<ConfirmAuthenticatorSetupRequest>
{
    public ConfirmAuthenticatorSetupRequestValidator()
    {
        RuleFor(request => request.Code).NotEmpty().Length(6, 8);
    }
}

public sealed class BeginAuthenticatorSetup(IUserAccountService userAccountService)
{
    public Task<Result<AuthenticatorSetup>> HandleAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        userAccountService.BeginAuthenticatorSetupAsync(userId, cancellationToken);
}

public sealed class ConfirmAuthenticatorSetup(
    IValidator<ConfirmAuthenticatorSetupRequest> validator,
    IUserAccountService userAccountService)
{
    public async Task<Result> HandleAsync(
        Guid userId,
        ConfirmAuthenticatorSetupRequest request,
        CancellationToken cancellationToken)
    {
        var validationFailure = (await validator.ValidateAsync(request, cancellationToken))
            .ToFailure();
        if (validationFailure is not null)
        {
            return validationFailure;
        }

        return await userAccountService.ConfirmAuthenticatorSetupAsync(
            userId,
            request.Code,
            cancellationToken);
    }
}

public sealed record DisableAuthenticatorRequest
{
    public required string Code { get; init; }
}

public sealed class DisableAuthenticatorRequestValidator
    : AbstractValidator<DisableAuthenticatorRequest>
{
    public DisableAuthenticatorRequestValidator()
    {
        RuleFor(request => request.Code).NotEmpty().Length(6, 8);
    }
}

public sealed class DisableAuthenticator(
    IValidator<DisableAuthenticatorRequest> validator,
    IUserAccountService userAccountService,
    Application.Features.Authorization.Abstractions.IAuthorizationAdminStore authorizationAdminStore,
    Application.Common.Settings.IIdentitySettings identitySettings)
{
    public async Task<Result> HandleAsync(
        Guid userId,
        DisableAuthenticatorRequest request,
        CancellationToken cancellationToken)
    {
        var validationFailure = (await validator.ValidateAsync(request, cancellationToken))
            .ToFailure();
        if (validationFailure is not null)
        {
            return validationFailure;
        }

        if (identitySettings.RequireMfaForPrivileged &&
            await authorizationAdminStore.IsMemberOfAnyPrivilegedGroupAsync(
                userId,
                cancellationToken))
        {
            return Result.Failure(IdentityErrors.PrivilegedMfaRequired);
        }

        return await userAccountService.DisableAuthenticatorAsync(
            userId,
            request.Code,
            cancellationToken);
    }
}
