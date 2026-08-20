namespace Application.Features.Authorization.OrganizationUnits;

/// <summary>Payload for creating an organization unit in the nested tree.</summary>
public sealed record CreateOrganizationUnitRequest(
    string Name,
    string Code,
    Guid? ParentId);
