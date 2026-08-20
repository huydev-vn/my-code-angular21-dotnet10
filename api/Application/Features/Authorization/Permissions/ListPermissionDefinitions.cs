using Application.Features.Authorization.Abstractions;

namespace Application.Features.Authorization.Permissions;

public sealed class ListPermissionDefinitions(IAuthorizationAdminStore store)
{
    public async Task<Contracts.PermissionDefinitionListResponse> HandleAsync(
        CancellationToken cancellationToken)
    {
        var permissions = await store.ListPermissionsAsync(cancellationToken);
        return new Contracts.PermissionDefinitionListResponse(
            permissions.Select(permission => permission.ToResponse()).ToArray());
    }
}
