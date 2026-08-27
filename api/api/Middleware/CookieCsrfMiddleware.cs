using Api.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Api.Middleware;

/// <summary>
/// Defends cookie-authenticated identity endpoints against cross-site request forgery
/// by requiring Origin/Referer to match configured Client:Origins when the refresh
/// cookie is present, and by requiring a trusted Origin/Referer on login/register/MFA
/// (login CSRF). Body-token refresh/revoke without a cookie skips the Origin check.
/// </summary>
internal sealed class CookieCsrfMiddleware(RequestDelegate next, IConfiguration configuration)
{
    private static readonly PathString RefreshPath = new("/api/identity/refresh");
    private static readonly PathString RevokePath = new("/api/identity/revoke");
    private static readonly PathString LoginPath = new("/api/identity/login");
    private static readonly PathString RegisterPath = new("/api/identity/register");
    private static readonly PathString MfaVerifyPath = new("/api/identity/mfa/verify");

    public async Task InvokeAsync(HttpContext context)
    {
        if (RequiresOriginCheck(context.Request))
        {
            var allowedOrigins = configuration
                .GetSection("Client:Origins")
                .Get<string[]>() ?? [];

            if (!IsTrustedOrigin(context.Request, allowedOrigins))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Forbidden",
                    Detail = "Cross-site cookie authentication is not allowed for this origin.",
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.4",
                    Instance = context.Request.Path
                };
                problem.Extensions["code"] = "identity.csrf_origin_rejected";
                problem.Extensions["traceId"] = context.TraceIdentifier;
                await context.Response.WriteAsJsonAsync(problem);
                return;
            }
        }

        await next(context);
    }

    private static bool RequiresOriginCheck(HttpRequest request)
    {
        if (!HttpMethods.IsPost(request.Method))
        {
            return false;
        }

        if (request.Path == RefreshPath || request.Path == RevokePath)
        {
            return !string.IsNullOrWhiteSpace(RefreshTokenCookie.Read(request));
        }

        // Browser POSTs that set a session cookie must present Origin or Referer.
        // Missing both fails closed (login CSRF / session fixation via cookie plant).
        if (request.Path == LoginPath ||
            request.Path == RegisterPath ||
            request.Path == MfaVerifyPath)
        {
            return true;
        }

        return false;
    }

    private static bool IsTrustedOrigin(HttpRequest request, IReadOnlyList<string> allowedOrigins)
    {
        if (allowedOrigins.Count == 0)
        {
            return false;
        }

        if (Uri.TryCreate(request.Headers.Origin.ToString(), UriKind.Absolute, out var origin))
        {
            return MatchesAllowedOrigin(origin, allowedOrigins);
        }

        if (Uri.TryCreate(request.Headers.Referer.ToString(), UriKind.Absolute, out var referer))
        {
            return MatchesAllowedOrigin(referer, allowedOrigins);
        }

        // Browser cookie posts should send Origin or Referer; reject otherwise.
        return false;
    }

    private static bool MatchesAllowedOrigin(Uri candidate, IReadOnlyList<string> allowedOrigins)
    {
        var candidateOrigin = candidate.GetLeftPart(UriPartial.Authority);
        foreach (var allowed in allowedOrigins)
        {
            if (Uri.TryCreate(allowed, UriKind.Absolute, out var allowedUri) &&
                string.Equals(
                    candidateOrigin,
                    allowedUri.GetLeftPart(UriPartial.Authority),
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
