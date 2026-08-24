using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Infrastructure.Caching;

/// <summary>Pings Redis for readiness when a shared connection is registered.</summary>
internal sealed class RedisHealthCheck(IConnectionMultiplexer multiplexer) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var latency = await multiplexer.GetDatabase().PingAsync();
            return HealthCheckResult.Healthy($"Redis ping {latency.TotalMilliseconds:F0}ms");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Redis ping failed.", exception);
        }
    }
}
