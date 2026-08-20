namespace Application.Features.Identity.Abstractions;

public sealed record IssuedRefreshToken(
    string PlainText,
    string Hash,
    DateTimeOffset ExpiresAt);
