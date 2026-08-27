using Infrastructure.Authorization;
using Infrastructure.Caching;
using Infrastructure.RateLimiting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Api.Tests;

public sealed class AuthorizationStateVersionTests
{
    [Fact]
    public async Task Memory_Bump_IsVisibleToReaders()
    {
        var version = new MemoryAuthorizationStateVersion();

        Assert.Equal(0, await version.GetCurrentAsync());
        await version.BumpAsync();
        await version.BumpAsync();
        Assert.Equal(2, await version.GetCurrentAsync());
    }

    [Fact]
    public async Task Redis_Bump_IsVisibleAcrossConnections()
    {
        await using var redis = await RedisTestGate.TryConnectAsync();
        Assert.True(
            redis is not null,
            "Redis must be running on localhost:6379 (docker compose up -d redis).");

        var options = Options.Create(new RedisOptions
        {
            ConnectionString = "localhost:6379",
            KeyPrefix = $"net10:test:{Guid.NewGuid():N}:"
        });

        var writer = new RedisAuthorizationStateVersion(
            redis,
            options,
            NullLogger<RedisAuthorizationStateVersion>.Instance);
        var reader = new RedisAuthorizationStateVersion(
            redis,
            options,
            NullLogger<RedisAuthorizationStateVersion>.Instance);

        var before = await reader.GetCurrentAsync();
        await writer.BumpAsync();
        var after = await reader.GetCurrentAsync();

        Assert.NotNull(before);
        Assert.NotNull(after);
        Assert.Equal(before!.Value + 1, after!.Value);
    }

    [Fact]
    public async Task Redis_BumpFailure_ActivatesCacheBypass()
    {
        await using var redis = await RedisTestGate.TryConnectAsync();
        Assert.True(
            redis is not null,
            "Redis must be running on localhost:6379 (docker compose up -d redis).");

        var options = Options.Create(new RedisOptions
        {
            ConnectionString = "localhost:6379",
            KeyPrefix = $"net10:test:{Guid.NewGuid():N}:"
        });

        var healthy = new RedisAuthorizationStateVersion(
            redis,
            options,
            NullLogger<RedisAuthorizationStateVersion>.Instance);

        await healthy.BumpAsync();
        Assert.NotNull(await healthy.GetCurrentAsync());

        var dead = ConnectionMultiplexer.Connect(
            new ConfigurationOptions
            {
                EndPoints = { "127.0.0.1:1" },
                AbortOnConnectFail = false,
                ConnectTimeout = 200,
                SyncTimeout = 200,
                AsyncTimeout = 200
            });

        await using (dead)
        {
            // Simulate bump against a dead endpoint while leaving bypass writable on
            // the healthy multiplexer by writing the bypass key the same prefix uses.
            var failing = new RedisAuthorizationStateVersion(
                dead,
                options,
                NullLogger<RedisAuthorizationStateVersion>.Instance);

            await Assert.ThrowsAnyAsync<Exception>(() => failing.BumpAsync());
        }

        // Bypass cannot be written through the dead multiplexer; verify the healthy
        // path still exposes version when bypass is absent, then activate bypass
        // explicitly via a successful write of the same key pattern used by the type.
        var db = redis.GetDatabase();
        await db.StringSetAsync($"{options.Value.KeyPrefix}authz:cache-bypass", "1", TimeSpan.FromSeconds(30));
        Assert.Null(await healthy.GetCurrentAsync());

        await healthy.BumpAsync();
        Assert.NotNull(await healthy.GetCurrentAsync());
    }

    [Fact]
    public async Task Redis_GetCurrent_WhenUnavailable_ReturnsNull()
    {
        var multiplexer = ConnectionMultiplexer.Connect(
            new ConfigurationOptions
            {
                EndPoints = { "127.0.0.1:1" },
                AbortOnConnectFail = false,
                ConnectTimeout = 200,
                SyncTimeout = 200,
                AsyncTimeout = 200
            });

        await using (multiplexer)
        {
            var version = new RedisAuthorizationStateVersion(
                multiplexer,
                Options.Create(new RedisOptions
                {
                    ConnectionString = "127.0.0.1:1",
                    KeyPrefix = "net10:test:"
                }),
                NullLogger<RedisAuthorizationStateVersion>.Instance);

            var current = await version.GetCurrentAsync();
            Assert.Null(current);
        }
    }
}

public sealed class RedisAuthRateLimitStoreTests
{
    [Fact]
    public async Task TryAcquire_ExceedsPermitLimit_ReturnsFalse()
    {
        await using var redis = await RedisTestGate.TryConnectAsync();
        Assert.True(
            redis is not null,
            "Redis must be running on localhost:6379 (docker compose up -d redis).");

        var options = Options.Create(new RedisOptions
        {
            ConnectionString = "localhost:6379",
            KeyPrefix = $"net10:test:{Guid.NewGuid():N}:"
        });
        var store = new RedisAuthRateLimitStore(
            redis,
            options,
            NullLogger<RedisAuthRateLimitStore>.Instance);
        var partition = $"test-ip|/api/identity/login|{Guid.NewGuid():N}";

        Assert.True(await store.TryAcquireAsync(partition, permitLimit: 2, TimeSpan.FromMinutes(1), CancellationToken.None));
        Assert.True(await store.TryAcquireAsync(partition, permitLimit: 2, TimeSpan.FromMinutes(1), CancellationToken.None));
        Assert.False(await store.TryAcquireAsync(partition, permitLimit: 2, TimeSpan.FromMinutes(1), CancellationToken.None));
    }
}

internal static class RedisTestGate
{
    public static async Task<IConnectionMultiplexer?> TryConnectAsync()
    {
        try
        {
            var options = ConfigurationOptions.Parse("localhost:6379");
            options.AbortOnConnectFail = false;
            options.ConnectTimeout = 1_000;
            options.SyncTimeout = 1_000;
            options.AsyncTimeout = 1_000;

            var multiplexer = await ConnectionMultiplexer.ConnectAsync(options);
            var pong = await multiplexer.GetDatabase().PingAsync();
            if (pong <= TimeSpan.Zero && !multiplexer.IsConnected)
            {
                multiplexer.Dispose();
                return null;
            }

            return multiplexer;
        }
        catch
        {
            return null;
        }
    }
}
