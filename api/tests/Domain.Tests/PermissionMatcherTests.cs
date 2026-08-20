using Domain.Authorization;

namespace Domain.Tests;

public sealed class PermissionMatcherTests
{
    [Fact]
    public void Grants_matches_normalized_codes_ordinally()
    {
        var granted = new[] { "users.read", "authorization.groups.write" };

        Assert.True(PermissionMatcher.Grants(granted, " Users.Read "));
        Assert.False(PermissionMatcher.Grants(granted, "users.write"));
        Assert.Equal("users.read", PermissionMatcher.Normalize(" Users.Read "));
    }
}
