namespace Application.Features.Identity.Abstractions;

/// <summary>
/// One-time MFA login challenges. Tickets are opaque to clients; only hashes
/// are stored (Redis when configured, otherwise in-process memory).
/// </summary>
public interface IMfaChallengeStore
{
    Task StoreAsync(
        string ticket,
        Guid userId,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken);

    /// <summary>Atomically reads and deletes a pending challenge.</summary>
    Task<Guid?> ConsumeAsync(string ticket, CancellationToken cancellationToken);
}
