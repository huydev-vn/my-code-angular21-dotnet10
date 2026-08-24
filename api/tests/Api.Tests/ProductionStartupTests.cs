using Infrastructure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Api.Tests;

public sealed class ProductionStartupTests
{
    [Fact]
    public void Host_WithoutRedis_Throws()
    {
        using var factory = new ProductionApiFactory(new Dictionary<string, string?>
        {
            ["Redis:ConnectionString"] = "",
            ["ConnectionStrings:Redis"] = "",
            ["Client:Origins:0"] = "https://app.example.com",
            ["Jwt:SigningKey"] = "production-signing-key-at-least-32-bytes!!"
        });

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("Redis", FlattenMessage(exception), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Host_WithHttpClientOrigins_Throws()
    {
        using var factory = new ProductionApiFactory(new Dictionary<string, string?>
        {
            ["Redis:ConnectionString"] = "localhost:6379",
            ["Redis:AbortOnConnectFail"] = "false",
            ["Client:Origins:0"] = "http://localhost:4200",
            ["Jwt:SigningKey"] = "production-signing-key-at-least-32-bytes!!"
        });

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("Client:Origins", FlattenMessage(exception), StringComparison.Ordinal);
    }

    [Fact]
    public void Host_WithDevelopmentSigningKey_Throws()
    {
        using var factory = new ProductionApiFactory(new Dictionary<string, string?>
        {
            ["Redis:ConnectionString"] = "localhost:6379",
            ["Redis:AbortOnConnectFail"] = "false",
            ["Client:Origins:0"] = "https://app.example.com",
            ["Jwt:SigningKey"] = JwtOptions.DevelopmentSigningKey
        });

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("SigningKey", FlattenMessage(exception), StringComparison.OrdinalIgnoreCase);
    }

    private static string FlattenMessage(Exception exception)
    {
        var parts = new List<string>();
        for (var current = exception; current is not null; current = current.InnerException)
        {
            parts.Add(current.Message);
        }

        return string.Join(" | ", parts);
    }

    private sealed class ProductionApiFactory(Dictionary<string, string?> overrides)
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                var values = new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Database"] =
                        "Host=127.0.0.1;Port=1;Database=unused;Username=unused;Password=unused",
                    ["AllowedHosts"] = "api.example.com",
                    ["Jwt:Issuer"] = "net10-angular19",
                    ["Jwt:Audience"] = "net10-angular19-client",
                    ["Jwt:SigningKey"] = "production-signing-key-at-least-32-bytes!!",
                    ["Identity:RunSeeders"] = "false",
                    ["Identity:AllowRegistration"] = "false",
                    ["OpenTelemetry:Enabled"] = "false"
                };

                foreach (var (key, value) in overrides)
                {
                    values[key] = value;
                }

                configuration.AddInMemoryCollection(values);
            });

            // Avoid resolving Redis/DB during host construction for negative tests.
            builder.ConfigureServices(services =>
            {
                services.Configure<HostOptions>(options =>
                    options.BackgroundServiceExceptionBehavior =
                        BackgroundServiceExceptionBehavior.Ignore);
            });
        }
    }
}
