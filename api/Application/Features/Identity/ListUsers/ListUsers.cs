using Application.Common.Pagination;
using Application.Features.Identity.Abstractions;
using Application.Features.Identity.Contracts;

namespace Application.Features.Identity.ListUsers;

public sealed class ListUsers(IUserAccountService userAccountService)
{
    public async Task<UserListResponse> HandleAsync(
        PageRequest page,
        CancellationToken cancellationToken)
    {
        var result = await userAccountService.ListAsync(page, cancellationToken);
        return new UserListResponse(
            result.Items.Select(user =>
                    user.ToResponse(
                        isPrivileged: false,
                        requireMfaForPrivileged: false,
                        [],
                        [],
                        []))
                .ToArray(),
            result.TotalCount,
            result.Page,
            result.PageSize);
    }
}
