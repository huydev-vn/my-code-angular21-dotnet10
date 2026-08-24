namespace Infrastructure.Caching;

/// <summary>
/// Redis is used only for shared authorization cache/version, MFA challenge
/// tickets, and distributed rate-limit counters. PostgreSQL remains the source
/// of truth for identity, refresh tokens, permissions, and audit.
/// </summary>
public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    /// <summary>StackExchange.Redis configuration string.</summary>
    public string ConnectionString { get; init; } = string.Empty;

    /// <summary>Key prefix shared by all Redis keys for this application.</summary>
    public string KeyPrefix { get; init; } = "net10:";

    /// <summary>Connect timeout in milliseconds.</summary>
    public int ConnectTimeoutMs { get; init; } = 5_000;

    /// <summary>Sync/async operation timeout in milliseconds.</summary>
    public int OperationTimeoutMs { get; init; } = 3_000;

    /// <summary>
    /// When true (default), Redis must be reachable at startup outside Development.
    /// </summary>
    public bool AbortOnConnectFail { get; init; } = true;

    public bool HasValidTimeouts =>
        ConnectTimeoutMs is >= 500 and <= 30_000 &&
        OperationTimeoutMs is >= 500 and <= 30_000;

    public bool HasValidKeyPrefix =>
        !string.IsNullOrWhiteSpace(KeyPrefix) &&
        !KeyPrefix.Contains(' ', StringComparison.Ordinal);
}
