namespace Infrastructure.Authorization;

/// <summary>Options for the distributed authorization context cache (Redis or memory).</summary>
internal sealed class AuthorizationCacheOptions
{
    public const string SectionName = "Authorization:Cache";

    /// <summary>
    /// Absolute TTL for cached user authorization contexts. Shared Redis version
    /// bumps drop keys immediately across replicas; TTL bounds stale entries if
    /// a bump is missed.
    /// </summary>
    public int AbsoluteExpirationSeconds { get; init; } = 30;
}
