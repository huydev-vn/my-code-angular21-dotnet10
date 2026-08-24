namespace Application.Features.Authorization.Assignments;

public sealed record AssignGroupPermissionRequest(Guid GroupId, Guid PermissionId);

public sealed record AssignUserToGroupRequest(Guid GroupId, Guid UserId);

public sealed record AssignGroupOrganizationUnitRequest(
    Guid GroupId,
    Guid OrganizationUnitId);

public sealed record RevokeGroupPermissionRequest(Guid GroupId, Guid PermissionId);

public sealed record RevokeUserFromGroupRequest(Guid GroupId, Guid UserId);

public sealed record RevokeGroupOrganizationUnitRequest(
    Guid GroupId,
    Guid OrganizationUnitId);

