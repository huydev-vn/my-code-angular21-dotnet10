using Domain.Identity;

namespace Domain.Tests;

public sealed class RefreshTokenTests
{
    [Fact]
    public void Revoke_is_idempotent_and_records_replacement()
    {
        var now = new DateTimeOffset(2026, 8, 20, 0, 0, 0, TimeSpan.Zero);
        var token = RefreshToken.Issue(
            Guid.NewGuid(),
            "abc",
            Guid.NewGuid(),
            now,
            now.AddDays(14));
        var replacement = Guid.NewGuid();

        token.Revoke(now.AddMinutes(1), replacement);
        token.Revoke(now.AddMinutes(2), Guid.NewGuid());

        Assert.True(token.IsRevoked);
        Assert.Equal(replacement, token.ReplacedByTokenId);
        Assert.False(token.IsActive(now.AddMinutes(3)));
    }
}
