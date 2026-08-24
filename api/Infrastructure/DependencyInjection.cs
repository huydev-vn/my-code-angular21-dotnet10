using Infrastructure.Authorization;
using Infrastructure.Caching;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Infrastructure.RateLimiting;
using Infrastructure.Time;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

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
        services.AddSingleton<Application.Common.Settings.IIdentitySettings>(sp =>
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
            // AspNetRoles tables remain for Identity schema compatibility only.
            // Authorization uses UserGroup + PermissionDefinition — never RoleManager policies.
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddOptions<AuthorizationCacheOptions>()
            .Bind(configuration.GetSection(AuthorizationCacheOptions.SectionName));
        services.AddOptions<RedisOptions>()
            .Bind(configuration.GetSection(RedisOptions.SectionName))
            .Validate(
                options => options.HasValidTimeouts,
                "Redis:ConnectTimeoutMs and Redis:OperationTimeoutMs must be between 500 and 30000.")
            .Validate(
                options => options.HasValidKeyPrefix,
                "Redis:KeyPrefix must be a non-empty value without spaces.")
            .ValidateOnStart();

        AddRedisOrMemoryCaching(services, configuration, isDevelopment);

        services.AddSingleton<Application.Features.Identity.Abstractions.IAuthMetrics, AuthMetrics>();

        services.AddScoped<Application.Common.Persistence.IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<Application.Features.Identity.Abstractions.IUserAccountService, UserAccountService>();
        services.AddScoped<Application.Features.Identity.Abstractions.ITokenService, TokenService>();
        services.AddScoped<Application.Features.Identity.Abstractions.IRefreshTokenStore, RefreshTokenStore>();
        services.AddHostedService<RefreshTokenCleanupHostedService>();
        services.AddScoped<Application.Features.Authorization.Abstractions.IAuthorizationAdminStore, AuthorizationAdminStore>();
        services.AddScoped<Application.Features.Authorization.Abstractions.IAuthorizationDecisionService, AuthorizationDecisionService>();
        services.AddScoped<Application.Features.Authorization.Abstractions.IAuthorizationAuditor, AuthorizationAuditor>();

        return services;
    }

    private static void AddRedisOrMemoryCaching(
        IServiceCollection services,
        IConfiguration configuration,
        bool isDevelopment)
    {
        var redisConnection = RedisConnection.ResolveConnectionString(configuration);

        if (string.IsNullOrWhiteSpace(redisConnection))
        {
            if (!isDevelopment)
            {
                throw new InvalidOperationException(
                    "Redis:ConnectionString (or ConnectionStrings:Redis) is required outside Development. " +
                    "Redis holds shared authorization version/cache and distributed auth rate limits; " +
                    "PostgreSQL remains the source of truth for identity and permissions.");
            }

            services.AddDistributedMemoryCache();
            services.AddMemoryCache(options => options.SizeLimit = 2048);
            services.AddSingleton<Application.Features.Authorization.Abstractions.IAuthorizationStateVersion, MemoryAuthorizationStateVersion>();
            services.AddSingleton<Application.Features.Identity.Abstractions.IMfaChallengeStore, MemoryMfaChallengeStore>();
            return;
        }

        var redisOptions = configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>()
            ?? new RedisOptions();
        var keyPrefix = string.IsNullOrWhiteSpace(redisOptions.KeyPrefix)
            ? "net10:"
            : redisOptions.KeyPrefix;

        var configurationOptions = ConfigurationOptions.Parse(redisConnection);
        configurationOptions.AbortOnConnectFail = isDevelopment
            ? false
            : redisOptions.AbortOnConnectFail;
        configurationOptions.ConnectTimeout = Math.Clamp(redisOptions.ConnectTimeoutMs, 500, 30_000);
        configurationOptions.SyncTimeout = Math.Clamp(redisOptions.OperationTimeoutMs, 500, 30_000);
        configurationOptions.AsyncTimeout = Math.Clamp(redisOptions.OperationTimeoutMs, 500, 30_000);

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(configurationOptions));

        services.AddStackExchangeRedisCache(options =>
        {
            options.ConfigurationOptions = configurationOptions;
            options.InstanceName = keyPrefix;
        });

        services.AddSingleton<Application.Features.Authorization.Abstractions.IAuthorizationStateVersion, RedisAuthorizationStateVersion>();
        services.AddSingleton<RedisAuthRateLimitStore>();
        services.AddSingleton<Application.Features.Identity.Abstractions.IMfaChallengeStore, RedisMfaChallengeStore>();
        services.AddHealthChecks()
            .AddCheck<RedisHealthCheck>("redis", failureStatus: HealthStatus.Unhealthy, tags: ["ready"]);
    }
}
