using Application.Common.Pagination;
using Application.Features.Authorization.Abstractions;

namespace Application.Features.Authorization.OrganizationUnits;

public sealed class ListOrganizationUnits(IAuthorizationAdminStore store)
{
    public async Task<Contracts.OrganizationUnitListResponse> HandleAsync(
        PageRequest page,
        bool? isActive,
        CancellationToken cancellationToken)
    {
        var result = await store.ListOrganizationUnitsAsync(page, isActive, cancellationToken);
        return new Contracts.OrganizationUnitListResponse(
            result.Items.Select(unit => unit.ToResponse()).ToArray(),
            result.TotalCount,
            result.Page,
            result.PageSize);
    }
}
