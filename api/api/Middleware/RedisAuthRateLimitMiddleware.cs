using Infrastructure.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace Api.Middleware;

/// <summary>
/// Shared Redis fixed-window limiter for identity auth endpoints. Falls through
/// when Redis is not registered (Development memory fallback); the ASP.NET
/// process-local rate limiter remains as a second layer.
/// </summary>
internal sealed class RedisAuthRateLimitMiddleware(RequestDelegate next)
{
    private static readonly PathString[] AuthPaths =
    [
        new("/api/identity/login"),
        new("/api/identity/register"),
        new("/api/identity/refresh"),
        new("/api/identity/revoke"),
        new("/api/identity/mfa/verify")
    ];

    private const int PermitLimit = 10;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    public async Task InvokeAsync(HttpContext context)
    {
        var store = context.RequestServices.GetService<RedisAuthRateLimitStore>();
        if (store is null || !RequiresLimit(context.Request))
        {
            await next(context);
            return;
        }

        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "/";
        var partitionKey = $"{ip}|{path}";

        if (!await store.TryAcquireAsync(partitionKey, PermitLimit, Window, context.RequestAborted))
        {
            context.RequestServices
                .GetService<Application.Features.Identity.Abstractions.IAuthMetrics>()
                ?.RateLimited();

            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Too Many Requests",
                Detail = "Too many authentication attempts. Try again later.",
                Type = "https://tools.ietf.org/html/rfc6585#section-4",
                Instance = context.Request.Path
            };
            problem.Extensions["code"] = "http.rate_limited";
            problem.Extensions["traceId"] = context.TraceIdentifier;
            await context.Response.WriteAsJsonAsync(problem);
            return;
        }

        await next(context);
    }

    private static bool RequiresLimit(HttpRequest request) =>
        HttpMethods.IsPost(request.Method) &&
        AuthPaths.Any(path => request.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
}
