using Api.Authorization;
using Api.Extensions;
using Application.Features.Authorization.Assignments;
using Application.Features.Authorization.Contracts;
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
    GetPermissionDefinition getPermissionDefinition,
    ListPermissionDefinitions listPermissionDefinitions,
    CreateUserGroup createUserGroup,
    GetUserGroup getUserGroup,
    ListUserGroups listUserGroups,
    CreateOrganizationUnit createOrganizationUnit,
    GetOrganizationUnit getOrganizationUnit,
    ListOrganizationUnits listOrganizationUnits,
    AssignGroupPermission assignGroupPermission,
    AssignUserToGroup assignUserToGroup,
    AssignGroupOrganizationUnit assignGroupOrganizationUnit,
    GetUserAuthorizationContext getUserAuthorizationContext) : ControllerBase
{
    /// <summary>Lists the permission catalog. Requires authorization.permissions.read.</summary>
    [RequirePermission(SystemPermissions.AuthorizationPermissionsRead)]
    [HttpGet("permissions")]
    [ProducesResponseType(typeof(PermissionDefinitionListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListPermissions(CancellationToken cancellationToken) =>
        Ok(await listPermissionDefinitions.HandleAsync(cancellationToken));

    /// <summary>Returns one permission. Requires authorization.permissions.read.</summary>
    [RequirePermission(SystemPermissions.AuthorizationPermissionsRead)]
    [HttpGet("permissions/{id:guid}")]
    [ProducesResponseType(typeof(PermissionDefinitionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPermission(
        Guid id,
        CancellationToken cancellationToken) =>
        (await getPermissionDefinition.HandleAsync(id, cancellationToken)).ToActionResult(this);

    /// <summary>Creates a permission catalog entry. Requires authorization.permissions.write.</summary>
    [RequirePermission(SystemPermissions.AuthorizationPermissionsWrite)]
    [HttpPost("permissions")]
    [ProducesResponseType(typeof(PermissionDefinitionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreatePermission(
        [FromBody] CreatePermissionDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await createPermissionDefinition.HandleAsync(request, cancellationToken);
        return result.ToCreatedAtAction(
            this,
            nameof(GetPermission),
            new { id = result.Value?.Id });
    }

    /// <summary>Lists user groups. Requires authorization.groups.read.</summary>
    [RequirePermission(SystemPermissions.AuthorizationGroupsRead)]
    [HttpGet("groups")]
    [ProducesResponseType(typeof(UserGroupListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListGroups(CancellationToken cancellationToken) =>
        Ok(await listUserGroups.HandleAsync(cancellationToken));

    /// <summary>Returns one user group. Requires authorization.groups.read.</summary>
    [RequirePermission(SystemPermissions.AuthorizationGroupsRead)]
    [HttpGet("groups/{id:guid}")]
    [ProducesResponseType(typeof(UserGroupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGroup(
        Guid id,
        CancellationToken cancellationToken) =>
        (await getUserGroup.HandleAsync(id, cancellationToken)).ToActionResult(this);

    /// <summary>Creates a user group. Requires authorization.groups.write.</summary>
    [RequirePermission(SystemPermissions.AuthorizationGroupsWrite)]
    [HttpPost("groups")]
    [ProducesResponseType(typeof(UserGroupResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateGroup(
        [FromBody] CreateUserGroupRequest request,
        CancellationToken cancellationToken)
    {
        var result = await createUserGroup.HandleAsync(request, cancellationToken);
        return result.ToCreatedAtAction(
            this,
            nameof(GetGroup),
            new { id = result.Value?.Id });
    }

    /// <summary>Lists organization units. Requires authorization.organization-units.read.</summary>
    [RequirePermission(SystemPermissions.AuthorizationOrganizationUnitsRead)]
    [HttpGet("organization-units")]
    [ProducesResponseType(typeof(OrganizationUnitListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListOrganizationUnits(
        CancellationToken cancellationToken) =>
        Ok(await listOrganizationUnits.HandleAsync(cancellationToken));

    /// <summary>Returns one organization unit. Requires authorization.organization-units.read.</summary>
    [RequirePermission(SystemPermissions.AuthorizationOrganizationUnitsRead)]
    [HttpGet("organization-units/{id:guid}")]
    [ProducesResponseType(typeof(OrganizationUnitResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrganizationUnit(
        Guid id,
        CancellationToken cancellationToken) =>
        (await getOrganizationUnit.HandleAsync(id, cancellationToken)).ToActionResult(this);

    /// <summary>Creates an organization unit. Requires authorization.organization-units.write.</summary>
    [RequirePermission(SystemPermissions.AuthorizationOrganizationUnitsWrite)]
    [HttpPost("organization-units")]
    [ProducesResponseType(typeof(OrganizationUnitResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateOrganizationUnit(
        [FromBody] CreateOrganizationUnitRequest request,
        CancellationToken cancellationToken)
    {
        var result = await createOrganizationUnit.HandleAsync(request, cancellationToken);
        return result.ToCreatedAtAction(
            this,
            nameof(GetOrganizationUnit),
            new { id = result.Value?.Id });
    }

    /// <summary>Assigns a permission to a group. Requires authorization.groups.write.</summary>
    [RequirePermission(SystemPermissions.AuthorizationGroupsWrite)]
    [HttpPost("groups/permissions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignGroupPermission(
        [FromBody] AssignGroupPermissionRequest request,
        CancellationToken cancellationToken) =>
        (await assignGroupPermission.HandleAsync(request, cancellationToken)).ToActionResult(this);

    /// <summary>Assigns a user to a group. Requires authorization.groups.write.</summary>
    [RequirePermission(SystemPermissions.AuthorizationGroupsWrite)]
    [HttpPost("groups/users")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignUserToGroup(
        [FromBody] AssignUserToGroupRequest request,
        CancellationToken cancellationToken) =>
        (await assignUserToGroup.HandleAsync(request, cancellationToken)).ToActionResult(this);

    /// <summary>Scopes a group to an organization unit. Requires authorization.organization-units.write.</summary>
    [RequirePermission(SystemPermissions.AuthorizationOrganizationUnitsWrite)]
    [HttpPost("groups/organization-units")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignGroupOrganizationUnit(
        [FromBody] AssignGroupOrganizationUnitRequest request,
        CancellationToken cancellationToken) =>
        (await assignGroupOrganizationUnit.HandleAsync(request, cancellationToken))
            .ToActionResult(this);

    /// <summary>
    /// Returns the current user's groups, permissions, and organization-unit
    /// scope. Authenticated callers only; no admin permission required.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserAuthorizationContextResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await getUserAuthorizationContext.HandleAsync(userId.Value, cancellationToken);
        return result.ToActionResult(this);
    }
}
