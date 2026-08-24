using Application.Common.Errors;

namespace Application.Features.Identity.Errors;

public static class IdentityErrors
{
    public static readonly Error InvalidCredentials = Error.Unauthorized(
        "identity.invalid_credentials",
        "Invalid email or password.");

    public static readonly Error EmailTaken = Error.Conflict(
        "identity.email_taken",
        "An account with this email already exists.");

    public static readonly Error RegistrationFailed = Error.Validation(
        "identity.registration_failed",
        "The account could not be created.");

    public static readonly Error InvalidRefreshToken = Error.Unauthorized(
        "identity.invalid_refresh_token",
        "The refresh token is invalid.");

    public static readonly Error UserNotFound = Error.NotFound(
        "identity.user_not_found",
        "User was not found.");

    public static readonly Error RegistrationDisabled = Error.Forbidden(
        "identity.registration_disabled",
        "Self-service registration is disabled.");

    public static readonly Error TokenIssuanceFailed = Error.Conflict(
        "identity.token_issuance_failed",
        "Authentication succeeded but tokens could not be issued. Try again.");

    public static readonly Error MfaRequired = Error.Unauthorized(
        "identity.mfa_required",
        "A multi-factor authentication code is required.");

    public static readonly Error InvalidMfaCode = Error.Unauthorized(
        "identity.invalid_mfa_code",
        "The multi-factor authentication code is invalid.");

    public static readonly Error InvalidMfaTicket = Error.Unauthorized(
        "identity.invalid_mfa_ticket",
        "The multi-factor authentication challenge is invalid or expired.");

    public static readonly Error MfaAlreadyEnabled = Error.Conflict(
        "identity.mfa_already_enabled",
        "Authenticator MFA is already enabled for this account.");

    public static readonly Error MfaNotEnabled = Error.Validation(
        "identity.mfa_not_enabled",
        "Authenticator MFA is not enabled for this account.");

    public static readonly Error PrivilegedMfaRequired = Error.Forbidden(
        "identity.privileged_mfa_required",
        "Privileged accounts must keep authenticator MFA enabled.");
}
