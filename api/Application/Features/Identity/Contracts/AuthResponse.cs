namespace Application.Features.Identity.Contracts;

/// <summary>Issued access and refresh tokens.</summary>
public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
