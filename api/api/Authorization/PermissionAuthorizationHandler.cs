using Application.Features.Authorization.Abstractions;
using Api.Extensions;

namespace Api.Authorization;

internal sealed class PermissionRequirement(
    string permission,
    string? organizationUnitRouteKey = null)
    : Microsoft.AspNetCore.Authorization.IAuthorizationRequirement
{
    public string Permission { get; } = permission;

    public string? OrganizationUnitRouteKey { get; } = organizationUnitRouteKey;
}

/// <summary>
/// Resolves permission (and optional organization-unit) checks from the database
/// instead of JWT claims so revocations take effect immediately.
/// </summary>
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
        var userId = context.User.GetUserId();
        if (userId is null)
        {
            return;
        }

        var httpContext = httpContextAccessor.HttpContext;
        var cancellationToken = httpContext?.RequestAborted ?? CancellationToken.None;

        if (requirement.OrganizationUnitRouteKey is not null)
        {
            if (httpContext is null ||
                !TryGetOrganizationUnitId(
                    httpContext,
                    requirement.OrganizationUnitRouteKey,
                    out var organizationUnitId))
            {
                logger.LogWarning(
                    "Authorization denied for user {UserId}: missing organization unit route value {RouteKey}.",
                    userId,
                    requirement.OrganizationUnitRouteKey);
                return;
            }

            var unitDecision = await decisionService.HasPermissionOnUnitAsync(
                userId.Value,
                requirement.Permission,
                organizationUnitId,
                cancellationToken);

            if (unitDecision.IsAllowed)
            {
                context.Succeed(requirement);
                return;
            }

            logger.LogWarning(
                "Authorization denied for user {UserId} on permission {Permission} for unit {UnitId}. Reason: {Reason}",
                userId,
                requirement.Permission,
                organizationUnitId,
                unitDecision.Reason);
            return;
        }

        var decision = await decisionService.HasPermissionAsync(
            userId.Value,
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

    private static bool TryGetOrganizationUnitId(
        HttpContext httpContext,
        string routeKey,
        out Guid organizationUnitId)
    {
        if (httpContext.Request.RouteValues.TryGetValue(routeKey, out var routeValue) &&
            Guid.TryParse(routeValue?.ToString(), out organizationUnitId))
        {
            return true;
        }

        if (httpContext.Request.Query.TryGetValue(routeKey, out var queryValue) &&
            Guid.TryParse(queryValue.ToString(), out organizationUnitId))
        {
            return true;
        }

        organizationUnitId = Guid.Empty;
        return false;
    }
}
