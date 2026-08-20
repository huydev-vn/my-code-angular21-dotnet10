using Application.Common.Results;
using Application.Features.Authorization.Abstractions;

namespace Application.Features.Authorization.GetContext;

public sealed class GetUserAuthorizationContext(IAuthorizationDecisionService decisionService)
{
    public async Task<Result<Contracts.UserAuthorizationContextResponse>> HandleAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var context = await decisionService.GetContextAsync(userId, cancellationToken);
        if (context is null)
        {
            return Result<Contracts.UserAuthorizationContextResponse>.Success(
                new Contracts.UserAuthorizationContextResponse(userId, [], [], []));
        }

        return Result<Contracts.UserAuthorizationContextResponse>.Success(context.ToResponse());
    }
}
