namespace Application.Features.Identity.Abstractions;

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);
