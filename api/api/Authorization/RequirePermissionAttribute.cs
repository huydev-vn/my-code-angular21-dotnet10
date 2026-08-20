using Application.Features.Identity.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace Api.Authorization;

/// <summary>Requires an authenticated caller with the named permission.</summary>
internal sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permission)
        : base(PermissionPolicies.Name(permission))
    {
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme;
    }
}
