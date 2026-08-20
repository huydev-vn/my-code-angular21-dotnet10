namespace Domain.Identity;

/// <summary>
/// Persisted refresh-token grant. Only the hash is stored; the plaintext token
/// is returned once to the client.
/// </summary>
public sealed class RefreshToken
{
    private RefreshToken()
    {
    }

    private RefreshToken(
        Guid id,
        Guid userId,
        string tokenHash,
        Guid familyId,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
    {
        Id = id;
        UserId = userId;
        TokenHash = tokenHash;
        FamilyId = familyId;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = null!;

    public Guid FamilyId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public Guid? ReplacedByTokenId { get; private set; }

    public bool IsRevoked => RevokedAt is not null;

    public bool IsExpired(DateTimeOffset utcNow) => utcNow >= ExpiresAt;

    public bool IsActive(DateTimeOffset utcNow) => !IsRevoked && !IsExpired(utcNow);

    public static RefreshToken Issue(
        Guid userId,
        string tokenHash,
        Guid familyId,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        if (expiresAt <= createdAt)
        {
            throw new ArgumentException(
                "Refresh token expiry must be later than creation.",
                nameof(expiresAt));
        }

        return new RefreshToken(
            Guid.NewGuid(),
            userId,
            tokenHash,
            familyId,
            createdAt,
            expiresAt);
    }

    public void Revoke(DateTimeOffset utcNow, Guid? replacedByTokenId = null)
    {
        if (IsRevoked)
        {
            return;
        }

        RevokedAt = utcNow;
        ReplacedByTokenId = replacedByTokenId;
    }
}
