using Domain.Authorization;

namespace Domain.Tests;

public sealed class UserGroupPrivilegedLifecycleTests
{
    [Fact]
    public void Deactivate_WhenPrivileged_Throws()
    {
        var group = UserGroup.CreatePrivileged(
            "System Administrators",
            null,
            new DateTimeOffset(2026, 8, 24, 0, 0, 0, TimeSpan.Zero));

        Assert.True(group.IsPrivileged);
        Assert.Throws<InvalidOperationException>(() => group.Deactivate());
        Assert.True(group.IsActive);
    }

    [Fact]
    public void MarkPrivileged_PromotesExistingGroup()
    {
        var group = UserGroup.Create(
            "Ops",
            null,
            new DateTimeOffset(2026, 8, 24, 0, 0, 0, TimeSpan.Zero));

        Assert.False(group.IsPrivileged);
        group.MarkPrivileged();
        Assert.True(group.IsPrivileged);
    }

    [Fact]
    public void IsPrivilegedCatalogPermission_MatchesHighRiskWrites()
    {
        Assert.True(
            SystemPermissions.IsPrivilegedCatalogPermission(
                SystemPermissions.AuthorizationGroupsWrite));
        Assert.False(
            SystemPermissions.IsPrivilegedCatalogPermission(SystemPermissions.UsersRead));
    }
}
