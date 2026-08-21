using Application.Common.Persistence;
using Application.Common.Settings;
using Application.Features.Authorization.Abstractions;
using Application.Features.Identity.Abstractions;
using Infrastructure.Authorization;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionString,
        bool isDevelopment = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddOptions<IdentitySettings>()
            .Bind(configuration.GetSection(IdentitySettings.SectionName));
        services.AddSingleton<IIdentitySettings>(sp =>
            sp.GetRequiredService<IOptions<IdentitySettings>>().Value);

        services.AddSingleton<Application.Common.Time.IClock, SystemClock>();
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(
                connectionString,
                npgsqlOptions => npgsqlOptions.MigrationsAssembly(
                    typeof(AppDbContext).Assembly.FullName));

            if (isDevelopment)
            {
                options.EnableDetailedErrors();
                options.EnableSensitiveDataLogging();
            }
        });

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<AppDbContext>();

        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IUserAccountService, UserAccountService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IRefreshTokenStore, RefreshTokenStore>();
        services.AddScoped<IAuthorizationAdminStore, AuthorizationAdminStore>();
        services.AddScoped<IAuthorizationDecisionService, AuthorizationDecisionService>();
        services.AddScoped<IAuthorizationAuditor, AuthorizationAuditor>();

        return services;
    }
}
