namespace Application.Features.Identity.Revoke;

/// <summary>Payload for revoking a refresh-token family.</summary>
public sealed record RevokeRefreshTokenRequest
{
    public required string RefreshToken { get; init; }
}
