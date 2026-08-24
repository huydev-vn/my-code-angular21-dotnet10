using Application.Features.Authorization.Abstractions;

namespace Infrastructure.Authorization;

/// <summary>Process-local authorization version (Development fallback when Redis is absent).</summary>
internal sealed class MemoryAuthorizationStateVersion : IAuthorizationStateVersion
{
    private long _current;

    public Task<long?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<long?>(Interlocked.Read(ref _current));
    }

    public Task BumpAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Interlocked.Increment(ref _current);
        return Task.CompletedTask;
    }
}
