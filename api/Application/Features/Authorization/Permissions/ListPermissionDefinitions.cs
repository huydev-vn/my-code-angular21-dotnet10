using Application.Common.Pagination;
using Application.Features.Authorization.Abstractions;

namespace Application.Features.Authorization.Permissions;

public sealed class ListPermissionDefinitions(IAuthorizationAdminStore store)
{
    public async Task<Contracts.PermissionDefinitionListResponse> HandleAsync(
        PageRequest page,
        bool? isActive,
        CancellationToken cancellationToken)
    {
        var result = await store.ListPermissionsAsync(page, isActive, cancellationToken);
        return new Contracts.PermissionDefinitionListResponse(
            result.Items.Select(permission => permission.ToResponse()).ToArray(),
            result.TotalCount,
            result.Page,
            result.PageSize);
    }
}
