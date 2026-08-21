namespace Application.Features.Identity.Refresh;

/// <summary>Payload for rotating an access token with a refresh token.</summary>
public sealed record RefreshTokensRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}
