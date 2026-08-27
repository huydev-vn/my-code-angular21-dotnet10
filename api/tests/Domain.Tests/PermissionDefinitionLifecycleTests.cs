using Domain.Authorization;

namespace Domain.Tests;

public sealed class PermissionDefinitionLifecycleTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 21, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_NormalizesCode_SetsDefaults_AndVersionOne()
    {
        var permission = PermissionDefinition.Create(
            " Users.Read ",
            " View users ",
            " users ",
            " read ",
            PermissionScopeMode.Global,
            Now,
            resource: null,
            PermissionRiskLevel.Medium);

        Assert.Equal("users.read", permission.Code);
        Assert.Equal("View users", permission.Name);
        Assert.Equal("users", permission.Module);
        Assert.Equal("read", permission.Action);
        Assert.Equal("users", permission.Resource);
        Assert.Equal(PermissionScopeMode.Global, permission.ScopeMode);
        Assert.Equal(PermissionRiskLevel.Medium, permission.RiskLevel);
        Assert.False(permission.IsSystemManaged);
        Assert.True(permission.IsActive);
        Assert.Equal(1, permission.Version);
    }

    [Fact]
    public void Create_SystemManaged_PreservesExplicitResourceAndRisk()
    {
        var permission = PermissionDefinition.Create(
            SystemPermissions.AuthorizationGroupsWrite,
            "Manage user groups",
            "authorization",
            "groups.write",
            PermissionScopeMode.Global,
            Now,
            "authorization.groups",
            PermissionRiskLevel.Critical,
            isSystemManaged: true);

        Assert.True(permission.IsSystemManaged);
        Assert.Equal("authorization.groups", permission.Resource);
        Assert.Equal(PermissionRiskLevel.Critical, permission.RiskLevel);
    }

    [Fact]
    public void Update_ChangesMetadata_NotCode_AndBumpsVersion()
    {
        var permission = PermissionDefinition.Create(
            "invoice.read",
            "View invoices",
            "invoice",
            "read",
            PermissionScopeMode.OrganizationUnit,
            Now,
            "invoice",
            PermissionRiskLevel.Low);

        permission.Update(
            "View invoices (all)",
            "billing",
            "read",
            "invoice",
            PermissionScopeMode.Global,
            PermissionRiskLevel.Medium);

        Assert.Equal("invoice.read", permission.Code);
        Assert.Equal("View invoices (all)", permission.Name);
        Assert.Equal("billing", permission.Module);
        Assert.Equal("invoice", permission.Resource);
        Assert.Equal(PermissionScopeMode.Global, permission.ScopeMode);
        Assert.Equal(PermissionRiskLevel.Medium, permission.RiskLevel);
        Assert.Equal(2, permission.Version);
        Assert.False(permission.IsSystemManaged);
    }

    [Fact]
    public void Update_SystemManaged_AllowsMetadata_KeepsCodeAndFlag()
    {
        var permission = PermissionDefinition.Create(
            SystemPermissions.UsersRead,
            "View users",
            "users",
            "read",
            PermissionScopeMode.Global,
            Now,
            "users",
            PermissionRiskLevel.Medium,
            isSystemManaged: true);

        permission.Update(
            "View users (renamed)",
            "users",
            "read",
            "users",
            PermissionScopeMode.OrganizationUnit,
            PermissionRiskLevel.High);

        Assert.Equal(SystemPermissions.UsersRead, permission.Code);
        Assert.True(permission.IsSystemManaged);
        Assert.Equal("View users (renamed)", permission.Name);
        Assert.Equal(PermissionScopeMode.OrganizationUnit, permission.ScopeMode);
        Assert.Equal(PermissionRiskLevel.High, permission.RiskLevel);
        Assert.Equal(2, permission.Version);
    }

    [Fact]
    public void Deactivate_and_Activate_toggle_IsActive_and_bump_Version()
    {
        var permission = PermissionDefinition.Create(
            "users.read",
            "View users",
            "users",
            "read",
            PermissionScopeMode.Global,
            Now);

        Assert.True(permission.IsActive);
        Assert.Equal(1, permission.Version);

        permission.Deactivate();
        Assert.False(permission.IsActive);
        Assert.Equal(2, permission.Version);

        permission.Activate();
        Assert.True(permission.IsActive);
        Assert.Equal(3, permission.Version);
    }

    [Fact]
    public void IsPrivilegedCatalogPermission_IncludesCriticalRiskLevel()
    {
        var criticalCustom = PermissionDefinition.Create(
            "billing.approve",
            "Approve billing",
            "billing",
            "approve",
            PermissionScopeMode.OrganizationUnit,
            Now,
            "billing",
            PermissionRiskLevel.Critical);

        Assert.True(SystemPermissions.IsPrivilegedCatalogPermission(criticalCustom));
        Assert.False(SystemPermissions.IsPrivilegedCatalogPermission(criticalCustom.Code));
        Assert.True(
            SystemPermissions.IsPrivilegedCatalogPermission(
                SystemPermissions.AuthorizationGroupsWrite));
        Assert.False(
            SystemPermissions.IsPrivilegedCatalogPermission(SystemPermissions.UsersRead));
    }
}
