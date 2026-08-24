namespace Application.Features.Identity.Contracts;

/// <summary>Public representation of an authenticated user.</summary>
public sealed record UserResponse(
    Guid Id,
    string Email,
    DateTimeOffset CreatedAt,
    bool TwoFactorEnabled,
    bool IsPrivileged,
    bool RequiresMfaEnrollment,
    IReadOnlyList<string> Groups,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<Guid> AccessibleOrganizationUnitIds);
