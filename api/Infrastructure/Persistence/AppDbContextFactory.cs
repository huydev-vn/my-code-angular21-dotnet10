using System.Text.Json;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Persistence;

internal sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Database")
            ?? ReadLocalConnectionString()
            ?? ReadUserSecretsConnectionString()
            ?? "Host=localhost;Port=5432;Database=app;Username=postgres";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options);
    }

    private static string? ReadLocalConnectionString()
    {
        foreach (var path in ConnectionStringFiles())
        {
            if (!File.Exists(path))
            {
                continue;
            }

            using var document = JsonDocument.Parse(File.ReadAllText(path));
            if (document.RootElement.TryGetProperty("ConnectionStrings", out var connections)
                && connections.TryGetProperty("Database", out var database))
            {
                var value = database.GetString();
                if (!string.IsNullOrWhiteSpace(value)
                    && value.Contains("Password=", StringComparison.OrdinalIgnoreCase))
                {
                    return value;
                }
            }
        }

        return null;
    }

    private static string? ReadUserSecretsConnectionString()
    {
        var userSecretsId = TryReadUserSecretsId();
        if (userSecretsId is null)
        {
            return null;
        }

        var secretsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Microsoft",
            "UserSecrets",
            userSecretsId,
            "secrets.json");

        if (!File.Exists(secretsPath))
        {
            return null;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(secretsPath));
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (!property.Name.Equals(
                    "ConnectionStrings:Database",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = property.Value.GetString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static string? TryReadUserSecretsId()
    {
        foreach (var projectPath in ApiProjectFiles())
        {
            if (!File.Exists(projectPath))
            {
                continue;
            }

            var document = XDocument.Load(projectPath);
            var userSecretsId = document
                .Descendants("UserSecretsId")
                .Select(element => element.Value.Trim())
                .FirstOrDefault(id => !string.IsNullOrWhiteSpace(id));

            if (userSecretsId is not null)
            {
                return userSecretsId;
            }
        }

        return null;
    }

    private static IEnumerable<string> ConnectionStringFiles()
    {
        var current = Directory.GetCurrentDirectory();
        yield return Path.Combine(current, "appsettings.Development.json");
        yield return Path.Combine(current, "api", "appsettings.Development.json");
        yield return Path.Combine(current, "..", "api", "appsettings.Development.json");
    }

    private static IEnumerable<string> ApiProjectFiles()
    {
        var current = Directory.GetCurrentDirectory();
        yield return Path.Combine(current, "api.csproj");
        yield return Path.Combine(current, "api", "api.csproj");
        yield return Path.Combine(current, "..", "api", "api.csproj");
    }
}
