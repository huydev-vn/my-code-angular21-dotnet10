using Infrastructure.Caching;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Infrastructure.RateLimiting;

/// <summary>
/// Shared fixed-window counter for authentication endpoints across API replicas.
/// PostgreSQL is not used; counters are ephemeral abuse-control state only.
/// </summary>
public sealed class RedisAuthRateLimitStore(
    IConnectionMultiplexer multiplexer,
    IOptions<RedisOptions> options,
    ILogger<RedisAuthRateLimitStore> logger)
{
    public async Task<bool> TryAcquireAsync(
        string partitionKey,
        int permitLimit,
        TimeSpan window,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = $"{options.Value.KeyPrefix}ratelimit:auth:{partitionKey}";
        try
        {
            var db = multiplexer.GetDatabase();
            var count = await db.StringIncrementAsync(key);
            if (count == 1)
            {
                await db.KeyExpireAsync(key, window);
            }

            return count <= permitLimit;
        }
        catch (Exception exception)
        {
            // Fail open to the process-local ASP.NET rate limiter rather than
            // blocking all authentication when Redis is briefly unavailable.
            logger.LogWarning(
                exception,
                "Redis auth rate-limit check failed for {PartitionKey}; deferring to local limiter.",
                partitionKey);
            return true;
        }
    }
}
