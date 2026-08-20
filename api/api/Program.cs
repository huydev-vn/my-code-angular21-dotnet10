using System.Text.Json.Serialization;
using Api.Extensions;
using Api.Middleware;
using Api.OpenApi;
using Application;
using Infrastructure;
using Infrastructure.Authorization;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Scalar.AspNetCore;

const string ClientCorsPolicy = "Client";

var builder = WebApplication.CreateBuilder(args);

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
            .AllowAnyMethod()));

builder.Services.AddApiSecurityServices();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddApiAuthentication(builder.Configuration);

var app = builder.Build();

try
{
    using var scope = app.Services.CreateScope();
    await IdentitySeeder.SeedAsync(
        scope.ServiceProvider,
        app.Configuration,
        CancellationToken.None);
    await AuthorizationSeeder.SeedAsync(
        scope.ServiceProvider,
        app.Configuration,
        CancellationToken.None);
}
catch (Exception exception) when (app.Environment.IsDevelopment())
{
    app.Logger.LogWarning(
        exception,
        "Identity or authorization seed skipped because the database is not ready.");
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

app.Run();
