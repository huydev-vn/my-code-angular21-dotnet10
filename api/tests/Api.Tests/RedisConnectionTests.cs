using Infrastructure.Caching;
using Microsoft.Extensions.Configuration;

namespace Api.Tests;

public sealed class RedisConnectionTests
{
    [Fact]
    public void ResolveConnectionString_PrefersRedisSection()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Redis:ConnectionString"] = "localhost:6379",
                ["ConnectionStrings:Redis"] = "fallback:6380"
            })
            .Build();

        Assert.Equal("localhost:6379", RedisConnection.ResolveConnectionString(configuration));
    }

    [Fact]
    public void ResolveConnectionString_FallsBackWhenSectionBlank()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Redis:ConnectionString"] = "  ",
                ["ConnectionStrings:Redis"] = "localhost:6380"
            })
            .Build();

        Assert.Equal("localhost:6380", RedisConnection.ResolveConnectionString(configuration));
    }

    [Fact]
    public void ResolveConnectionString_ReturnsNullWhenBothMissing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        Assert.Null(RedisConnection.ResolveConnectionString(configuration));
    }
}
