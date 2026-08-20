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
}
