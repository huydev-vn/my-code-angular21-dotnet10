using System.Text.Json;
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

    private static IEnumerable<string> ConnectionStringFiles()
    {
        var current = Directory.GetCurrentDirectory();
        yield return Path.Combine(current, "appsettings.Development.json");
        yield return Path.Combine(current, "api", "appsettings.Development.json");
        yield return Path.Combine(current, "..", "api", "appsettings.Development.json");
    }
}
