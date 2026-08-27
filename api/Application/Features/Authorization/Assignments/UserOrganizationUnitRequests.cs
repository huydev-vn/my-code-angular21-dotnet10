using Domain.Authorization;

namespace Application.Features.Authorization.Assignments;

/// <summary>Assigns a user to an organization unit as Primary or Additional membership metadata.</summary>
public sealed record AssignUserOrganizationUnitRequest(
    Guid UserId,
    Guid OrganizationUnitId,
    OrganizationUnitRelationship Relationship);

/// <summary>Deactivates a user↔OU membership (does not affect group permissions or scope).</summary>
public sealed record RevokeUserOrganizationUnitRequest(
    Guid UserId,
    Guid OrganizationUnitId);
