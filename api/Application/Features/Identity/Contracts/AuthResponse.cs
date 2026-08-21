namespace Application.Features.Identity.Contracts;

/// <summary>Access token returned to API clients. Refresh tokens are cookie-only.</summary>
public sealed record AccessTokenResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt);

/// <summary>Issued access and refresh tokens used inside the application layer.</summary>
public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt)
{
    public AccessTokenResponse ToAccessTokenResponse() =>
        new(AccessToken, AccessTokenExpiresAt);
}
