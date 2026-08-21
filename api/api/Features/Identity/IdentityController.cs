using Api.Authorization;
using Api.Extensions;
using Api.Identity;
using Application.Common.Pagination;
using Application.Features.Identity.Contracts;
using Application.Features.Identity.GetCurrentUser;
using Application.Features.Identity.ListUsers;
using Application.Features.Identity.Login;
using Application.Features.Identity.Refresh;
using Application.Features.Identity.Register;
using Application.Features.Identity.Revoke;
using Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.Features.Identity;

[ApiController]
[Route("api/identity")]
[Produces("application/json")]
public sealed class IdentityController(
    RegisterUser registerUser,
    LoginUser loginUser,
    RefreshTokens refreshTokens,
    RevokeRefreshToken revokeRefreshToken,
    GetCurrentUser getCurrentUser,
    ListUsers listUsers,
    IHostEnvironment environment) : ControllerBase
{
    /// <summary>Creates a new user account and returns an access token.</summary>
    [AllowAnonymous]
    [EnableRateLimiting(AuthenticationExtensions.AuthRateLimitPolicy)]
    [HttpPost("register")]
    [ProducesResponseType(typeof(AccessTokenResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await registerUser.HandleAsync(request, cancellationToken);
        if (result.IsFailure)
        {
            return result.ToActionResult(this);
        }

        RefreshTokenCookie.Set(Response, result.Value!, environment.IsDevelopment());
        return CreatedAtAction(nameof(Me), result.Value!.ToAccessTokenResponse());
    }

    /// <summary>Authenticates a user and returns an access token.</summary>
    [AllowAnonymous]
    [EnableRateLimiting(AuthenticationExtensions.AuthRateLimitPolicy)]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AccessTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await loginUser.HandleAsync(request, cancellationToken);
        if (result.IsFailure)
        {
            return result.ToActionResult(this);
        }

        RefreshTokenCookie.Set(Response, result.Value!, environment.IsDevelopment());
        return Ok(result.Value!.ToAccessTokenResponse());
    }

    /// <summary>
    /// Rotates the refresh-token cookie and issues a new access token.
    /// Accepts an optional body refresh token for non-browser clients.
    /// </summary>
    [AllowAnonymous]
    [EnableRateLimiting(AuthenticationExtensions.AuthRateLimitPolicy)]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AccessTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokensRequest? request,
        CancellationToken cancellationToken)
    {
        var refreshToken = ResolveRefreshToken(request?.RefreshToken);
        if (refreshToken is null)
        {
            return BadRequest(CreateMissingRefreshTokenProblem());
        }

        var result = await refreshTokens.HandleAsync(
            new RefreshTokensRequest { RefreshToken = refreshToken },
            cancellationToken);
        if (result.IsFailure)
        {
            RefreshTokenCookie.Clear(Response, environment.IsDevelopment());
            return result.ToActionResult(this);
        }

        RefreshTokenCookie.Set(Response, result.Value!, environment.IsDevelopment());
        return Ok(result.Value!.ToAccessTokenResponse());
    }

    /// <summary>
    /// Revokes the refresh-token family from the cookie or optional body payload.
    /// </summary>
    [AllowAnonymous]
    [EnableRateLimiting(AuthenticationExtensions.AuthRateLimitPolicy)]
    [HttpPost("revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Revoke(
        [FromBody] RevokeRefreshTokenRequest? request,
        CancellationToken cancellationToken)
    {
        var refreshToken = ResolveRefreshToken(request?.RefreshToken);
        if (refreshToken is null)
        {
            RefreshTokenCookie.Clear(Response, environment.IsDevelopment());
            return NoContent();
        }

        var result = await revokeRefreshToken.HandleAsync(
            new RevokeRefreshTokenRequest { RefreshToken = refreshToken },
            cancellationToken);
        RefreshTokenCookie.Clear(Response, environment.IsDevelopment());
        return result.ToActionResult(this);
    }

    /// <summary>Returns the current authenticated user.</summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await getCurrentUser.HandleAsync(userId.Value, cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Lists users. Requires the users.read permission.</summary>
    [RequirePermission(SystemPermissions.UsersRead)]
    [HttpGet("users")]
    [ProducesResponseType(typeof(UserListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Users(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var result = await listUsers.HandleAsync(
            PageRequest.Create(page, pageSize),
            cancellationToken);
        return Ok(result);
    }

    private string? ResolveRefreshToken(string? bodyToken) =>
        !string.IsNullOrWhiteSpace(bodyToken)
            ? bodyToken
            : RefreshTokenCookie.Read(Request);

    private ProblemDetails CreateMissingRefreshTokenProblem() =>
        new()
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Bad Request",
            Detail = "A refresh token cookie or body value is required.",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            Extensions =
            {
                ["code"] = "identity.refresh_token_required",
                ["traceId"] = HttpContext.TraceIdentifier
            }
        };
}
