using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.Features.Authorization.Abstractions;
using Microsoft.Extensions.Logging;

namespace Api.Authorization;

internal sealed class PermissionRequirement(string permission)
    : Microsoft.AspNetCore.Authorization.IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

internal sealed class PermissionAuthorizationHandler(
    IAuthorizationDecisionService decisionService,
    IHttpContextAccessor httpContextAccessor,
    ILogger<PermissionAuthorizationHandler> logger)
    : Microsoft.AspNetCore.Authorization.AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        Microsoft.AspNetCore.Authorization.AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var userIdValue = context.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return;
        }

        var cancellationToken = httpContextAccessor.HttpContext?.RequestAborted
            ?? CancellationToken.None;

        var decision = await decisionService.HasPermissionAsync(
            userId,
            requirement.Permission,
            cancellationToken);

        if (decision.IsAllowed)
        {
            context.Succeed(requirement);
            return;
        }

        logger.LogWarning(
            "Authorization denied for user {UserId} on permission {Permission}. Reason: {Reason}",
            userId,
            requirement.Permission,
            decision.Reason);
    }
}
