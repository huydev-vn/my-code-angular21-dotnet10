using System.Text.Json.Serialization;
using Api.Extensions;
using Api.Middleware;
using Api.OpenApi;
using Application;
using Application.Common.Settings;
using Infrastructure;
using Infrastructure.Authorization;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Scalar.AspNetCore;
using Serilog;

const string ClientCorsPolicy = "Client";

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, _, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", context.HostingEnvironment.ApplicationName));

var connectionString = builder.Configuration.GetConnectionString("Database")
    ?? throw new InvalidOperationException(
        "Connection string 'Database' is not configured.");
var clientOrigins = builder.Configuration
    .GetSection("Client:Origins")
    .Get<string[]>() ?? [];

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter()));

builder.Services.AddOpenApi(options =>
    options.AddDocumentTransformer<BearerSecurityTransformer>());
builder.Services.AddProblemDetails();
builder.Services.AddHttpContextAccessor();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>(
        name: "database",
        tags: ["ready"]);
builder.Services.AddCors(options =>
    options.AddPolicy(
        ClientCorsPolicy,
        policy => policy
            .WithOrigins(clientOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));

builder.Services.AddApiSecurityServices(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(
    builder.Configuration,
    connectionString,
    builder.Environment.IsDevelopment());
builder.Services.AddApiAuthentication(builder.Configuration);

var app = builder.Build();

var identitySettings = app.Services.GetRequiredService<IIdentitySettings>();
if (identitySettings.RunSeeders)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var stopping = app.Lifetime.ApplicationStopping;
        await IdentitySeeder.SeedAsync(
            scope.ServiceProvider,
            app.Configuration,
            stopping);
        await AuthorizationSeeder.SeedAsync(
            scope.ServiceProvider,
            app.Configuration,
            stopping);
    }
    catch (Exception exception) when (app.Environment.IsDevelopment())
    {
        app.Logger.LogWarning(
            exception,
            "Identity or authorization seed skipped because the database is not ready.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference(
        "/scalar",
        options => options
            .WithTitle("Backend API")
            .EnablePersistentAuthentication())
        .AllowAnonymous();
}

app.UseApiSecurityPipeline(ClientCorsPolicy);
app.MapControllers();
app.MapApiHealthChecks();

try
{
    app.Run();
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;

