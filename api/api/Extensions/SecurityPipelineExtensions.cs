using System.Net;
using System.Threading.RateLimiting;
using Api.Configuration;
using Api.Middleware;
using Application.Features.Identity.Abstractions;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Api.Extensions;

internal static class SecurityPipelineExtensions
{
    /// <summary>Registers rate limiting and reverse-proxy header options.</summary>
    public static IServiceCollection AddApiSecurityServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<ReverseProxyOptions>()
            .Bind(configuration.GetSection(ReverseProxyOptions.SectionName));

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.RequestServices
                    .GetService<IAuthMetrics>()
                    ?.RateLimited();

                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too Many Requests",
                    Detail = "Too many authentication attempts. Try again later.",
                    Type = "https://tools.ietf.org/html/rfc6585#section-4",
                    Instance = context.HttpContext.Request.Path
                };
                problem.Extensions["code"] = "http.rate_limited";
                problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
                await context.HttpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
            };
            // Partition only by client IP + path. Do not accept client-supplied
            // account headers — those enable unlimited bucket rotation.
            options.AddPolicy(
                AuthenticationExtensions.AuthRateLimitPolicy,
                httpContext =>
                {
                    var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    var path = httpContext.Request.Path.Value?.ToLowerInvariant() ?? "/";
                    var partitionKey = $"{ip}|{path}";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        });
                });
        });

        services.AddOptions<ForwardedHeadersOptions>()
            .Configure<Microsoft.Extensions.Options.IOptions<ReverseProxyOptions>>(
                (options, proxyOptions) =>
                {
                    options.ForwardedHeaders =
                        ForwardedHeaders.XForwardedFor |
                        ForwardedHeaders.XForwardedProto |
                        ForwardedHeaders.XForwardedHost;

                    options.KnownProxies.Clear();
                    options.KnownIPNetworks.Clear();

                    foreach (var proxy in proxyOptions.Value.KnownProxies)
                    {
                        if (IPAddress.TryParse(proxy, out var address))
                        {
                            options.KnownProxies.Add(address);
                        }
                    }

                    foreach (var network in proxyOptions.Value.KnownNetworks)
                    {
                        if (System.Net.IPNetwork.TryParse(network, out var parsed))
                        {
                            options.KnownIPNetworks.Add(parsed);
                        }
                    }
                });

        return services;
    }

    /// <summary>
    /// Applies the HTTP security pipeline. Forwarded headers must run before HTTPS
    /// redirection so reverse proxies do not cause redirect loops.
    /// </summary>
    public static WebApplication UseApiSecurityPipeline(
        this WebApplication app,
        string corsPolicy)
    {
        if (!app.Environment.IsDevelopment())
        {
            var allowedHosts = app.Configuration["AllowedHosts"];
            if (string.IsNullOrWhiteSpace(allowedHosts) || allowedHosts.Trim() == "*")
            {
                throw new InvalidOperationException(
                    "AllowedHosts must be set to concrete host name(s) outside Development.");
            }

            var knownProxies = app.Configuration
                .GetSection("ReverseProxy:KnownProxies")
                .Get<string[]>() ?? [];
            var knownNetworks = app.Configuration
                .GetSection("ReverseProxy:KnownNetworks")
                .Get<string[]>() ?? [];
            if (knownProxies.Length == 0 && knownNetworks.Length == 0)
            {
                app.Logger.LogWarning(
                    "ReverseProxy:KnownProxies and KnownNetworks are empty. " +
                    "If the API sits behind a reverse proxy, configure them or rate limiting " +
                    "and forwarded headers will use the proxy IP for every client.");
            }

            var clientOrigins = app.Configuration.GetSection("Client:Origins").Get<string[]>() ?? [];
            if (clientOrigins.Length == 0 ||
                clientOrigins.Any(origin =>
                    !Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
                    uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new InvalidOperationException(
                    "Client:Origins must contain at least one absolute HTTPS origin outside Development.");
            }

            var redisConnection = Infrastructure.Caching.RedisConnection.ResolveConnectionString(
                app.Configuration);
            if (string.IsNullOrWhiteSpace(redisConnection))
            {
                throw new InvalidOperationException(
                    "Redis:ConnectionString (or ConnectionStrings:Redis) is required outside Development for shared " +
                    "authorization cache/version and distributed auth rate limits.");
            }
        }

        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseMiddleware<CorrelationIdMiddleware>();

        if (!app.Environment.IsDevelopment())
        {
            app.UseForwardedHeaders();
            app.UseHttpsRedirection();
            app.UseHsts();
        }

        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseMiddleware<CookieCsrfMiddleware>();
        app.UseMiddleware<RedisAuthRateLimitMiddleware>();
        app.UseSerilogRequestLogging(options =>
        {
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set(
                    "CorrelationId",
                    httpContext.Items[CorrelationIdMiddleware.HeaderName]?.ToString()
                    ?? httpContext.TraceIdentifier);
                diagnosticContext.Set("UserId", httpContext.User.GetUserId()?.ToString());
            };
        });
        app.UseCors(corsPolicy);
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }

    public static void MapApiHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks(
                "/health/live",
                new HealthCheckOptions { Predicate = _ => false })
            .AllowAnonymous();

        // Readiness probes PostgreSQL and Redis (when configured). Leave
        // AllowAnonymous for orchestrator probes, but do not expose this path on
        // the public internet — restrict at ingress / load-balancer ACLs to the
        // control plane network only.
        app.MapHealthChecks(
                "/health/ready",
                new HealthCheckOptions
                {
                    Predicate = check => check.Tags.Contains("ready")
                })
            .AllowAnonymous();

        if (!app.Environment.IsDevelopment())
        {
            app.Logger.LogWarning(
                "Map /health/ready is anonymous and hits PostgreSQL/Redis. " +
                "Restrict it to the orchestrator/load-balancer network at the edge.");
        }
    }
}
