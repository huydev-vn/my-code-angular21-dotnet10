using Application.Common.Results;
using Application.Features.Authorization.Abstractions;
using Application.Features.Authorization.Errors;

namespace Application.Features.Authorization.OrganizationUnits;

/// <summary>Returns one organization unit by id.</summary>
public sealed class GetOrganizationUnit(IAuthorizationAdminStore store)
{
    public async Task<Result<Contracts.OrganizationUnitResponse>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var unit = await store.FindOrganizationUnitByIdAsync(id, cancellationToken);
        return unit is null
            ? Result<Contracts.OrganizationUnitResponse>.Failure(
                AuthorizationErrors.OrganizationUnitNotFound)
            : Result<Contracts.OrganizationUnitResponse>.Success(unit.ToResponse());
    }
}
