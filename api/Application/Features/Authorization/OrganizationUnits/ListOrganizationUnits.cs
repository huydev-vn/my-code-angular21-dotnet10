using Application.Features.Authorization.Abstractions;

namespace Application.Features.Authorization.OrganizationUnits;

public sealed class ListOrganizationUnits(IAuthorizationAdminStore store)
{
    public async Task<Contracts.OrganizationUnitListResponse> HandleAsync(
        CancellationToken cancellationToken)
    {
        var units = await store.ListOrganizationUnitsAsync(cancellationToken);
        return new Contracts.OrganizationUnitListResponse(
            units.Select(unit => unit.ToResponse()).ToArray());
    }
}
