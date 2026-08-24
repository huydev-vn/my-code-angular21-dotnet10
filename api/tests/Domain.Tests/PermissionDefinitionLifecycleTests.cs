using Domain.Authorization;

namespace Domain.Tests;

public sealed class PermissionDefinitionLifecycleTests
{
    [Fact]
    public void Deactivate_and_Activate_toggle_IsActive_and_bump_Version()
    {
        var permission = PermissionDefinition.Create(
            "users.read",
            "View users",
            "users",
            "read",
            new DateTimeOffset(2026, 8, 21, 0, 0, 0, TimeSpan.Zero));

        Assert.True(permission.IsActive);
        Assert.Equal(1, permission.Version);

        permission.Deactivate();
        Assert.False(permission.IsActive);
        Assert.Equal(2, permission.Version);

        permission.Activate();
        Assert.True(permission.IsActive);
        Assert.Equal(3, permission.Version);
    }
}
