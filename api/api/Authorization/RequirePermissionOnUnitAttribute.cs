using Application.Features.Identity.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace Api.Authorization;

/// <summary>
/// Requires a permission and that the caller can access the organization unit
/// identified by the named route value.
/// </summary>
internal sealed class RequirePermissionOnUnitAttribute : AuthorizeAttribute
{
    public RequirePermissionOnUnitAttribute(
        string permission,
        string routeKey = PermissionPolicies.DefaultOrganizationUnitRouteKey)
        : base(PermissionPolicies.ForUnit(permission, routeKey))
    {
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme;
    }
}
