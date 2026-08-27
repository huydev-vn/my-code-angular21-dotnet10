using Application.Common.Results;
using Application.Features.Authorization.Abstractions;
using Application.Features.Authorization.Contracts;

namespace Application.Features.Authorization.GetContext;

/// <summary>
/// Builds a UI-oriented capability payload from effective permissions + catalog metadata
/// and separate user↔OU membership rows. Does not put the matrix in JWT.
/// </summary>
public sealed class GetUserCapabilities(
    IAuthorizationDecisionService decisionService,
    IAuthorizationAdminStore store)
{
    public async Task<Result<UserCapabilitiesResponse>> HandleAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var context = await decisionService.GetContextAsync(userId, cancellationToken);
        var groups = context?.GroupNames ?? [];
        var permissionCodes = context?.PermissionCodes ?? [];
        var accessibleUnitIds = context?.AccessibleOrganizationUnitIds ?? [];

        var catalog = await store.ListActivePermissionsByCodesAsync(
            permissionCodes,
            cancellationToken);
        // Only return metadata for codes the user actually holds (join catalog ∩ grants).
        var grantedSet = permissionCodes.ToHashSet(StringComparer.Ordinal);
        var permissions = catalog
            .Where(permission => grantedSet.Contains(permission.Code))
            .Select(permission => permission.ToCapabilityResponse())
            .ToArray();

        var memberships = await store.ListUserOrganizationUnitsAsync(
            userId,
            activeOnly: true,
            cancellationToken);

        return Result<UserCapabilitiesResponse>.Success(
            new UserCapabilitiesResponse(
                userId,
                groups,
                permissions,
                accessibleUnitIds,
                memberships.Select(item => item.ToResponse()).ToArray()));
    }
}
