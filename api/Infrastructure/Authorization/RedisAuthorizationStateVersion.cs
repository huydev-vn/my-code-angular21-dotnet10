using Application.Features.Authorization.Abstractions;
using Infrastructure.Caching;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Infrastructure.Authorization;

/// <summary>Shared authorization generation counter stored in Redis.</summary>
internal sealed class RedisAuthorizationStateVersion(
    IConnectionMultiplexer multiplexer,
    IOptions<RedisOptions> options,
    ILogger<RedisAuthorizationStateVersion> logger) : IAuthorizationStateVersion
{
    /// <summary>
    /// Matches <see cref="AuthorizationDecisionService"/> max cache TTL so a failed
    /// bump forces PostgreSQL reads until any stale distributed entries expire.
    /// </summary>
    private static readonly TimeSpan CacheBypassTtl = TimeSpan.FromSeconds(300);

    private string VersionKey => $"{options.Value.KeyPrefix}authz:version";

    private string CacheBypassKey => $"{options.Value.KeyPrefix}authz:cache-bypass";

    public async Task<long?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var db = multiplexer.GetDatabase();
            if (await db.KeyExistsAsync(CacheBypassKey))
            {
                logger.LogWarning(
                    "Authorization cache bypass is active; loading contexts from PostgreSQL.");
                return null;
            }

            var value = await db.StringGetAsync(VersionKey);
            return value.HasValue ? (long)value : 0L;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Redis authorization version read failed; callers must bypass distributed cache.");
            return null;
        }
    }

    public async Task BumpAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var db = multiplexer.GetDatabase();
            await db.StringIncrementAsync(VersionKey);
            // Clear any prior bypass so replicas can resume versioned caching.
            await db.KeyDeleteAsync(CacheBypassKey);
        }
        catch (Exception exception)
        {
            // PostgreSQL already committed. Mark cache untrusted so subsequent
            // GetCurrentAsync returns null and DecisionService skips stale Redis entries.
            await TryActivateCacheBypassAsync();
            logger.LogError(exception, "Redis authorization version bump failed after commit.");
            throw;
        }
    }

    private async Task TryActivateCacheBypassAsync()
    {
        try
        {
            await multiplexer.GetDatabase().StringSetAsync(
                CacheBypassKey,
                "1",
                CacheBypassTtl);
        }
        catch (Exception bypassException)
        {
            logger.LogError(
                bypassException,
                "Failed to activate authorization cache bypass after bump failure.");
        }
    }
}
