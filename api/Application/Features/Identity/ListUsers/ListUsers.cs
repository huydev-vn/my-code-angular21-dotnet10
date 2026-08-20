using Application.Features.Identity.Abstractions;
using Application.Features.Identity.Contracts;

namespace Application.Features.Identity.ListUsers;

public sealed class ListUsers(IUserAccountService userAccountService)
{
    public async Task<UserListResponse> HandleAsync(CancellationToken cancellationToken)
    {
        var users = await userAccountService.ListAsync(cancellationToken);
        return new UserListResponse(
            users.Select(user => user.ToResponse([], [], [])).ToArray());
    }
}
