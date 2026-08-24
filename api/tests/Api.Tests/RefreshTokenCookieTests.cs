using Api.Identity;
using Application.Features.Identity.Contracts;
using Microsoft.AspNetCore.Http;

namespace Api.Tests;

public sealed class RefreshTokenCookieTests
{
    [Fact]
    public void Set_InDevelopment_WritesHttpOnlyLaxPathCookieWithoutSecure()
    {
        var context = new DefaultHttpContext();
        var tokens = new AuthResponse(
            AccessToken: "access",
            AccessTokenExpiresAt: DateTimeOffset.UtcNow.AddMinutes(15),
            RefreshToken: "refresh-plain",
            RefreshTokenExpiresAt: DateTimeOffset.UtcNow.AddDays(14));

        RefreshTokenCookie.Set(context.Response, tokens, isDevelopment: true);

        var setCookie = Assert.Single(context.Response.Headers.SetCookie);
        Assert.Contains($"{RefreshTokenCookie.Name}=refresh-plain", setCookie, StringComparison.Ordinal);
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/identity", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secure", setCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Set_OutsideDevelopment_MarksCookieSecure()
    {
        var context = new DefaultHttpContext();
        var tokens = new AuthResponse(
            AccessToken: "access",
            AccessTokenExpiresAt: DateTimeOffset.UtcNow.AddMinutes(15),
            RefreshToken: "refresh-plain",
            RefreshTokenExpiresAt: DateTimeOffset.UtcNow.AddDays(14));

        RefreshTokenCookie.Set(context.Response, tokens, isDevelopment: false);

        var setCookie = Assert.Single(context.Response.Headers.SetCookie);
        Assert.Contains("secure", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/identity", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", setCookie, StringComparison.OrdinalIgnoreCase);
    }
}
