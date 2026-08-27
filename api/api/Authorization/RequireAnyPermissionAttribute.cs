using Application.Features.Identity.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace Api.Authorization;

/// <summary>
/// Requires an authenticated caller who holds at least one of the named permissions.
/// Used for two-tier admin endpoints (system Critical write OR regional delegate).
/// </summary>
internal sealed class RequireAnyPermissionAttribute : AuthorizeAttribute
{
    public RequireAnyPermissionAttribute(params string[] permissions)
        : base(PermissionPolicies.Any(permissions))
    {
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme;
    }
}
