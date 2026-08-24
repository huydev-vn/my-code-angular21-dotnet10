using Api.Authorization;
using Api.Extensions;
using Application.Common.Pagination;
using Application.Features.Authorization.Assignments;
using Application.Features.Authorization.Audit;
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
    UpdatePermissionDefinition updatePermissionDefinition,
    SetPermissionDefinitionActive setPermissionDefinitionActive,
    CreateUserGroup createUserGroup,
    GetUserGroup getUserGroup,
    ListUserGroups listUserGroups,
    UpdateUserGroup updateUserGroup,
    SetUserGroupActive setUserGroupActive,
    CreateOrganizationUnit createOrganizationUnit,
    GetOrganizationUnit getOrganizationUnit,
    ListOrganizationUnits listOrganizationUnits,
    UpdateOrganizationUnit updateOrganizationUnit,
    SetOrganizationUnitActive setOrganizationUnitActive,
    AssignGroupPermission assignGroupPermission,
    AssignUserToGroup assignUserToGroup,
    AssignGroupOrganizationUnit assignGroupOrganizationUnit,
    RevokeGroupPermission revokeGroupPermission,
    RevokeUserFromGroup revokeUserFromGroup,
    RevokeGroupOrganizationUnit revokeGroupOrganizationUnit,
    ListAuthorizationAuditEvents listAuthorizationAuditEvents,
    GetUserAuthorizationContext getUserAuthorizationContext) : ControllerBase
{
    /// <summary>Lists the permission catalog. Requires authorization.permissions.read.</summary>
    [RequirePermission(SystemPermissions.AuthorizationPermissionsRead)]
    [HttpGet("permissions")]
    [ProducesResponseType(typeof(PermissionDefinitionListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListPermissions(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken) =>
        Ok(await listPermissionDefinitions.HandleAsync(
            PageRequest.Create(page, pageSize),
            isActive,
            cancellationToken));

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

    /// <summary>Updates a permission catalog entry. Requires authorization.permissions.write.</summary>
    [RequirePermission(SystemPermissions.AuthorizationPermissionsWrite)]
    [HttpPut("permissions/{id:guid}")]
    [ProducesResponseType(typeof(PermissionDefinitionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePermission(
        Guid id,
        [FromBody] UpdatePermissionDefinitionRequest request,
        CancellationToken cancellationToken) =>
        (await updatePermissionDefinition.HandleAsync(id, request, cancellationToken))
            .ToActionResult(this);

    /// <summary>Activates or deactivates a permission. Requires authorization.permissions.write.</summary>
    [RequirePermission(SystemPermissions.AuthorizationPermissionsWrite)]
    [HttpPost("permissions/{id:guid}/active")]
    [ProducesResponseType(typeof(PermissionDefinitionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPermissionActive(
        Guid id,
        [FromQuery] bool isActive,
        CancellationToken cancellationToken) =>
        (await setPermissionDefinitionActive.HandleAsync(id, isActive, cancellationToken))
            .ToActionResult(this);

    /// <summary>Lists user groups. Requires authorization.groups.read.</summary>
    [RequirePermission(SystemPermissions.AuthorizationGroupsRead)]
    [HttpGet("groups")]
    [ProducesResponseType(typeof(UserGroupListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListGroups(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken) =>
        Ok(await listUserGroups.HandleAsync(
            PageRequest.Create(page, pageSize),
            isActive,
            cancellationToken));

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

    /// <summary>Updates a user group. Requires authorization.groups.write.</summary>
    [RequirePermission(SystemPermissions.AuthorizationGroupsWrite)]
    [HttpPut("groups/{id:guid}")]
    [ProducesResponseType(typeof(UserGroupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateGroup(
        Guid id,
        [FromBody] UpdateUserGroupRequest request,
        CancellationToken cancellationToken) =>
        (await updateUserGroup.HandleAsync(id, request, cancellationToken)).ToActionResult(this);

    /// <summary>Activates or deactivates a user group. Requires authorization.groups.write.</summary>
    [RequirePermission(SystemPermissions.AuthorizationGroupsWrite)]
    [HttpPost("groups/{id:guid}/active")]
    [ProducesResponseType(typeof(UserGroupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetGroupActive(
        Guid id,
        [FromQuery] bool isActive,
        CancellationToken cancellationToken) =>
        (await setUserGroupActive.HandleAsync(id, isActive, cancellationToken)).ToActionResult(this);

    /// <summary>Lists organization units. Requires authorization.organization-units.read.</summary>
    [RequirePermission(SystemPermissions.AuthorizationOrganizationUnitsRead)]
    [HttpGet("organization-units")]
    [ProducesResponseType(typeof(OrganizationUnitListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListOrganizationUnits(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken) =>
        Ok(await listOrganizationUnits.HandleAsync(
            PageRequest.Create(page, pageSize),
            isActive,
            cancellationToken));

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

    /// <summary>Updates an organization unit. Requires authorization.organization-units.write.</summary>
    [RequirePermission(SystemPermissions.AuthorizationOrganizationUnitsWrite)]
    [HttpPut("organization-units/{id:guid}")]
    [ProducesResponseType(typeof(OrganizationUnitResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOrganizationUnit(
        Guid id,
        [FromBody] UpdateOrganizationUnitRequest request,
        CancellationToken cancellationToken) =>
        (await updateOrganizationUnit.HandleAsync(id, request, cancellationToken))
            .ToActionResult(this);

    /// <summary>Activates or deactivates an organization unit. Requires authorization.organization-units.write.</summary>
    [RequirePermission(SystemPermissions.AuthorizationOrganizationUnitsWrite)]
    [HttpPost("organization-units/{id:guid}/active")]
    [ProducesResponseType(typeof(OrganizationUnitResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetOrganizationUnitActive(
        Guid id,
        [FromQuery] bool isActive,
        CancellationToken cancellationToken) =>
        (await setOrganizationUnitActive.HandleAsync(id, isActive, cancellationToken))
            .ToActionResult(this);

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

    /// <summary>Revokes a permission from a group. Requires authorization.groups.write.</summary>
    [RequirePermission(SystemPermissions.AuthorizationGroupsWrite)]
    [HttpDelete("groups/permissions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeGroupPermission(
        [FromBody] RevokeGroupPermissionRequest request,
        CancellationToken cancellationToken) =>
        (await revokeGroupPermission.HandleAsync(request, cancellationToken)).ToActionResult(this);

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

    /// <summary>Removes a user from a group. Requires authorization.groups.write.</summary>
    [RequirePermission(SystemPermissions.AuthorizationGroupsWrite)]
    [HttpDelete("groups/users")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeUserFromGroup(
        [FromBody] RevokeUserFromGroupRequest request,
        CancellationToken cancellationToken) =>
        (await revokeUserFromGroup.HandleAsync(request, cancellationToken)).ToActionResult(this);

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

    /// <summary>Removes organization-unit scope from a group. Requires authorization.organization-units.write.</summary>
    [RequirePermission(SystemPermissions.AuthorizationOrganizationUnitsWrite)]
    [HttpDelete("groups/organization-units")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeGroupOrganizationUnit(
        [FromBody] RevokeGroupOrganizationUnitRequest request,
        CancellationToken cancellationToken) =>
        (await revokeGroupOrganizationUnit.HandleAsync(request, cancellationToken))
            .ToActionResult(this);

    /// <summary>Lists authorization audit events. Requires authorization.audit.read.</summary>
    [RequirePermission(SystemPermissions.AuthorizationAuditRead)]
    [HttpGet("audit-events")]
    [ProducesResponseType(typeof(AuthorizationAuditEventListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAuditEvents(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? action,
        [FromQuery] Guid? actorUserId,
        CancellationToken cancellationToken) =>
        Ok(await listAuthorizationAuditEvents.HandleAsync(
            PageRequest.Create(page, pageSize),
            action,
            actorUserId,
            cancellationToken));

    /// <summary>
    /// Returns the current user's groups, permissions, and organization-unit
    /// scope. Authenticated callers only; no admin permission required.
    /// Users with no group membership receive empty permission/scope lists.
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
