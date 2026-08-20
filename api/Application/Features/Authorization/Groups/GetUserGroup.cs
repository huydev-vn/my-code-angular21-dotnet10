using Application.Common.Results;
using Application.Features.Authorization.Abstractions;
using Application.Features.Authorization.Errors;

namespace Application.Features.Authorization.Groups;

/// <summary>Returns one user group by id.</summary>
public sealed class GetUserGroup(IAuthorizationAdminStore store)
{
    public async Task<Result<Contracts.UserGroupResponse>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var group = await store.FindGroupByIdAsync(id, cancellationToken);
        return group is null
            ? Result<Contracts.UserGroupResponse>.Failure(AuthorizationErrors.GroupNotFound)
            : Result<Contracts.UserGroupResponse>.Success(group.ToResponse());
    }
}
