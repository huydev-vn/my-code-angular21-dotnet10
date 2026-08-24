using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Infrastructure.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Api.Tests;

public sealed class JwtBearerTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient(
        new() { AllowAutoRedirect = false });

    [Fact]
    public async Task ProtectedEndpoint_WithTamperedSignature_ReturnsUnauthorized()
    {
        var token = CreateAccessToken(signingKey: "TAMPERED-KEY-NOT-THE-DEV-SIGNING-KEY!!");
        var response = await SendAuthorizedAsync(token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithWrongIssuer_ReturnsUnauthorized()
    {
        var token = CreateAccessToken(issuer: "evil-issuer");
        var response = await SendAuthorizedAsync(token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithWrongAudience_ReturnsUnauthorized()
    {
        var token = CreateAccessToken(audience: "evil-audience");
        var response = await SendAuthorizedAsync(token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithExpiredToken_ReturnsUnauthorized()
    {
        var token = CreateAccessToken(
            notBefore: DateTime.UtcNow.AddMinutes(-30),
            expires: DateTime.UtcNow.AddMinutes(-10));
        var response = await SendAuthorizedAsync(token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<HttpResponseMessage> SendAuthorizedAsync(string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/identity/users");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(request);
    }

    private static string CreateAccessToken(
        string? signingKey = null,
        string? issuer = null,
        string? audience = null,
        DateTime? notBefore = null,
        DateTime? expires = null)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(signingKey ?? JwtOptions.DevelopmentSigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: issuer ?? "net10-angular19",
            audience: audience ?? "net10-angular19-client",
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString("D")),
                new Claim(JwtRegisteredClaimNames.Email, "jwt-test@example.com")
            ],
            notBefore: notBefore ?? now.AddMinutes(-1),
            expires: expires ?? now.AddMinutes(15),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
