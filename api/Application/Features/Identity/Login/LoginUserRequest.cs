namespace Application.Features.Identity.Login;

/// <summary>Payload for authenticating with email and password.</summary>
public sealed record LoginUserRequest
{
    public required string Email { get; init; }

    public required string Password { get; init; }
}
