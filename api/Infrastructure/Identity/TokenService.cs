using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using Application.Common.Time;
using Application.Features.Identity.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Identity;

internal sealed class TokenService(IOptions<JwtOptions> jwtOptions, IClock clock) : ITokenService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public AccessToken CreateAccessToken(UserAccount user)
    {
        var expiresAt = clock.UtcNow.AddMinutes(_jwt.AccessTokenMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey));
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email)
        };

        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = _jwt.Issuer,
            Audience = _jwt.Audience,
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        });

        return new AccessToken(token, expiresAt);
    }

    public IssuedRefreshToken CreateRefreshToken()
    {
        var plainText = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        return new IssuedRefreshToken(
            plainText,
            HashRefreshToken(plainText),
            clock.UtcNow.AddDays(_jwt.RefreshTokenDays));
    }

    public string HashRefreshToken(string refreshToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
