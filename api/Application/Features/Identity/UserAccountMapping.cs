using Application.Features.Identity.Abstractions;
using Application.Features.Identity.Contracts;

namespace Application.Features.Identity;

internal static class UserAccountMapping
{
    public static UserResponse ToResponse(
        this UserAccount user,
        IReadOnlyList<string> groups,
        IReadOnlyList<string> permissions,
        IReadOnlyList<Guid> accessibleOrganizationUnitIds) =>
        new(
            user.Id,
            user.Email,
            user.CreatedAt,
            groups,
            permissions,
            accessibleOrganizationUnitIds);
}
