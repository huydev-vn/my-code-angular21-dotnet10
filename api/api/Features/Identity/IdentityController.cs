using Api.Authorization;
using Api.Extensions;
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
    ListUsers listUsers) : ControllerBase
{
    /// <summary>Creates a new user account and returns tokens.</summary>
    [AllowAnonymous]
    [EnableRateLimiting(AuthenticationExtensions.AuthRateLimitPolicy)]
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
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

        return CreatedAtAction(nameof(Me), result.Value);
    }

    /// <summary>Authenticates a user and returns tokens.</summary>
    [AllowAnonymous]
    [EnableRateLimiting(AuthenticationExtensions.AuthRateLimitPolicy)]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await loginUser.HandleAsync(request, cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Rotates refresh tokens and issues a new access token.</summary>
    [AllowAnonymous]
    [EnableRateLimiting(AuthenticationExtensions.AuthRateLimitPolicy)]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokensRequest request,
        CancellationToken cancellationToken)
    {
        var result = await refreshTokens.HandleAsync(request, cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Revokes the refresh-token family for the presented token.</summary>
    [AllowAnonymous]
    [EnableRateLimiting(AuthenticationExtensions.AuthRateLimitPolicy)]
    [HttpPost("revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Revoke(
        [FromBody] RevokeRefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await revokeRefreshToken.HandleAsync(request, cancellationToken);
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
}
