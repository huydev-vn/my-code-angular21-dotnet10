using Application.Features.Identity.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace Api.Authorization;

/// <summary>
/// Requires a permission. When the permission's catalog ScopeMode is OrganizationUnit,
/// also requires access to the organization unit identified by the named route/query value.
/// Global/None ScopeMode succeeds on the permission grant alone (OU param ignored).
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
