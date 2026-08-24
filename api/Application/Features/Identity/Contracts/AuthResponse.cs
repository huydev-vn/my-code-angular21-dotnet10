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

/// <summary>Password login outcome: either tokens or an MFA challenge.</summary>
public sealed record LoginResult(
    AuthResponse? Tokens,
    string? MfaToken,
    DateTimeOffset? MfaExpiresAt)
{
    public bool RequiresMfa =>
        !string.IsNullOrWhiteSpace(MfaToken) && MfaExpiresAt is not null;

    public static LoginResult Succeeded(AuthResponse tokens) =>
        new(tokens, MfaToken: null, MfaExpiresAt: null);

    public static LoginResult Challenge(string mfaToken, DateTimeOffset expiresAt) =>
        new(Tokens: null, mfaToken, expiresAt);
}

/// <summary>Browser/API response when TOTP is required after password login.</summary>
public sealed record MfaChallengeResponse(
    string MfaToken,
    DateTimeOffset ExpiresAt);

/// <summary>Authenticator enrollment payload for authenticator apps.</summary>
public sealed record AuthenticatorSetupResponse(
    string SharedKey,
    string AuthenticatorUri);
