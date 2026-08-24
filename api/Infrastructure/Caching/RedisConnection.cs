using Microsoft.Extensions.Configuration;

namespace Infrastructure.Caching;

/// <summary>
/// Resolves Redis connection settings. Prefer <c>Redis:ConnectionString</c>;
/// fall back to <c>ConnectionStrings:Redis</c> when the Redis section value is blank.
/// </summary>
public static class RedisConnection
{
    public static string? ResolveConnectionString(IConfiguration configuration)
    {
        var fromSection = configuration[$"{RedisOptions.SectionName}:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(fromSection))
        {
            return fromSection.Trim();
        }

        var fromConnectionStrings = configuration.GetConnectionString("Redis");
        return string.IsNullOrWhiteSpace(fromConnectionStrings)
            ? null
            : fromConnectionStrings.Trim();
    }
}
