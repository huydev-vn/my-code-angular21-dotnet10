using Domain.Authorization;
using Domain.Identity;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<PermissionDefinition> PermissionDefinitions => Set<PermissionDefinition>();

    public DbSet<UserGroup> UserGroups => Set<UserGroup>();

    public DbSet<UserGroupMembership> UserGroupMemberships => Set<UserGroupMembership>();

    public DbSet<GroupPermission> GroupPermissions => Set<GroupPermission>();

    public DbSet<OrganizationUnit> OrganizationUnits => Set<OrganizationUnit>();

    public DbSet<GroupOrganizationUnit> GroupOrganizationUnits => Set<GroupOrganizationUnit>();

    public DbSet<UserOrganizationUnit> UserOrganizationUnits => Set<UserOrganizationUnit>();

    public DbSet<AuthorizationAuditEvent> AuthorizationAuditEvents => Set<AuthorizationAuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
