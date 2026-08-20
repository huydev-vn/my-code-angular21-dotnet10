using Application.Common.Time;
using Application.Features.Authorization.Abstractions;
using Domain.Authorization;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Authorization;

public static class AuthorizationSeeder
{
    public const string SystemAdministratorsGroupName = "System Administrators";

    public static async Task SeedAsync(
        IServiceProvider services,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var dbContext = services.GetRequiredService<AppDbContext>();
        var clock = services.GetRequiredService<IClock>();
        var logger = services.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(AuthorizationSeeder));

        await SeedPermissionsAsync(dbContext, clock, cancellationToken);
        var adminGroup = await SeedSystemAdministratorsGroupAsync(
            dbContext,
            clock,
            cancellationToken);
        await SeedAdminMembershipAsync(
            services,
            configuration,
            dbContext,
            clock,
            adminGroup.Id,
            logger,
            cancellationToken);
    }

    private static async Task SeedPermissionsAsync(
        AppDbContext dbContext,
        IClock clock,
        CancellationToken cancellationToken)
    {
        var existingCodes = await dbContext.PermissionDefinitions
            .AsNoTracking()
            .Select(permission => permission.Code)
            .ToListAsync(cancellationToken);
        var existing = existingCodes.ToHashSet(StringComparer.Ordinal);

        foreach (var (code, name, module, action) in SystemPermissions.All)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (existing.Contains(code))
            {
                continue;
            }

            dbContext.PermissionDefinitions.Add(
                PermissionDefinition.Create(code, name, module, action, clock.UtcNow));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task<UserGroup> SeedSystemAdministratorsGroupAsync(
        AppDbContext dbContext,
        IClock clock,
        CancellationToken cancellationToken)
    {
        var group = await dbContext.UserGroups
            .FirstOrDefaultAsync(
                candidate => candidate.Name == SystemAdministratorsGroupName,
                cancellationToken);

        if (group is null)
        {
            group = UserGroup.Create(
                SystemAdministratorsGroupName,
                "Full access to authorization administration.",
                clock.UtcNow);
            dbContext.UserGroups.Add(group);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var permissionIds = await dbContext.PermissionDefinitions
            .AsNoTracking()
            .Where(permission => permission.IsActive)
            .Select(permission => permission.Id)
            .ToListAsync(cancellationToken);

        var existingAssignments = await dbContext.GroupPermissions
            .AsNoTracking()
            .Where(assignment => assignment.GroupId == group.Id)
            .Select(assignment => assignment.PermissionId)
            .ToListAsync(cancellationToken);
        var assigned = existingAssignments.ToHashSet();

        foreach (var permissionId in permissionIds)
        {
            if (assigned.Contains(permissionId))
            {
                continue;
            }

            dbContext.GroupPermissions.Add(
                GroupPermission.Create(group.Id, permissionId, clock.UtcNow));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return group;
    }

    private static async Task SeedAdminMembershipAsync(
        IServiceProvider services,
        IConfiguration configuration,
        AppDbContext dbContext,
        IClock clock,
        Guid adminGroupId,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var adminEmail = configuration["Identity:SeedAdmin:Email"];
        if (string.IsNullOrWhiteSpace(adminEmail))
        {
            return;
        }

        var adminUser = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Email == adminEmail, cancellationToken);

        if (adminUser is null)
        {
            return;
        }

        var exists = await dbContext.UserGroupMemberships
            .AsNoTracking()
            .AnyAsync(
                membership =>
                    membership.UserId == adminUser.Id &&
                    membership.GroupId == adminGroupId,
                cancellationToken);

        if (exists)
        {
            return;
        }

        dbContext.UserGroupMemberships.Add(
            UserGroupMembership.Create(adminUser.Id, adminGroupId, clock.UtcNow));
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Assigned seed admin user to the {GroupName} group.",
            SystemAdministratorsGroupName);
    }
}
