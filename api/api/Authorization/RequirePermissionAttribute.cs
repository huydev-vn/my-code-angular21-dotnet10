using Application.Features.Identity.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace Api.Authorization;

internal sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permission)
        : base(PermissionPolicies.Name(permission))
    {
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme;
    }
}
