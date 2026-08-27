using Application.Features.Identity.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Api.Authorization;

/// <summary>Builds JWT permission policies on demand from attribute policy names.</summary>
internal sealed class PermissionAuthorizationPolicyProvider(
    IOptions<AuthorizationOptions> options) : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback = new(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() =>
        _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() =>
        _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (PermissionPolicies.TryParseUnitPolicy(policyName, out var unitPermission, out var routeKey))
        {
            return Task.FromResult<AuthorizationPolicy?>(
                BuildPolicy(new PermissionRequirement(unitPermission, routeKey)));
        }

        if (PermissionPolicies.TryParseAnyPolicy(policyName, out var anyPermissions))
        {
            return Task.FromResult<AuthorizationPolicy?>(
                BuildAnyPolicy(new AnyPermissionRequirement(anyPermissions)));
        }

        if (policyName.StartsWith(PermissionPolicies.Prefix, StringComparison.Ordinal))
        {
            var permission = policyName[PermissionPolicies.Prefix.Length..];
            return Task.FromResult<AuthorizationPolicy?>(
                BuildPolicy(new PermissionRequirement(permission)));
        }

        return _fallback.GetPolicyAsync(policyName);
    }

    private static AuthorizationPolicy BuildPolicy(PermissionRequirement requirement) =>
        new AuthorizationPolicyBuilder()
            .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser()
            .AddRequirements(requirement)
            .Build();

    private static AuthorizationPolicy BuildAnyPolicy(AnyPermissionRequirement requirement) =>
        new AuthorizationPolicyBuilder()
            .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser()
            .AddRequirements(requirement)
            .Build();
}
