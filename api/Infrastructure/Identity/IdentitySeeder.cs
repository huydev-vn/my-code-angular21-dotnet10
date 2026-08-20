using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Identity;

public static class IdentitySeeder
{
    public static async Task SeedAsync(
        IServiceProvider services,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = services.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(IdentitySeeder));

        var adminEmail = configuration["Identity:SeedAdmin:Email"];
        var adminPassword = configuration["Identity:SeedAdmin:Password"];
        if (string.IsNullOrWhiteSpace(adminEmail) ||
            string.IsNullOrWhiteSpace(adminPassword))
        {
            return;
        }

        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is not null)
        {
            return;
        }

        admin = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var createAdmin = await userManager.CreateAsync(admin, adminPassword);
        if (!createAdmin.Succeeded)
        {
            logger.LogWarning("Configured seed admin could not be created.");
            return;
        }

        logger.LogInformation("Seeded the configured admin user.");
    }
}
