namespace Application.Features.Identity.Abstractions;

/// <summary>Application-facing identity account snapshot (not an EF entity).</summary>
public sealed record UserAccount(
    Guid Id,
    string Email,
    DateTimeOffset CreatedAt,
    bool IsLockedOut,
    bool TwoFactorEnabled);
