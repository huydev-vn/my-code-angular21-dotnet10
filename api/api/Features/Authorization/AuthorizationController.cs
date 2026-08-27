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
    MoveOrganizationUnit moveOrganizationUnit,
    SetOrganizationUnitActive setOrganizationUnitActive,
    AssignGroupPermission assignGroupPermission,
    AssignUserToGroup assignUserToGroup,
    AssignGroupOrganizationUnit assignGroupOrganizationUnit,
    RevokeGroupPermission revokeGroupPermission,
    RevokeUserFromGroup revokeUserFromGroup,
    RevokeGroupOrganizationUnit revokeGroupOrganizationUnit,
    ListAuthorizationAuditEvents listAuthorizationAuditEvents,
    GetUserAuthorizationContext getUserAuthorizationContext,
    ListAccessibleOrganizationUnits listAccessibleOrganizationUnits,
    // Agent C
    AssignUserOrganizationUnit assignUserOrganizationUnit,
    RevokeUserOrganizationUnit revokeUserOrganizationUnit,
    ListUserOrganizationUnits listUserOrganizationUnits,
    GetUserCapabilities getUserCapabilities) : ControllerBase
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

    /// <summary>
    /// Moves an organization unit under a new parent (null = root).
    /// Requires authorization.organization-units.write. Non-privileged actors must keep both
    /// the unit and new parent within their accessible organization-unit set.
    /// </summary>
    [RequirePermission(SystemPermissions.AuthorizationOrganizationUnitsWrite)]
    [HttpPost("organization-units/{id:guid}/move")]
    [ProducesResponseType(typeof(OrganizationUnitResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MoveOrganizationUnit(
        Guid id,
        [FromBody] MoveOrganizationUnitRequest request,
        CancellationToken cancellationToken) =>
        (await moveOrganizationUnit.HandleAsync(id, request, cancellationToken))
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

    /// <summary>
    /// Assigns a permission to a group. System admins: authorization.groups.write;
    /// regional admins: authorization.assignments.delegate (subject to grant containment).
    /// </summary>
    [RequireAnyPermission(
        SystemPermissions.AuthorizationGroupsWrite,
        SystemPermissions.AuthorizationAssignmentsDelegate)]
    [HttpPost("groups/permissions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignGroupPermission(
        [FromBody] AssignGroupPermissionRequest request,
        CancellationToken cancellationToken) =>
        (await assignGroupPermission.HandleAsync(request, cancellationToken)).ToActionResult(this);

    /// <summary>
    /// Revokes a permission from a group. Same two-tier gate as assign; revoke requires
    /// the permission still be delegatable by the actor (or privileged).
    /// </summary>
    [RequireAnyPermission(
        SystemPermissions.AuthorizationGroupsWrite,
        SystemPermissions.AuthorizationAssignmentsDelegate)]
    [HttpDelete("groups/permissions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeGroupPermission(
        [FromBody] RevokeGroupPermissionRequest request,
        CancellationToken cancellationToken) =>
        (await revokeGroupPermission.HandleAsync(request, cancellationToken)).ToActionResult(this);

    /// <summary>
    /// Assigns a user to a group. System: groups.write; regional: assignments.delegate
    /// (group OU roots must be within the actor's accessible set).
    /// </summary>
    [RequireAnyPermission(
        SystemPermissions.AuthorizationGroupsWrite,
        SystemPermissions.AuthorizationAssignmentsDelegate)]
    [HttpPost("groups/users")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignUserToGroup(
        [FromBody] AssignUserToGroupRequest request,
        CancellationToken cancellationToken) =>
        (await assignUserToGroup.HandleAsync(request, cancellationToken)).ToActionResult(this);

    /// <summary>
    /// Removes a user from a group. Same two-tier gate and group containment as assign.
    /// </summary>
    [RequireAnyPermission(
        SystemPermissions.AuthorizationGroupsWrite,
        SystemPermissions.AuthorizationAssignmentsDelegate)]
    [HttpDelete("groups/users")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeUserFromGroup(
        [FromBody] RevokeUserFromGroupRequest request,
        CancellationToken cancellationToken) =>
        (await revokeUserFromGroup.HandleAsync(request, cancellationToken)).ToActionResult(this);

    /// <summary>
    /// Scopes a group to an organization unit. System: organization-units.write;
    /// regional: assignments.delegate (OU must be in actor accessible set).
    /// </summary>
    [RequireAnyPermission(
        SystemPermissions.AuthorizationOrganizationUnitsWrite,
        SystemPermissions.AuthorizationAssignmentsDelegate)]
    [HttpPost("groups/organization-units")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignGroupOrganizationUnit(
        [FromBody] AssignGroupOrganizationUnitRequest request,
        CancellationToken cancellationToken) =>
        (await assignGroupOrganizationUnit.HandleAsync(request, cancellationToken))
            .ToActionResult(this);

    /// <summary>
    /// Removes organization-unit scope from a group. Same two-tier gate and OU containment.
    /// </summary>
    [RequireAnyPermission(
        SystemPermissions.AuthorizationOrganizationUnitsWrite,
        SystemPermissions.AuthorizationAssignmentsDelegate)]
    [HttpDelete("groups/organization-units")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
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

    /// <summary>
    /// Returns UI capabilities for the current user: granted permission metadata,
    /// group→OU accessible ids, and separate user↔OU membership rows.
    /// Authenticated callers only. Client should use this for show/hide only — never as security.
    /// </summary>
    [HttpGet("me/capabilities")]
    [ProducesResponseType(typeof(UserCapabilitiesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MeCapabilities(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await getUserCapabilities.HandleAsync(userId.Value, cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// Lists organization units within the caller's accessible set only (fail closed → empty).
    /// Authenticated callers only; does not require Global authorization.organization-units.read.
    /// Admin full-tree listing remains GET organization-units.
    /// </summary>
    [HttpGet("me/organization-units")]
    [ProducesResponseType(typeof(OrganizationUnitListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MeOrganizationUnits(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        return Ok(await listAccessibleOrganizationUnits.HandleAsync(
            userId.Value,
            PageRequest.Create(page, pageSize),
            isActive,
            cancellationToken));
    }

    /// <summary>
    /// Assigns a user to an organization unit (Primary/Additional membership metadata).
    /// Does not grant permissions or data access. System: organization-units.write;
    /// regional: users-organization-units.manage (OU must be in actor accessible set).
    /// </summary>
    [RequireAnyPermission(
        SystemPermissions.AuthorizationOrganizationUnitsWrite,
        SystemPermissions.AuthorizationUsersOrganizationUnitsManage)]
    [HttpPost("users/organization-units")]
    [ProducesResponseType(typeof(UserOrganizationUnitResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignUserOrganizationUnit(
        [FromBody] AssignUserOrganizationUnitRequest request,
        CancellationToken cancellationToken) =>
        (await assignUserOrganizationUnit.HandleAsync(request, cancellationToken))
            .ToActionResult(this);

    /// <summary>
    /// Deactivates a user↔OU membership. Does not change permissions or group scope.
    /// Same two-tier gate and OU containment as assign.
    /// </summary>
    [RequireAnyPermission(
        SystemPermissions.AuthorizationOrganizationUnitsWrite,
        SystemPermissions.AuthorizationUsersOrganizationUnitsManage)]
    [HttpDelete("users/organization-units")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeUserOrganizationUnit(
        [FromBody] RevokeUserOrganizationUnitRequest request,
        CancellationToken cancellationToken) =>
        (await revokeUserOrganizationUnit.HandleAsync(request, cancellationToken))
            .ToActionResult(this);

    /// <summary>
    /// Lists organization-unit memberships for a user. Requires authorization.organization-units.read.
    /// </summary>
    [RequirePermission(SystemPermissions.AuthorizationOrganizationUnitsRead)]
    [HttpGet("users/{userId:guid}/organization-units")]
    [ProducesResponseType(typeof(IReadOnlyList<UserOrganizationUnitResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListUserOrganizationUnits(
        Guid userId,
        [FromQuery] bool activeOnly = true,
        CancellationToken cancellationToken = default) =>
        Ok(await listUserOrganizationUnits.HandleAsync(userId, activeOnly, cancellationToken));
}
