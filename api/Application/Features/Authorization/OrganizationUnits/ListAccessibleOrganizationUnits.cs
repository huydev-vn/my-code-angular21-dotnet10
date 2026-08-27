using Application.Common.Pagination;
using Application.Features.Authorization.Abstractions;

namespace Application.Features.Authorization.OrganizationUnits;

/// <summary>
/// Lists organization units within the caller's accessible OU set (fail closed → empty).
/// Admin full-tree listing remains <see cref="ListOrganizationUnits"/>; Agent C can expose this later.
/// </summary>
public sealed class ListAccessibleOrganizationUnits(
    IAuthorizationAdminStore store,
    IAuthorizationScopeService scopeService)
{
    public async Task<Contracts.OrganizationUnitListResponse> HandleAsync(
        Guid userId,
        PageRequest page,
        bool? isActive,
        CancellationToken cancellationToken)
    {
        var accessibleIds = await scopeService.GetAccessibleOrganizationUnitIdsAsync(
            userId,
            cancellationToken);

        if (accessibleIds.Count == 0)
        {
            return new Contracts.OrganizationUnitListResponse(
                [],
                0,
                page.Page,
                page.PageSize);
        }

        var result = await store.ListOrganizationUnitsByIdsAsync(
            page,
            accessibleIds,
            isActive,
            cancellationToken);

        return new Contracts.OrganizationUnitListResponse(
            result.Items.Select(unit => unit.ToResponse()).ToArray(),
            result.TotalCount,
            result.Page,
            result.PageSize);
    }
}
