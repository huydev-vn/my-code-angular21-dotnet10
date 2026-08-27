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
    private static readonly PathString LoginPath = new("/api/identity/login");
    private static readonly PathString RegisterPath = new("/api/identity/register");
    private static readonly PathString MfaVerifyPath = new("/api/identity/mfa/verify");
    private static readonly PathString RefreshPath = new("/api/identity/refresh");
    private static readonly PathString RevokePath = new("/api/identity/revoke");

    private const int PermitLimit = 10;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    public async Task InvokeAsync(HttpContext context)
    {
        var store = context.RequestServices.GetService<RedisAuthRateLimitStore>();
        if (store is null || !TryGetLimitedPath(context.Request, out var path, out var failClosed))
        {
            await next(context);
            return;
        }

        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var partitionKey = $"{ip}|{path}";

        if (!await store.TryAcquireAsync(
                partitionKey,
                PermitLimit,
                Window,
                context.RequestAborted,
                failClosed))
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

    private static bool TryGetLimitedPath(
        HttpRequest request,
        out string path,
        out bool failClosed)
    {
        path = string.Empty;
        failClosed = false;
        if (!HttpMethods.IsPost(request.Method))
        {
            return false;
        }

        if (request.Path.Equals(LoginPath, StringComparison.OrdinalIgnoreCase) ||
            request.Path.Equals(RegisterPath, StringComparison.OrdinalIgnoreCase) ||
            request.Path.Equals(MfaVerifyPath, StringComparison.OrdinalIgnoreCase))
        {
            path = request.Path.Value?.ToLowerInvariant() ?? "/";
            failClosed = true;
            return true;
        }

        if (request.Path.Equals(RefreshPath, StringComparison.OrdinalIgnoreCase) ||
            request.Path.Equals(RevokePath, StringComparison.OrdinalIgnoreCase))
        {
            path = request.Path.Value?.ToLowerInvariant() ?? "/";
            failClosed = false;
            return true;
        }

        return false;
    }
}
