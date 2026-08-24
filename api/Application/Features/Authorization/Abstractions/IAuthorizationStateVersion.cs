namespace Application.Features.Authorization.Abstractions;

/// <summary>
/// Monotonic version bumped when authorization state changes so permission
/// caches drop. When Redis is configured this is shared across replicas;
/// otherwise it is process-local (Development fallback only).
/// </summary>
public interface IAuthorizationStateVersion
{
    /// <summary>
    /// Returns the current version, or <c>null</c> when the shared store is
    /// unavailable. Callers must bypass distributed cache on <c>null</c>.
    /// </summary>
    Task<long?> GetCurrentAsync(CancellationToken cancellationToken = default);

    Task BumpAsync(CancellationToken cancellationToken = default);
}
