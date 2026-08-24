using System.Net;
using System.Net.Http.Json;
using Api.Middleware;

namespace Api.Tests;

public sealed class SecurityPipelineTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient(
        new() { AllowAutoRedirect = false });

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/identity/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task HealthLive_ReturnsOk()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CorrelationId_IsEchoedOnResponse()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add(CorrelationIdMiddleware.HeaderName, "test-correlation-id");

        var response = await _client.SendAsync(request);

        Assert.Equal("test-correlation-id", response.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single());
    }

    [Fact]
    public async Task Login_WithEmptyBody_ReturnsValidationProblem()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/identity/login",
            new { email = "", password = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = await response.Content.ReadAsStringAsync();
        Assert.Contains("validation.failed", payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Login_WithUntrustedOrigin_ReturnsCsrfForbidden()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/identity/login")
        {
            Content = JsonContent.Create(new
            {
                email = "user@example.com",
                password = "Password123!@#"
            })
        };
        request.Headers.TryAddWithoutValidation("Origin", "https://evil.example");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var payload = await response.Content.ReadAsStringAsync();
        Assert.Contains("identity.csrf_origin_rejected", payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Refresh_WithCookieAndUntrustedOrigin_ReturnsCsrfForbidden()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/identity/refresh");
        request.Headers.TryAddWithoutValidation("Origin", "https://evil.example");
        request.Headers.TryAddWithoutValidation("Cookie", "refresh_token=not-a-real-token");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var payload = await response.Content.ReadAsStringAsync();
        Assert.Contains("identity.csrf_origin_rejected", payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MfaVerify_WithUntrustedOrigin_ReturnsCsrfForbidden()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/identity/mfa/verify")
        {
            Content = JsonContent.Create(new
            {
                mfaTicket = "not-a-real-ticket",
                code = "123456"
            })
        };
        request.Headers.TryAddWithoutValidation("Origin", "https://evil.example");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var payload = await response.Content.ReadAsStringAsync();
        Assert.Contains("identity.csrf_origin_rejected", payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Login_WithTrustedOrigin_PassesCsrfGate()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/identity/login")
        {
            Content = JsonContent.Create(new
            {
                email = "user@example.com",
                password = "Password123!@#"
            })
        };
        request.Headers.TryAddWithoutValidation("Origin", "http://localhost:4200");

        var response = await _client.SendAsync(request);

        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        var payload = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("identity.csrf_origin_rejected", payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Refresh_WithoutCookie_SkipsCsrfEvenWithUntrustedOrigin()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/identity/refresh")
        {
            Content = JsonContent.Create(new { refreshToken = "body-token" })
        };
        request.Headers.TryAddWithoutValidation("Origin", "https://evil.example");

        var response = await _client.SendAsync(request);

        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        var payload = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("identity.csrf_origin_rejected", payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Refresh_WithCookieAndNoOrigin_ReturnsCsrfForbidden()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/identity/refresh");
        request.Headers.TryAddWithoutValidation("Cookie", "refresh_token=not-a-real-token");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var payload = await response.Content.ReadAsStringAsync();
        Assert.Contains("identity.csrf_origin_rejected", payload, StringComparison.Ordinal);
    }
}
