using Application.Common.Results;
using Application.Features.Authorization.Abstractions;
using Application.Features.Authorization.Errors;

namespace Application.Features.Authorization.Permissions;

/// <summary>Returns one permission catalog entry by id.</summary>
public sealed class GetPermissionDefinition(IAuthorizationAdminStore store)
{
    public async Task<Result<Contracts.PermissionDefinitionResponse>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var permission = await store.FindPermissionByIdAsync(id, cancellationToken);
        return permission is null
            ? Result<Contracts.PermissionDefinitionResponse>.Failure(
                AuthorizationErrors.PermissionNotFound)
            : Result<Contracts.PermissionDefinitionResponse>.Success(permission.ToResponse());
    }
}
