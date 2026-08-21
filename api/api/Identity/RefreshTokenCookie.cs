using Application.Features.Identity.Contracts;

namespace Api.Identity;

/// <summary>HttpOnly refresh-token cookie helpers for the identity endpoints.</summary>
internal static class RefreshTokenCookie
{
    public const string Name = "refresh_token";
    private const string Path = "/api/identity";

    public static void Set(HttpResponse response, AuthResponse tokens, bool isDevelopment)
    {
        response.Cookies.Append(
            Name,
            tokens.RefreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = !isDevelopment,
                SameSite = SameSiteMode.Lax,
                Path = Path,
                Expires = tokens.RefreshTokenExpiresAt,
                IsEssential = true
            });
    }

    public static void Clear(HttpResponse response, bool isDevelopment)
    {
        response.Cookies.Append(
            Name,
            string.Empty,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = !isDevelopment,
                SameSite = SameSiteMode.Lax,
                Path = Path,
                Expires = DateTimeOffset.UnixEpoch,
                IsEssential = true
            });
    }

    public static string? Read(HttpRequest request) =>
        request.Cookies.TryGetValue(Name, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;
}
