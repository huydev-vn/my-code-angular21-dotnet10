using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Api.Authorization;
using Api.Extensions;
using Application.Features.Authorization.Assignments;
using Application.Features.Authorization.GetContext;
using Application.Features.Authorization.Groups;
using Application.Features.Authorization.OrganizationUnits;
using Application.Features.Authorization.Permissions;
using Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Features.Authorization;

[ApiController]
[Route("api/authorization")]
[Authorize]
[Produces("application/json")]
public sealed class AuthorizationController(
    CreatePermissionDefinition createPermissionDefinition,
    ListPermissionDefinitions listPermissionDefinitions,
    CreateUserGroup createUserGroup,
    ListUserGroups listUserGroups,
    CreateOrganizationUnit createOrganizationUnit,
    ListOrganizationUnits listOrganizationUnits,
    AssignGroupPermission assignGroupPermission,
    AssignUserToGroup assignUserToGroup,
    AssignGroupOrganizationUnit assignGroupOrganizationUnit,
    GetUserAuthorizationContext getUserAuthorizationContext) : ControllerBase
{
    [RequirePermission(SystemPermissions.AuthorizationPermissionsRead)]
    [HttpGet("permissions")]
    public async Task<IActionResult> ListPermissions(CancellationToken cancellationToken) =>
        Ok(await listPermissionDefinitions.HandleAsync(cancellationToken));

    [RequirePermission(SystemPermissions.AuthorizationPermissionsWrite)]
    [HttpPost("permissions")]
    public async Task<IActionResult> CreatePermission(
        [FromBody] CreatePermissionDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await createPermissionDefinition.HandleAsync(request, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(ListPermissions), result.Value)
            : result.ToActionResult();
    }

    [RequirePermission(SystemPermissions.AuthorizationGroupsRead)]
    [HttpGet("groups")]
    public async Task<IActionResult> ListGroups(CancellationToken cancellationToken) =>
        Ok(await listUserGroups.HandleAsync(cancellationToken));

    [RequirePermission(SystemPermissions.AuthorizationGroupsWrite)]
    [HttpPost("groups")]
    public async Task<IActionResult> CreateGroup(
        [FromBody] CreateUserGroupRequest request,
        CancellationToken cancellationToken)
    {
        var result = await createUserGroup.HandleAsync(request, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(ListGroups), result.Value)
            : result.ToActionResult();
    }

    [RequirePermission(SystemPermissions.AuthorizationOrganizationUnitsRead)]
    [HttpGet("organization-units")]
    public async Task<IActionResult> ListOrganizationUnits(
        CancellationToken cancellationToken) =>
        Ok(await listOrganizationUnits.HandleAsync(cancellationToken));

    [RequirePermission(SystemPermissions.AuthorizationOrganizationUnitsWrite)]
    [HttpPost("organization-units")]
    public async Task<IActionResult> CreateOrganizationUnit(
        [FromBody] CreateOrganizationUnitRequest request,
        CancellationToken cancellationToken)
    {
        var result = await createOrganizationUnit.HandleAsync(request, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(ListOrganizationUnits), result.Value)
            : result.ToActionResult();
    }

    [RequirePermission(SystemPermissions.AuthorizationGroupsWrite)]
    [HttpPost("groups/permissions")]
    public async Task<IActionResult> AssignGroupPermission(
        [FromBody] AssignGroupPermissionRequest request,
        CancellationToken cancellationToken) =>
        (await assignGroupPermission.HandleAsync(request, cancellationToken)).ToActionResult();

    [RequirePermission(SystemPermissions.AuthorizationGroupsWrite)]
    [HttpPost("groups/users")]
    public async Task<IActionResult> AssignUserToGroup(
        [FromBody] AssignUserToGroupRequest request,
        CancellationToken cancellationToken) =>
        (await assignUserToGroup.HandleAsync(request, cancellationToken)).ToActionResult();

    [RequirePermission(SystemPermissions.AuthorizationOrganizationUnitsWrite)]
    [HttpPost("groups/organization-units")]
    public async Task<IActionResult> AssignGroupOrganizationUnit(
        [FromBody] AssignGroupOrganizationUnitRequest request,
        CancellationToken cancellationToken) =>
        (await assignGroupOrganizationUnit.HandleAsync(request, cancellationToken))
            .ToActionResult();

    /// <summary>
    /// Returns the current user's groups, permissions, and organization-unit
    /// scope. Authenticated callers only; no admin permission required.
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await getUserAuthorizationContext.HandleAsync(userId.Value, cancellationToken);
        return result.ToActionResult();
    }

    private Guid? GetUserId()
    {
        var value = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
