using Application.Common.Results;
using Application.Features.Authorization.Abstractions;
using Application.Features.Identity.Abstractions;
using Application.Features.Identity.Contracts;
using Application.Features.Identity.Errors;

namespace Application.Features.Identity.GetCurrentUser;

public sealed class GetCurrentUser(
    IUserAccountService userAccountService,
    IAuthorizationDecisionService authorizationDecisionService)
{
    public async Task<Result<UserResponse>> HandleAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await userAccountService.FindByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result<UserResponse>.Failure(IdentityErrors.UserNotFound);
        }

        var authorization = await authorizationDecisionService.GetContextAsync(
            userId,
            cancellationToken);

        return Result<UserResponse>.Success(
            user.ToResponse(
                authorization?.GroupNames ?? [],
                authorization?.PermissionCodes ?? [],
                authorization?.AccessibleOrganizationUnitIds ?? []));
    }
}
