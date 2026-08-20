namespace Application.Features.Identity.Abstractions;

public sealed record UserAccount(
    Guid Id,
    string Email,
    DateTimeOffset CreatedAt);
