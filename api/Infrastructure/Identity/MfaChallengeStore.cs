using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Application.Features.Identity.Abstractions;
using Infrastructure.Caching;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Infrastructure.Identity;

internal sealed class MemoryMfaChallengeStore(Application.Common.Time.IClock clock)
    : IMfaChallengeStore
{
    private readonly ConcurrentDictionary<string, (Guid UserId, DateTimeOffset ExpiresAt)> _tickets =
        new(StringComparer.Ordinal);

    public Task StoreAsync(
        string ticket,
        Guid userId,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _tickets[Hash(ticket)] = (userId, expiresAt);
        return Task.CompletedTask;
    }

    public Task<Guid?> ConsumeAsync(string ticket, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = Hash(ticket);
        if (!_tickets.TryRemove(key, out var entry))
        {
            return Task.FromResult<Guid?>(null);
        }

        return Task.FromResult<Guid?>(
            entry.ExpiresAt > clock.UtcNow ? entry.UserId : null);
    }

    internal static string Hash(string ticket)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(ticket));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

internal sealed class RedisMfaChallengeStore(
    IConnectionMultiplexer multiplexer,
    IOptions<RedisOptions> options,
    Application.Common.Time.IClock clock,
    ILogger<RedisMfaChallengeStore> logger) : IMfaChallengeStore
{
    public async Task StoreAsync(
        string ticket,
        Guid userId,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ttl = expiresAt - clock.UtcNow;
        if (ttl <= TimeSpan.Zero)
        {
            return;
        }

        var key = ChallengeKey(ticket);
        try
        {
            await multiplexer.GetDatabase().StringSetAsync(key, userId.ToString(), ttl);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to store MFA challenge in Redis.");
            throw;
        }
    }

    public async Task<Guid?> ConsumeAsync(string ticket, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = ChallengeKey(ticket);
        try
        {
            var db = multiplexer.GetDatabase();
            var value = await db.StringGetDeleteAsync(key);
            if (!value.HasValue || !Guid.TryParse(value.ToString(), out var userId))
            {
                return null;
            }

            return userId;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to consume MFA challenge from Redis.");
            throw;
        }
    }

    private string ChallengeKey(string ticket) =>
        $"{options.Value.KeyPrefix}mfa:challenge:{MemoryMfaChallengeStore.Hash(ticket)}";
}
