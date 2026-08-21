namespace Application.Features.Identity.Revoke;

/// <summary>Payload for revoking a refresh-token family.</summary>
public sealed record RevokeRefreshTokenRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}
