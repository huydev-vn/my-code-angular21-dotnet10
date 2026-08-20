namespace Application.Features.Authorization.OrganizationUnits;

public sealed record CreateOrganizationUnitRequest(
    string Name,
    string Code,
    Guid? ParentId);
