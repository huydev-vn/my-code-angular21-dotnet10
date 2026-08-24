using Application.Features.Authorization.Abstractions;
using Application.Features.Authorization.Contracts;
using Domain.Authorization;

namespace Application.Features.Authorization;

internal static class AuthorizationMapping
{
    public static PermissionDefinitionResponse ToResponse(this PermissionDefinition permission) =>
        new(
            permission.Id,
            permission.Code,
            permission.Name,
            permission.Module,
            permission.Action,
            permission.IsActive,
            permission.CreatedAt);

    public static UserGroupResponse ToResponse(this UserGroup group) =>
        new(
            group.Id,
            group.Name,
            group.Description,
            group.IsPrivileged,
            group.IsActive,
            group.CreatedAt);

    public static OrganizationUnitResponse ToResponse(this OrganizationUnit unit) =>
        new(
            unit.Id,
            unit.Name,
            unit.Code,
            unit.ParentId,
            unit.IsActive,
            unit.CreatedAt);

    public static UserAuthorizationContextResponse ToResponse(
        this UserAuthorizationContext context) =>
        new(
            context.UserId,
            context.GroupNames,
            context.PermissionCodes,
            context.AccessibleOrganizationUnitIds);
}
