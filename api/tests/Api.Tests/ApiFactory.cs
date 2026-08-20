using Infrastructure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Api.Tests;

/// <summary>Hosts the API without seeders so pipeline tests do not need PostgreSQL.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] =
                    "Host=127.0.0.1;Port=1;Database=unused;Username=unused;Password=unused",
                ["Jwt:Issuer"] = "net10-angular19",
                ["Jwt:Audience"] = "net10-angular19-client",
                ["Jwt:SigningKey"] = JwtOptions.DevelopmentSigningKey,
                ["Identity:RunSeeders"] = "false",
                ["Identity:AllowRegistration"] = "false",
                ["Client:Origins:0"] = "http://localhost:4200"
            });
        });
    }
}
