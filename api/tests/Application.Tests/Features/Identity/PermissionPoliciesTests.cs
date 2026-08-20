using Application.Features.Identity.Authorization;

namespace Application.Tests.Features.Identity;

public sealed class PermissionPoliciesTests
{
    [Fact]
    public void Name_PrefixesPermission()
    {
        Assert.Equal("permission:users.read", PermissionPolicies.Name("users.read"));
    }

    [Fact]
    public void ForUnit_EncodesPermissionAndRouteKey()
    {
        var policy = PermissionPolicies.ForUnit("invoice.read", "organizationUnitId");

        Assert.True(
            PermissionPolicies.TryParseUnitPolicy(policy, out var permission, out var routeKey));
        Assert.Equal("invoice.read", permission);
        Assert.Equal("organizationUnitId", routeKey);
    }

    [Fact]
    public void TryParseUnitPolicy_RejectsPermissionOnlyPolicies()
    {
        Assert.False(
            PermissionPolicies.TryParseUnitPolicy(
                PermissionPolicies.Name("users.read"),
                out _,
                out _));
    }
}
