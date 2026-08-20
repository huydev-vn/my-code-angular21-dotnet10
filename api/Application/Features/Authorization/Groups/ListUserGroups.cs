using Application.Features.Authorization.Abstractions;

namespace Application.Features.Authorization.Groups;

public sealed class ListUserGroups(IAuthorizationAdminStore store)
{
    public async Task<Contracts.UserGroupListResponse> HandleAsync(
        CancellationToken cancellationToken)
    {
        var groups = await store.ListGroupsAsync(cancellationToken);
        return new Contracts.UserGroupListResponse(
            groups.Select(group => group.ToResponse()).ToArray());
    }
}
