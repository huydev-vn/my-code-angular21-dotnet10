namespace Application.Features.Authorization.Contracts;

/// <summary>A permission catalog entry.</summary>
public sealed record PermissionDefinitionResponse(
    Guid Id,
    string Code,
    string Name,
    string? Module,
    string? Action,
    bool IsActive,
    DateTimeOffset CreatedAt);

/// <summary>Paged list of permission catalog entries.</summary>
public sealed record PermissionDefinitionListResponse(
    IReadOnlyList<PermissionDefinitionResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);

/// <summary>A business user group used to assign permissions.</summary>
public sealed record UserGroupResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsPrivileged,
    bool IsActive,
    DateTimeOffset CreatedAt);

/// <summary>Paged list of user groups.</summary>
public sealed record UserGroupListResponse(
    IReadOnlyList<UserGroupResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);

/// <summary>An organization unit in the nested scope tree.</summary>
public sealed record OrganizationUnitResponse(
    Guid Id,
    string Name,
    string Code,
    Guid? ParentId,
    bool IsActive,
    DateTimeOffset CreatedAt);

/// <summary>Paged list of organization units.</summary>
public sealed record OrganizationUnitListResponse(
    IReadOnlyList<OrganizationUnitResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);

/// <summary>Resolved authorization context for the current user.</summary>
public sealed record UserAuthorizationContextResponse(
    Guid UserId,
    IReadOnlyList<string> Groups,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<Guid> AccessibleOrganizationUnitIds);

/// <summary>Confirmation of a group assignment.</summary>
public sealed record AssignmentResponse(
    Guid GroupId,
    Guid TargetId,
    DateTimeOffset AssignedAt);
