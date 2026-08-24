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
    private string VersionKey => $"{options.Value.KeyPrefix}authz:version";

    public async Task<long?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var value = await multiplexer.GetDatabase().StringGetAsync(VersionKey);
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
            await multiplexer.GetDatabase().StringIncrementAsync(VersionKey);
        }
        catch (Exception exception)
        {
            // PostgreSQL already committed; fail closed on subsequent cache reads by
            // not updating the shared version is worse than logging — rethrow so the
            // request surfaces as an error and operators notice Redis outage.
            logger.LogError(exception, "Redis authorization version bump failed after commit.");
            throw;
        }
    }
}
