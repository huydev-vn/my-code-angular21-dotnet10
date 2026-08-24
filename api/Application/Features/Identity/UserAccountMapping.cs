using Application.Features.Identity.Abstractions;
using Application.Features.Identity.Contracts;

namespace Application.Features.Identity;

internal static class UserAccountMapping
{
    public static UserResponse ToResponse(
        this UserAccount user,
        bool isPrivileged,
        bool requireMfaForPrivileged,
        IReadOnlyList<string> groups,
        IReadOnlyList<string> permissions,
        IReadOnlyList<Guid> accessibleOrganizationUnitIds) =>
        new(
            user.Id,
            user.Email,
            user.CreatedAt,
            user.TwoFactorEnabled,
            isPrivileged,
            RequiresMfaEnrollment: requireMfaForPrivileged &&
                isPrivileged &&
                !user.TwoFactorEnabled,
            groups,
            permissions,
            accessibleOrganizationUnitIds);
}
