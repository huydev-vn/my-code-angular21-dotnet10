using Serilog.Context;

namespace Api.Middleware;

/// <summary>
/// Accepts or assigns a correlation id and pushes it into the Serilog log context.
/// </summary>
internal sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    private const int MaxLength = 128;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);

        context.Items[HeaderName] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        var raw = context.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return context.TraceIdentifier;
        }

        var trimmed = raw.Trim();
        if (trimmed.Length > MaxLength || !IsSafeCorrelationId(trimmed))
        {
            return context.TraceIdentifier;
        }

        return trimmed;
    }

    private static bool IsSafeCorrelationId(string value)
    {
        foreach (var character in value)
        {
            if (char.IsAsciiLetterOrDigit(character) ||
                character is '-' or '_' or '.')
            {
                continue;
            }

            return false;
        }

        return true;
    }
}
