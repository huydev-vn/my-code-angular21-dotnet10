namespace Application.Features.Authorization.Abstractions;

/// <summary>Effective authorization state resolved for a user at runtime.</summary>
public sealed record UserAuthorizationContext(
    Guid UserId,
    IReadOnlyList<string> GroupNames,
    IReadOnlyList<string> PermissionCodes,
    IReadOnlyList<Guid> AccessibleOrganizationUnitIds);
