using Application.Common.Pagination;
using Application.Features.Authorization.Abstractions;

namespace Application.Features.Authorization.Groups;

public sealed class ListUserGroups(IAuthorizationAdminStore store)
{
    public async Task<Contracts.UserGroupListResponse> HandleAsync(
        PageRequest page,
        bool? isActive,
        CancellationToken cancellationToken)
    {
        var result = await store.ListGroupsAsync(page, isActive, cancellationToken);
        return new Contracts.UserGroupListResponse(
            result.Items.Select(group => group.ToResponse()).ToArray(),
            result.TotalCount,
            result.Page,
            result.PageSize);
    }
}
