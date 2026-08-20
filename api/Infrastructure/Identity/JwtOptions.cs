namespace Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public const int MinimumSigningKeyBytes = 32;

    public const string DevelopmentSigningKey = "DEV-ONLY-CHANGE-ME-USE-USER-SECRETS!!";

    public required string Issuer { get; init; }

    public required string Audience { get; init; }

    public required string SigningKey { get; init; }

    public int AccessTokenMinutes { get; init; } = 15;

    public int RefreshTokenDays { get; init; } = 14;

    public bool UsesDevelopmentSigningKey =>
        SigningKey is not null &&
        string.Equals(SigningKey, DevelopmentSigningKey, StringComparison.Ordinal);
}
