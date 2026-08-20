namespace Application.Features.Authorization.Contracts;

public sealed record PermissionDefinitionResponse(
    Guid Id,
    string Code,
    string Name,
    string? Module,
    string? Action,
    bool IsActive,
    DateTimeOffset CreatedAt);

public sealed record PermissionDefinitionListResponse(
    IReadOnlyList<PermissionDefinitionResponse> Items);

public sealed record UserGroupResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    DateTimeOffset CreatedAt);

public sealed record UserGroupListResponse(
    IReadOnlyList<UserGroupResponse> Items);

public sealed record OrganizationUnitResponse(
    Guid Id,
    string Name,
    string Code,
    Guid? ParentId,
    bool IsActive,
    DateTimeOffset CreatedAt);

public sealed record OrganizationUnitListResponse(
    IReadOnlyList<OrganizationUnitResponse> Items);

public sealed record UserAuthorizationContextResponse(
    Guid UserId,
    IReadOnlyList<string> Groups,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<Guid> AccessibleOrganizationUnitIds);

public sealed record AssignmentResponse(
    Guid GroupId,
    Guid TargetId,
    DateTimeOffset AssignedAt);
