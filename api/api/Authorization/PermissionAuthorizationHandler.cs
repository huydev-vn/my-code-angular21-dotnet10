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

/// <summary>Succeeds when the caller holds any one of the listed permissions.</summary>
internal sealed class AnyPermissionRequirement(IReadOnlyList<string> permissions)
    : Microsoft.AspNetCore.Authorization.IAuthorizationRequirement
{
    public IReadOnlyList<string> Permissions { get; } = permissions;
}

/// <summary>
/// Resolves permission (and optional organization-unit) checks from the database
/// instead of JWT claims so revocations take effect immediately.
/// Respects catalog <c>ScopeMode</c>: Global/None ignore route OU; OrganizationUnit requires it.
/// </summary>
internal sealed class PermissionAuthorizationHandler(
    IAuthorizationDecisionService decisionService,
    IAuthorizationScopeService scopeService,
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
            Guid? organizationUnitId = null;
            if (httpContext is not null &&
                TryGetOrganizationUnitId(
                    httpContext,
                    requirement.OrganizationUnitRouteKey,
                    out var parsedUnitId))
            {
                organizationUnitId = parsedUnitId;
            }

            var scopedDecision = await scopeService.AuthorizePermissionWithOptionalUnitAsync(
                userId.Value,
                requirement.Permission,
                organizationUnitId,
                cancellationToken);

            if (scopedDecision.IsAllowed)
            {
                context.Succeed(requirement);
                return;
            }

            logger.LogWarning(
                "Authorization denied for user {UserId} on permission {Permission} for unit {UnitId}. Reason: {Reason}",
                userId,
                requirement.Permission,
                organizationUnitId,
                scopedDecision.Reason);
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

/// <summary>
/// OR-check across permission codes for two-tier admin routes
/// (e.g. <c>authorization.groups.write</c> OR <c>authorization.assignments.delegate</c>).
/// </summary>
internal sealed class AnyPermissionAuthorizationHandler(
    IAuthorizationDecisionService decisionService,
    IHttpContextAccessor httpContextAccessor,
    ILogger<AnyPermissionAuthorizationHandler> logger)
    : Microsoft.AspNetCore.Authorization.AuthorizationHandler<AnyPermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        Microsoft.AspNetCore.Authorization.AuthorizationHandlerContext context,
        AnyPermissionRequirement requirement)
    {
        var userId = context.User.GetUserId();
        if (userId is null)
        {
            return;
        }

        var cancellationToken =
            httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;

        foreach (var permission in requirement.Permissions)
        {
            var decision = await decisionService.HasPermissionAsync(
                userId.Value,
                permission,
                cancellationToken);
            if (decision.IsAllowed)
            {
                context.Succeed(requirement);
                return;
            }
        }

        logger.LogWarning(
            "Authorization denied for user {UserId}; none of permissions [{Permissions}] granted.",
            userId,
            string.Join(", ", requirement.Permissions));
    }
}
