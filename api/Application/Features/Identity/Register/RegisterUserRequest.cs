namespace Application.Features.Identity.Register;

/// <summary>Payload for creating a new user account.</summary>
public sealed record RegisterUserRequest
{
    public required string Email { get; init; }

    public required string Password { get; init; }
}
