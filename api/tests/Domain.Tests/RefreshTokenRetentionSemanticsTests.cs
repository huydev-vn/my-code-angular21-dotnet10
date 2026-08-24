using Domain.Identity;

namespace Domain.Tests;

public sealed class RefreshTokenRetentionSemanticsTests
{
    [Fact]
    public void Expired_revoked_tokens_are_inactive_and_keep_replacement_link()
    {
        var now = new DateTimeOffset(2026, 8, 21, 0, 0, 0, TimeSpan.Zero);
        var familyId = Guid.NewGuid();
        var token = RefreshToken.Issue(
            Guid.NewGuid(),
            "hash-1",
            familyId,
            now,
            now.AddDays(14));
        var replacementId = Guid.NewGuid();

        token.Revoke(now.AddHours(1), replacementId);

        Assert.True(token.IsRevoked);
        Assert.Equal(replacementId, token.ReplacedByTokenId);
        Assert.Equal(familyId, token.FamilyId);
        Assert.False(token.IsActive(now.AddDays(15)));
        Assert.True(token.IsExpired(now.AddDays(15)));
    }
}
