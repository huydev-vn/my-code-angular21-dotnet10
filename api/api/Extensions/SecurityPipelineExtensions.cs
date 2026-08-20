using System.Net;
using System.Threading.RateLimiting;
using Api.Configuration;
using Api.Middleware;
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
            options.AddPolicy(
                AuthenticationExtensions.AuthRateLimitPolicy,
                httpContext => RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));
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
        app.MapHealthChecks(
                "/health/ready",
                new HealthCheckOptions
                {
                    Predicate = check => check.Tags.Contains("ready")
                })
            .AllowAnonymous();
    }
}
